/**
 * Copyright 2015 Marc Rufer, d-fens GmbH
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Entity;
using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Executors;
using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Loaders;
using biz.dfch.CS.Entity.LifeCycleManager.Controller;
using biz.dfch.CS.Entity.LifeCycleManager.Logging;
using biz.dfch.CS.Entity.LifeCycleManager.Model;
using biz.dfch.CS.Entity.LifeCycleManager.UserData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CalloutDefinition = biz.dfch.CS.Entity.LifeCycleManager.CumulusCoreService.CalloutDefinition;
using Job = biz.dfch.CS.Entity.LifeCycleManager.CumulusCoreService.Job;
using StateChangeLock = biz.dfch.CS.Entity.LifeCycleManager.CumulusCoreService.StateChangeLock;

[assembly: InternalsVisibleTo("biz.dfch.CS.Entity.LifeCycleManager.Tests")]
namespace biz.dfch.CS.Entity.LifeCycleManager
{
    // DFTODO implement entity hierarchy
    // DFTODO implement ACL
    // DFTODO Modify plugin interface to provide a factory
    public class LifeCycleManager
    {
        private const String CALLOUT_JOB_TYPE = "CalloutData";
        private static Object _lock = new Object();
        private static IStateMachineConfigLoader _staticStateMachineConfigLoader = null;
        private static ICalloutExecutor _staticCalloutExecutor = null;

        private static CumulusCoreService.Core _coreService = new CumulusCoreService.Core(
            new Uri(ConfigurationManager.AppSettings["LifeCycleManager.Endpoint.Core"]));

        [Import(typeof(IStateMachineConfigLoader))]
        internal IStateMachineConfigLoader _stateMachineConfigLoader;

        [Import(typeof (ICalloutExecutor))]
        internal ICalloutExecutor _calloutExecutor;

        private StateMachine.StateMachine _stateMachine;
        private EntityController _entityController;
        private String _entityType;

        public LifeCycleManager(IAuthenticationProvider authenticationProvider, String entityType)
        {
            Debug.WriteLine("Create new instance of LifeCycleManager for entityType '{0}'", entityType);
            _entityType = entityType;

            lock (_lock)
            {
                if (null == _staticStateMachineConfigLoader)
                {
                    LoadAndComposeParts();
                    _staticStateMachineConfigLoader = _stateMachineConfigLoader;
                    _staticCalloutExecutor = _calloutExecutor;
                    SetCoreServiceCredentialsBasedOnConfigValues();
                }
                else
                {
                    _stateMachineConfigLoader = _staticStateMachineConfigLoader;
                    _calloutExecutor = _staticCalloutExecutor;
                }
            }
            _entityController = new EntityController(authenticationProvider);
            _stateMachine = new StateMachine.StateMachine();
            ConfigureStateMachine(entityType);
        }

        private void LoadAndComposeParts()
        {
            var assemblyCatalog = new AggregateCatalog();

            // Adds all the parts found in the given directory
            var folder = ConfigurationManager.AppSettings["LifeCycleManager.ExtensionsFolder"];
            Debug.WriteLine("Loading assemblies from folder: {0}", folder);
            try
            {
                if (!Path.IsPathRooted(folder))
                {
                    folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, folder);
                }
                assemblyCatalog.Catalogs.Add(new DirectoryCatalog(folder));
            }
            catch (Exception ex)
            {
                Trace.WriteLine("WARNING: Loading extensions from '{0}' FAILED.\n{1}", folder, ex.Message);
            }
            finally
            {
                assemblyCatalog.Catalogs.Add(new AssemblyCatalog(typeof(LifeCycleManager).Assembly));
            }

            var _container = new CompositionContainer(assemblyCatalog);

            try
            {
                Debug.WriteLine("Composing MEF parts...");
                _container.ComposeParts(this);
                Debug.WriteLine("Composition MEF parts successfully completed!");
            }
            catch (CompositionException compositionException)
            {
                Trace.WriteLine(compositionException.ToString());
            }
        }

        private void SetCoreServiceCredentialsBasedOnConfigValues()
        {
            var username = ConfigurationManager.AppSettings["LifeCycleManager.Service.Core.User"];
            var password = ConfigurationManager.AppSettings["LifeCycleManager.Service.Core.Password"];
            _coreService.Credentials = new NetworkCredential(username, password);
        }

        private void ConfigureStateMachine(String entityType)
        {
            Debug.WriteLine("Configuring state machine for entityType '{0}'", entityType);
            String config;
            lock (_lock)
            {
                config = _stateMachineConfigLoader.LoadConfiguration(entityType);
            }

            if (null != config)
            {
                try
                {
                    _stateMachine.SetupStateMachine(config);
                }
                catch (JsonReaderException e)
                {
                    Debug.WriteLine("Error occurred while parsing state machine configuraiton for entity type '{0}' : {1}",
                        entityType,
                        e.Message);
                    throw new ArgumentException("Invalid state machine configuration", e);
                }
            }
        }

        public void RequestStateChange(Uri entityUri, String entity, String condition, String tenantId)
        {
            Debug.WriteLine("Got state change request for entity with URI: '{0}' and condition: '{1}'", entityUri, condition);
            
            CheckForExistingStateChangeLock(entityUri);
            var preCalloutDefinition = LoadCalloutDefinition(condition, entityUri, tenantId,
                Model.CalloutDefinition.CalloutDefinitionType.Pre.ToString());
            CreateStateChangeLockForEntity(entityUri);

            String token = null;
            if (null == preCalloutDefinition)
            {
                DoPostCallout(entityUri, entity, condition, tenantId);
            }
            else
            {
                try
                {
                    var calloutData = CreatePreCalloutData(entityUri, entity, condition);
                    token = CreateJob(entityUri, tenantId, calloutData);
                    lock (_lock)
                    {
                        _calloutExecutor.ExecuteCallout(preCalloutDefinition.Parameters, calloutData);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("An error occurred while preparing or executing pre '{0}' callout for entity with id '{1}'",
                        condition,
                        entityUri.ToString());
                    if (null != token)
                    {
                        ChangeJobState(token, JobStateEnum.Failed);
                    }
                    DeleteStateChangeLockOfEntity(entityUri);
                    throw new InvalidOperationException(e.Message);
                }
            }
        }

        private void DoPostCallout(Uri entityUri, String entity, String condition, String tenantId)
        {
            CalloutData postCalloutData = null;
            String token = null;
            try
            {
                var postCalloutDefinition = LoadCalloutDefinition(condition, entityUri, tenantId,
                    Model.CalloutDefinition.CalloutDefinitionType.Post.ToString());
                ChangeEntityState(entityUri, entity, condition);
                if (null != postCalloutDefinition)
                {
                    postCalloutData = CreatePostCalloutData(entityUri, entity, condition);
                    token = CreateJob(entityUri, tenantId, postCalloutData);
                    lock (_lock)
                    {
                        _calloutExecutor.ExecuteCallout(postCalloutDefinition.Parameters, postCalloutData);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("An error occurred while preparing or executing post '{0}' callout for entity with id '{1}'",
                    condition,
                    entityUri.ToString());
                if (null != token)
                {
                    ChangeJobState(token, JobStateEnum.Failed);
                }
                if (postCalloutData != null)
                {
                    SetEntityState(entityUri, entity, postCalloutData.OriginalState);
                }
                DeleteStateChangeLockOfEntity(entityUri);
                throw new InvalidOperationException(e.Message);
            }
        }

        private void CheckForExistingStateChangeLock(Uri entityUri)
        {
            var scl = _coreService.StateChangeLocks
               .Where(l => l.EntityId == entityUri.ToString())
               .FirstOrDefault();

            if (null != scl)
            {
                Debug.WriteLine("State of entity '{0}' can not be changed because the entity is locked for state changes", entityUri.ToString());
                throw new InvalidOperationException();
            }
        }

        private void CreateStateChangeLockForEntity(Uri entityUri)
        {
            _coreService.AddToStateChangeLocks(
                new StateChangeLock
                {
                    EntityId = entityUri.ToString()
                }
            );
            _coreService.SaveChanges();
            Debug.WriteLine("Created state change lock for entity with id '{0}'", entityUri);
        }

        private void SetEntityState(Uri entityUri, String entity, String stateToSet)
        {
            Debug.WriteLine("Set state of entity with id '{0}' to '{1}'", entityUri, stateToSet);
            var obj = JObject.Parse(entity);
            obj["State"] = stateToSet;
            _entityController.UpdateEntity(entityUri, JsonConvert.SerializeObject(obj));
        }

        public void Next(Uri entityUri, String entity, String tenantId)
        {
            Debug.WriteLine("Request next state for entity with Uri: '{0}", entityUri);
            RequestStateChange(entityUri, entity, _stateMachine.ContinueCondition, tenantId);
        }

        public void Cancel(Uri entityUri, String entity, String tenantId)
        {
            Debug.WriteLine("Request cancel condition for entity with Uri: '{0}", entityUri);
            RequestStateChange(entityUri, entity, _stateMachine.CancelCondition, tenantId);
        }

        public void OnAllowCallback(Job job)
        {
            var parameters = JsonConvert.DeserializeObject<CalloutData>(job.Parameters);

            var entityUri = new Uri(job.ReferencedItemId);

            ChangeJobState(job.Token, JobStateEnum.Finished);
            if (parameters.Type == Model.CalloutDefinition.CalloutDefinitionType.Pre.ToString())
            {
                Debug.WriteLine("Process allow POST action callback for job with id '{0}'", job.Id);
                DoPostCallout(entityUri, LoadEntity(entityUri), parameters.Action, job.TenantId);
            }
            else if (parameters.Type == Model.CalloutDefinition.CalloutDefinitionType.Post.ToString())
            {
                Debug.WriteLine("Process allow PRE action callback for job with id '{0}'", job.Id);
                DeleteStateChangeLockOfEntity(entityUri);
            }
        }

        public void OnDeclineCallback(Job job)
        {
            var parameters = JsonConvert.DeserializeObject<CalloutData>(job.Parameters);

            var entityUri = new Uri(job.ReferencedItemId);

            if (parameters.Type == Model.CalloutDefinition.CalloutDefinitionType.Pre.ToString())
            {
                Debug.WriteLine("Process deny POST action callback for job with id '{0}' - finish job and unlock entity",
                    job.Id);
            }
            else if (parameters.Type == Model.CalloutDefinition.CalloutDefinitionType.Post.ToString())
            {
                Debug.WriteLine("Process deny PRE action callback for job with id '{0}' - Reset state, finish job and unlock entity",
                    job.Id);
                SetEntityState(entityUri, LoadEntity(entityUri), parameters.OriginalState);
            }
            ChangeJobState(job.Token, JobStateEnum.Canceled);
            DeleteStateChangeLockOfEntity(entityUri);
        }

        private String LoadEntity(Uri entityUri)
        {
            return _entityController.LoadEntity(entityUri);
        }

        private void DeleteStateChangeLockOfEntity(Uri entityUri)
        {
            var entityUriAsString = entityUri.ToString();
            Debug.WriteLine("Deleting StateChangeLock for entity '{0}'", entityUriAsString);
            var scl = _coreService.StateChangeLocks
                .Where(l => l.EntityId == entityUriAsString).Single();
            _coreService.DeleteObject(scl);
            _coreService.SaveChanges();
            Debug.WriteLine("StateChangeLock for entity with id '{0}' deleted", entityUriAsString);
        }

        private CalloutDefinition LoadCalloutDefinition(String condition, Uri entityUri, String tenantId, String calloutType)
        {
            Debug.WriteLine("Loading {0} callout definition of tenant '{1}' for condition '{2}' and entity of type '{3}' with id '{4}'",
                calloutType,
                tenantId,
                condition,
                _entityType,
                entityUri.ToString());
            
            return _coreService.CalloutDefinitions.Where(
                c =>
                    (c.CalloutType == calloutType
                    || c.CalloutType == Model.CalloutDefinition.CalloutDefinitionType.PreAndPost.ToString())
                    && tenantId == c.TenantId
                    && (entityUri.ToString() == c.EntityId
                    || _entityType == c.EntityType)
                    && new Regex(c.Condition).IsMatch(condition)).FirstOrDefault();
        }

        private CalloutData CreatePreCalloutData(Uri entityUri, String entity, String condition)
        {
            return CreateCalloutData(entityUri, entity, condition,
                Model.CalloutDefinition.CalloutDefinitionType.Pre.ToString());
        }

        private CalloutData CreatePostCalloutData(Uri entityUri, String entity, String condition)
        {
            return CreateCalloutData(entityUri, entity, condition,
                Model.CalloutDefinition.CalloutDefinitionType.Post.ToString());
        }

        private CalloutData CreateCalloutData(Uri entityUri, String entity, String condition, String calloutType)
        {
            var obj = JObject.Parse(entity);
            var originalState = (String)obj["State"];
            var tenantId = (String) obj["Tid"];

            return new CalloutData
            {
                Type = calloutType,
                EntityType = _entityType,
                TenantId = tenantId,
                Action = condition,
                EntityId = entityUri.ToString(),
                OriginalState = originalState,
                UserId = CurrentUserDataProvider.GetCurrentUsername(),
                CallbackUrl = CreateCallbackUrl(entityUri.ToString())
            };
        }

        private String CreateCallbackUrl(String entityUri)
        {
            StringBuilder sb = new StringBuilder();

            using (SHA256 hash = SHA256Managed.Create())
            {
                Encoding enc = Encoding.UTF8;
                Byte[] result = hash.ComputeHash(enc.GetBytes(entityUri));

                foreach (Byte b in result)
                    sb.Append(b.ToString("x2"));
            }

            var token = sb.ToString();

            return String.Format("{0}/Jobs({1})", ConfigurationManager.AppSettings["LifeCycleManager.Endpoint.Core"], token);
        }

        private void ChangeEntityState(Uri entityUri, String entity, String condition)
        {
            var obj = JObject.Parse(entity);
            var entityState = (String)obj["State"];
            _stateMachine.SetupStateMachine(_stateMachine.GetStringRepresentation(), entityState);
            var newState = _stateMachine.ChangeState(condition);
            SetEntityState(entityUri, entity, newState);
        }

        private String CreateJob(Uri entityUri, String tenantId, CalloutData calloutData)
        {
            var token = calloutData.CallbackUrl.Split('(', ')')[1];
            _coreService.AddToJobs(new Job
            {
                State = JobStateEnum.Running.ToString(),
                Type = CALLOUT_JOB_TYPE,
                Parameters = JsonConvert.SerializeObject(calloutData),
                ReferencedItemId = entityUri.ToString(),
                TenantId = tenantId,
                Token = token
            });
            _coreService.SaveChanges();
            return token;
        }

        private void ChangeJobState(String token, JobStateEnum newJobState)
        {
            var job = GetRunningJob(token);

            job.State = newJobState.ToString();
            _coreService.UpdateObject(job);
            _coreService.SaveChanges();
            Debug.WriteLine("Changed state of job with id '{0}' to '{1}'", job.Id, newJobState);
        }

        private Job GetRunningJob(String token)
        {
            return _coreService.Jobs.Where(
                    j =>
                        token == j.Token
                        && CALLOUT_JOB_TYPE == j.Type
                        && JobStateEnum.Running.ToString() == j.State).Single();
        }
    }
}
