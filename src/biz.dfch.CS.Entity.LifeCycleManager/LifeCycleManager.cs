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
using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Executors;
using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Loaders;
using biz.dfch.CS.Entity.LifeCycleManager.Controller;
using biz.dfch.CS.Entity.LifeCycleManager.CumulusCoreService;
using biz.dfch.CS.Entity.LifeCycleManager.Logging;
using biz.dfch.CS.Entity.LifeCycleManager.UserData;
using Newtonsoft.Json;
using Job = biz.dfch.CS.Entity.LifeCycleManager.CumulusCoreService.Job;

namespace biz.dfch.CS.Entity.LifeCycleManager
{
    // DFTODO Check how to access entitites like job, calloutDefinition, etc if the actual user is not the owner
    public class LifeCycleManager
    {
        private static Object _lock = new Object();
        private static IStateMachineConfigLoader _staticStateMachineConfigLoader = null;
        private static ICalloutExecutor _staticCalloutExecutor = null;

        private static CumulusCoreService.Core _coreService = new CumulusCoreService.Core(
            new Uri(ConfigurationManager.AppSettings["Core.Endpoint"]));

        [Import(typeof(IStateMachineConfigLoader))]
        private IStateMachineConfigLoader _stateMachineConfigLoader;

        [Import(typeof (ICalloutExecutor))]
        private ICalloutExecutor _calloutExecutor;

        private StateMachine.StateMachine _stateMachine;
        private EntityController _entityController;
        private String _entityType;

        public LifeCycleManager(ICredentialProvider credentialProvider, String entityType)
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
                }
                else
                {
                    _stateMachineConfigLoader = _staticStateMachineConfigLoader;
                    _calloutExecutor = _staticCalloutExecutor;
                }
            }
            _entityController = new EntityController(credentialProvider);
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

        // DFTODO Check where to get TenantId from to load calloutDefinition!
        // DFTODO Check how to pass credentials to service reference (Cumulus problem) -> do it with system user
        // DFTODO Logging!!!
        public void RequestStateChange(Uri entityUri, String entity, String condition)
        {
            //Debug.WriteLine("Changing state for entity with Uri: '{0}' and condition: '{1}'", entityUri, condition);
            
            //CheckForExistingLock(entityUri);
            //LockEntity(entityUri);
            //var calloutDefinition = LoadCalloutDefinition(entityUri, Model.CalloutDefinition.CalloutDefinitionType.Pre.ToString());
            //if (null == calloutDefinition)
            //{
            //    try
            //    {
            //        ChangeState();
            //        CreateJob()
            //        ExecutePostCallout();
            //    }
            //    catch (Exception e)
            //    {
                    
            //        throw
            //    }
            //}
            //else
            //{
            //    try
            //    {
            //        CreateJob(entity);
            //    }
            //    catch (Exception)
            //    {
                    
            //        throw;
            //    }
            //}
        }

        private CalloutDefinition LoadCalloutDefinition(Uri entityUri, String calloutType)
        {
            // DFTODO Extend search on tenantId
            return _coreService.CalloutDefinitions.Where(
                c =>
                    (c.CalloutType.Equals(calloutType) ||
                     c.CalloutType.Equals(Model.CalloutDefinition.CalloutDefinitionType.PreAndPost.ToString()))
                    && (entityUri.ToString().Equals(c.EntityId) || (_entityType.Equals(c.EntityType)))).FirstOrDefault();
        }

        private void LockEntity(Uri entityUri)
        {
            _coreService.AddToStateChangeLocks(
                new StateChangeLock
                {
                    EntityType = _entityType,
                    CreatedBy = CurrentUserDataProvider.GetCurrentUserId(),
                    Created = DateTimeOffset.Now,
                    EntityId = entityUri.ToString()
                }
            );
            _coreService.SaveChanges();
        }

        public void Next(Uri entityUri, string entity)
        {
            Debug.WriteLine("Next for entity with Uri: '{0}", entityUri);
            RequestStateChange(entityUri, entity, _stateMachine.ContinueCondition);
        }

        public void Cancel(Uri entityUri, string entity)
        {
            Debug.WriteLine("Cancel entity with Uri: '{0}", entityUri);
            RequestStateChange(entityUri, entity, _stateMachine.CancelCondition);
        }

        public void OnAllowCallback(Job job)
        {
            Debug.WriteLine("Callback request for job with id '{0}'", job.Id);
            // DFTODO check, if job exists
            // DFTODO preCalloutCallback: finish job, change state, persist entity, load callout definition and execute post callout + create new job
            // DFTODO postCalloutCallback: unlock entity   
        }

        public void OnDeclineCallback(Job job)
        {
            // DFTODO check, if job exists
            // DFTODO revert transaction based on params (pre/post)
            // DFTODO unlock entity
        }

        private void UnLockEntity(Uri entityUri)
        {
            var scl = _coreService.StateChangeLocks
                .Where(l => l.EntityId.Equals(entityUri.ToString())).Single();
            _coreService.DeleteObject(scl);
            _coreService.SaveChanges();
        }

        private void CheckForExistingLock(Uri entityUri)
        {
            var scl = _coreService.StateChangeLocks
               .Where(l => l.EntityId.Equals(entityUri.ToString()))
               .FirstOrDefault();

            if (null == scl)
            {
                throw new InvalidOperationException();
            }
        }
    }
}
