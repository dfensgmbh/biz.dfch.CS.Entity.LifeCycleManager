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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Web;
using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Entity;
using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Executors;
using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Loaders;
using biz.dfch.CS.Entity.LifeCycleManager.Controller;
using biz.dfch.CS.Entity.LifeCycleManager.Model;
using biz.dfch.CS.Entity.LifeCycleManager.UserData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTestExtensions;
using Telerik.JustMock;
using CalloutDefinition = biz.dfch.CS.Entity.LifeCycleManager.CumulusCoreService.CalloutDefinition;
using Job = biz.dfch.CS.Entity.LifeCycleManager.CumulusCoreService.Job;
using StateChangeLock = biz.dfch.CS.Entity.LifeCycleManager.CumulusCoreService.StateChangeLock;

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "App.config", Watch = true)]
namespace biz.dfch.CS.Entity.LifeCycleManager.Tests
{
    [TestClass]
    public class LifeCycleManagerTest
    {
        private const String CUSTOM_STATE_MACHINE_CONFIG = "{\"Created-Continue\":\"Running\",\"Created-Cancel\":\"InternalErrorState\",\"Running-Continue\":\"Completed\"}";
        private const String ENTITY_TYPE = "EntityType";
        private const String STATE_MACHINE_FIELD = "_stateMachine";
        private const String ENTITY_CONTROLLER_FIELD = "_entityController";
        private const String CORE_SERVICE_FIELD = "_coreService";
        private const String ENTITY_STATE_CREATED = "Created";
        private const String ENTITY_STATE_RUNNING = "Running";
        private const String SAMPLE_ENTITY = "{\"State\":\"" + ENTITY_STATE_CREATED + "\",\"Tid\":\"" + TENANT_ID + "\"}";
        private const String UPDATED_ENTITY = "{\"State\":\"" + ENTITY_STATE_RUNNING + "\",\"Tid\":\"" + TENANT_ID + "\"}";
        private const String CONTINUE_CONDITION = "Continue";
        private const String CALLOUT_DEFINITION = "{\"callout-url\":\"test.com/callout\"}";
        private const String CALLOUT_JOB_TYPE = "CalloutData";
        private const String SAMPLE_TOKEN = "2ea77c09068ef6406b9c51a76d59b9b7c68208ee17d0d4e19d607e0310d581f3";
        private const String TENANT_ID = "aa506000-025b-474d-b747-53b67f50d46d";
        private const String EXPECTED_PRE_CALLOUT_DATA = "{\"EntityId\":\"http://test/api/ApplicationData.svc/EntityType(1)\",\"EntityType\":\"EntityType\",\"Action\":\"Continue\",\"CallbackUrl\":\"http://test/api/Core.svc/Jobs(" + SAMPLE_TOKEN + ")\",\"UserId\":\"Administrator\",\"TenantId\":\"" + TENANT_ID + "\",\"Type\":\"Pre\",\"OriginalState\":\"Created\"}";
        private const String EXPECTED_POST_CALLOUT_DATA = "{\"EntityId\":\"http://test/api/ApplicationData.svc/EntityType(1)\",\"EntityType\":\"EntityType\",\"Action\":\"Continue\",\"CallbackUrl\":\"http://test/api/Core.svc/Jobs(" + SAMPLE_TOKEN + ")\",\"UserId\":\"Administrator\",\"TenantId\":\"" + TENANT_ID + "\",\"Type\":\"Post\",\"OriginalState\":\"Created\"}";

        private Uri SAMPLE_ENTITY_URI = new Uri("http://test/api/ApplicationData.svc/EntityType(1)");
        private Uri SAMPLE_ENTITY_URI_2 = new Uri("http://test/api/ApplicationData.svc/EntityType(2)");
        
        private CumulusCoreService.Core _coreService;
        private ICalloutExecutor _calloutExecutor;
        private IStateMachineConfigLoader _stateMachineConfigLoader;
        private IAuthenticationProvider _authenticationProvider;
        private EntityController _entityController;
        private WindowsIdentity _windowsIdentity;

        public void FixEfProviderServicesProblem()
        {
            //The Entity Framework provider type 'System.Data.Entity.SqlServer.SqlProviderServices, EntityFramework.SqlServer'
            //for the 'System.Data.SqlClient' ADO.NET provider could not be loaded. 
            //Make sure the provider assembly is available to the running application. 
            //See http://go.microsoft.com/fwlink/?LinkId=260882 for more information.

            var instance = System.Data.Entity.SqlServer.SqlProviderServices.Instance;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            Mock.SetupStatic(typeof(CurrentUserDataProvider));
            Mock.SetupStatic(typeof(HttpContext));
            Mock.SetupStatic(typeof(CredentialCache));

            _stateMachineConfigLoader = Mock.Create<IStateMachineConfigLoader>();
            _authenticationProvider = Mock.Create<IAuthenticationProvider>();
            _coreService = Mock.Create<CumulusCoreService.Core>();
            _calloutExecutor = Mock.Create<ICalloutExecutor>();
            _entityController = Mock.Create<EntityController>();
            _windowsIdentity = Mock.Create<WindowsIdentity>();

            var hashGenerator = Mock.Create<SHA256>();
            Mock.Arrange(() => hashGenerator.ComputeHash(Arg.IsAny<byte[]>()))
                .IgnoreInstance()
                .Returns(StringToByteArray(SAMPLE_TOKEN));
        }

        private byte[] StringToByteArray(String hex)
        {
            int numberChars = hex.Length;
            byte[] bytes = new byte[numberChars / 2];
            for (int i = 0; i < numberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        [TestMethod]
        [WorkItem(25)]
        public void LifeCycleMangerConstructorLoadsAndComposesMefPartsOnFirstCall()
        {
            {
                var container = Mock.Create<CompositionContainer>();
                Mock.Arrange(() => container.ComposeParts())
                    .IgnoreInstance()
                    .CallOriginal()
                    .MustBeCalled();

                var lifeCycleManager = new LifeCycleManager(_authenticationProvider, ENTITY_TYPE);
                var lifeCycleManagerWithPrivatAccess = new PrivateObject(lifeCycleManager);
                var stateMachine =
                    (StateMachine.StateMachine) lifeCycleManagerWithPrivatAccess.GetField(STATE_MACHINE_FIELD);

                Assert.IsNotNull(stateMachine);
                Assert.AreEqual(CUSTOM_STATE_MACHINE_CONFIG, stateMachine.GetStringRepresentation());
                Mock.Assert(container);
            }
            {
                var container = Mock.Create<CompositionContainer>();
                Mock.Arrange(() => container.ComposeParts())
                    .IgnoreInstance()
                    .CallOriginal()
                    .OccursNever();

                var lifeCycleManager = new LifeCycleManager(_authenticationProvider, ENTITY_TYPE);
                var lifeCycleManagerWithPrivatAccess = new PrivateObject(lifeCycleManager);
                var stateMachine = (StateMachine.StateMachine)lifeCycleManagerWithPrivatAccess.GetField(STATE_MACHINE_FIELD);

                Assert.IsNotNull(stateMachine);
                Assert.AreEqual(CUSTOM_STATE_MACHINE_CONFIG, stateMachine.GetStringRepresentation());
                Mock.Assert(container);
            }
        }

        [TestMethod]
        [WorkItem(18)]
        [Ignore]
        public void LifeCycleManagerConstructorInitializesStateMachineWithDefaultConfigurationIfNoConfigurationDefinedExplicit()
        {
            Mock.Arrange(() => _stateMachineConfigLoader.LoadConfiguration(ENTITY_TYPE))
                .IgnoreInstance()
                .Returns((String)null)
                .MustBeCalled();

            var lifeCycleManager = new LifeCycleManager(_authenticationProvider, ENTITY_TYPE);
            var lifeCycleManagerWithPrivateAccess = new PrivateObject(lifeCycleManager);
            var stateMachine = (StateMachine.StateMachine)lifeCycleManagerWithPrivateAccess.GetField(STATE_MACHINE_FIELD);

            Assert.IsNotNull(stateMachine);
            Assert.AreEqual(new StateMachine.StateMachine().GetStringRepresentation(), stateMachine.GetStringRepresentation());
            Mock.Assert(_stateMachineConfigLoader);
        }

        [TestMethod]
        [WorkItem(11)]
        public void LifeCycleManagerConstructorCallsStateMachineConfigLoaderToLoadStateMachineConfigurationAccordingEntityType()
        {
            Mock.Arrange(() => _stateMachineConfigLoader.LoadConfiguration(ENTITY_TYPE))
                .IgnoreInstance()
                .MustBeCalled();

            new LifeCycleManager(_authenticationProvider, ENTITY_TYPE);

            Mock.Assert(_stateMachineConfigLoader);
        }

        [TestMethod]
        [WorkItem(12)]
        public void LifeCycleManagerConstructorInitializesStateMachineWithLoadedConfigurationIfAvailable()
        {
            Mock.Arrange(() => _stateMachineConfigLoader.LoadConfiguration(ENTITY_TYPE))
                .IgnoreInstance()
                .Returns(CUSTOM_STATE_MACHINE_CONFIG)
                .MustBeCalled();

            var lifeCycleManager = new LifeCycleManager(_authenticationProvider, ENTITY_TYPE);
            PrivateObject lifecycleManagerWithPrivateAccess = new PrivateObject(lifeCycleManager);
            var stateMachine = (StateMachine.StateMachine)lifecycleManagerWithPrivateAccess.GetField(STATE_MACHINE_FIELD);

            Assert.IsNotNull(stateMachine);
            Assert.AreEqual(CUSTOM_STATE_MACHINE_CONFIG, stateMachine.GetStringRepresentation());
            Mock.Assert(_stateMachineConfigLoader);
        }

        [TestMethod]
        [WorkItem(12)]
        [Ignore]
        public void LifeCycleManagerConstructorLoadedInvalidStateMachineConfigurationThrowsException()
        {
            Mock.Arrange(() => _stateMachineConfigLoader.LoadConfiguration(ENTITY_TYPE))
                .IgnoreInstance()
                .Returns("Invalid state machine configuration")
                .MustBeCalled();
            
            ThrowsAssert.Throws<ArgumentException>(() => new LifeCycleManager(_authenticationProvider, ENTITY_TYPE), "Invalid state machine configuration");
            Mock.Assert(_stateMachineConfigLoader);
        }

        [TestMethod]
        public void LifeCycleManagerConstructorInitializesEntityController()
        {
            Mock.Arrange(() => _stateMachineConfigLoader.LoadConfiguration(ENTITY_TYPE))
                .IgnoreInstance()
                .Returns(CUSTOM_STATE_MACHINE_CONFIG)
                .MustBeCalled();

            var lifeCycleManager = new LifeCycleManager(_authenticationProvider, ENTITY_TYPE);
            PrivateObject lifeCycleManagerWithPrivateAccess = new PrivateObject(lifeCycleManager);
            var entityController = (EntityController)lifeCycleManagerWithPrivateAccess.GetField(ENTITY_CONTROLLER_FIELD);
            
            Assert.IsNotNull(entityController);
            Mock.Assert(_stateMachineConfigLoader);
        }

        [TestMethod]
        [WorkItem(21)]
        public void RequestStateChangeForLockedEntityThrowsInvalidOperationException()
        {
            Mock.Arrange(() => _coreService.StateChangeLocks)
                .IgnoreInstance()
                .ReturnsCollection(new List<StateChangeLock>(new List<StateChangeLock>
                {
                    CreateStateChangeLock(SAMPLE_ENTITY_URI)
                } ))
                .MustBeCalled();

            var lifeCycleManager = new LifeCycleManager(_authenticationProvider, ENTITY_TYPE);

            ThrowsAssert.Throws<InvalidOperationException>(() => lifeCycleManager.
                RequestStateChange(SAMPLE_ENTITY_URI, SAMPLE_ENTITY, CONTINUE_CONDITION, TENANT_ID));
            Mock.Assert(_coreService);
        }

        [TestMethod]
        [WorkItem(21)]
        public void RequestStateChangeForNonLockedEntityLocksEntity()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetCurrentUsername())
                .Returns("Administrator")
                .MustBeCalled();

            Mock.Arrange(() => _coreService.StateChangeLocks)
                .IgnoreInstance()
                .ReturnsCollection(new List<StateChangeLock>(new List<StateChangeLock>
                {
                    CreateStateChangeLock(SAMPLE_ENTITY_URI_2)
                }))
                .MustBeCalled();

            Mock.Arrange(() => HttpContext.Current.User.Identity)
                .Returns(_windowsIdentity)
                .MustBeCalled();

            Mock.Arrange(() => CredentialCache.DefaultCredentials)
                .MustBeCalled();

            Mock.Arrange(() => _windowsIdentity.Impersonate())
                .MustBeCalled();

            Mock.Arrange(() => _coreService.CalloutDefinitions)
                .IgnoreInstance()
                .ReturnsCollection(new List<CalloutDefinition>(new List<CalloutDefinition>
                {
                    CreateCalloutDefinition(SAMPLE_ENTITY_URI.ToString(), 
                    Model.CalloutDefinition.CalloutDefinitionType.Pre.ToString())
                }))
                .MustBeCalled();

            Mock.Arrange(() => _coreService.AddToStateChangeLocks(Arg.IsAny<StateChangeLock>()))
                .IgnoreInstance()
                .MustBeCalled();

            Mock.Arrange(() => _coreService.SaveChanges())
                .IgnoreInstance()
                .MustBeCalled();

            Mock.Arrange(() => _coreService.AddToJobs(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .MustBeCalled();

            Mock.Arrange(() => _coreService.Jobs)
                .IgnoreInstance()
                .ReturnsCollection(new List<Job>(new List<Job>{CreateJob(SAMPLE_ENTITY_URI.ToString())}))
                .MustBeCalled();

            Mock.Arrange(() => _calloutExecutor.ExecuteCallout(CALLOUT_DEFINITION, Arg.IsAny<CalloutData>()))
                .MustBeCalled();

            var lifeCycleManager = new LifeCycleManager(_authenticationProvider, ENTITY_TYPE);
            lifeCycleManager._calloutExecutor = _calloutExecutor;
            lifeCycleManager.RequestStateChange(SAMPLE_ENTITY_URI, SAMPLE_ENTITY, CONTINUE_CONDITION, TENANT_ID);

            Mock.Assert(_coreService);
            Mock.Assert(_calloutExecutor);
            Mock.Assert(() => CurrentUserDataProvider.GetCurrentUsername());
            Mock.Assert(() => HttpContext.Current.User.Identity);
            Mock.Assert(CredentialCache.DefaultCredentials);
            Mock.Assert(_windowsIdentity);
        }

        [TestMethod]
        [WorkItem(15)]
        [WorkItem(17)]
        [WorkItem(22)]
        public void RequestStateChangeForExistingPreCalloutDefinitionCreatesJobForCalloutWithCalloutDataInParameters()
        {
            Job createdJob = null;
            Mock.Arrange(() => CurrentUserDataProvider.GetCurrentUsername())
                .Returns("Administrator")
                .MustBeCalled();

            Mock.Arrange(() => _coreService.StateChangeLocks)
                .IgnoreInstance()
                .ReturnsCollection(new List<StateChangeLock>(new List<StateChangeLock>
                {
                    CreateStateChangeLock(SAMPLE_ENTITY_URI_2)
                }))
                .MustBeCalled();

            Mock.Arrange(() => HttpContext.Current.User.Identity)
                .Returns(_windowsIdentity)
                .MustBeCalled();

            Mock.Arrange(() => CredentialCache.DefaultCredentials)
                .MustBeCalled();

            Mock.Arrange(() => _windowsIdentity.Impersonate())
                .MustBeCalled();

            Mock.Arrange(() => _coreService.CalloutDefinitions)
                .IgnoreInstance()
                .ReturnsCollection(new List<CalloutDefinition>(new List<CalloutDefinition>
                {
                    CreateCalloutDefinition(SAMPLE_ENTITY_URI.ToString(), 
                    Model.CalloutDefinition.CalloutDefinitionType.Pre.ToString())
                }))
                .MustBeCalled();

            Mock.Arrange(() => _coreService.AddToStateChangeLocks(Arg.IsAny<StateChangeLock>()))
                .IgnoreInstance()
                .MustBeCalled();

            Mock.Arrange(() => _coreService.SaveChanges())
                .IgnoreInstance()
                .MustBeCalled();

            Mock.Arrange(() => _coreService.AddToJobs(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .DoInstead((Job j) => { createdJob = j; })
                .MustBeCalled();

            Mock.Arrange(() => _coreService.Jobs)
                .IgnoreInstance()
                .ReturnsCollection(new List<Job>(new List<Job> { CreateJob(SAMPLE_ENTITY_URI.ToString()) }))
                .MustBeCalled();

            Mock.Arrange(() => _calloutExecutor.ExecuteCallout(CALLOUT_DEFINITION, Arg.IsAny<CalloutData>()))
                .MustBeCalled();

            var lifeCycleManager = new LifeCycleManager(_authenticationProvider, ENTITY_TYPE);
            lifeCycleManager._calloutExecutor = _calloutExecutor;
            lifeCycleManager.RequestStateChange(SAMPLE_ENTITY_URI, SAMPLE_ENTITY, CONTINUE_CONDITION, TENANT_ID);

            Assert.AreEqual(EXPECTED_PRE_CALLOUT_DATA, createdJob.Parameters);
            Assert.AreEqual(SAMPLE_ENTITY_URI.ToString() ,createdJob.ReferencedItemId);
            Assert.AreEqual(JobStateEnum.Running.ToString(), createdJob.State);
            Assert.AreEqual(CALLOUT_JOB_TYPE ,createdJob.Type);
            Assert.AreEqual(TENANT_ID, createdJob.TenantId);

            Mock.Assert(_coreService);
            Mock.Assert(_calloutExecutor);
            Mock.Assert(() => CurrentUserDataProvider.GetCurrentUsername());
            Mock.Assert(() => HttpContext.Current.User.Identity);
            Mock.Assert(CredentialCache.DefaultCredentials);
            Mock.Assert(_windowsIdentity);
        }

        [TestMethod]
        [WorkItem(17)]
        public void RequestStateChangeForNonLockedEntityRevertsTransactionAndThrowsInvalidOperationExceptionIfPreCalloutFails()
        {
            Job updatedJob = null;
            Mock.Arrange(() => CurrentUserDataProvider.GetCurrentUsername())
                .Returns("Administrator")
                .MustBeCalled();

            Mock.Arrange(() => _coreService.StateChangeLocks)
                .IgnoreInstance()
                .ReturnsCollection(new List<StateChangeLock>(new List<StateChangeLock>
                {
                    CreateStateChangeLock(SAMPLE_ENTITY_URI_2)
                }))
                .InSequence()
                .MustBeCalled();

            Mock.Arrange(() => HttpContext.Current.User.Identity)
                .Returns(_windowsIdentity)
                .MustBeCalled();

            Mock.Arrange(() => CredentialCache.DefaultCredentials)
                .MustBeCalled();

            Mock.Arrange(() => _windowsIdentity.Impersonate())
                .MustBeCalled();

            Mock.Arrange(() => _coreService.CalloutDefinitions)
                .IgnoreInstance()
                .ReturnsCollection(new List<CalloutDefinition>(new List<CalloutDefinition>
                {
                    CreateCalloutDefinition(SAMPLE_ENTITY_URI.ToString(), 
                    Model.CalloutDefinition.CalloutDefinitionType.Pre.ToString())
                }))
                .MustBeCalled();

            Mock.Arrange(() => _coreService.AddToStateChangeLocks(Arg.IsAny<StateChangeLock>()))
                .IgnoreInstance()
                .MustBeCalled();

            Mock.Arrange(() => _coreService.SaveChanges())
                .IgnoreInstance()
                .Occurs(4);

            Mock.Arrange(() => _coreService.AddToJobs(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .MustBeCalled();

            Mock.Arrange(() => _calloutExecutor.ExecuteCallout(CALLOUT_DEFINITION, Arg.IsAny<CalloutData>()))
                .Throws<HttpRequestException>()
                .MustBeCalled();

            Mock.Arrange(() => _coreService.Jobs)
                .IgnoreInstance()
                .ReturnsCollection(new List<Job>(new List<Job> { CreateJob(SAMPLE_ENTITY_URI.ToString()) }))
                .MustBeCalled();

            Mock.Arrange(() => _coreService.UpdateObject(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .DoInstead((Job j) => { updatedJob = j; })
                .MustBeCalled();

            var stateChangeLockToBeDeleted = CreateStateChangeLock(SAMPLE_ENTITY_URI);
            Mock.Arrange(() => _coreService.StateChangeLocks)
                .IgnoreInstance()
                .ReturnsCollection(new List<StateChangeLock>(new List<StateChangeLock>
                {
                    stateChangeLockToBeDeleted
                }))
                .InSequence()
                .MustBeCalled();

            Mock.Arrange(() => _coreService.DeleteObject(stateChangeLockToBeDeleted))
                .IgnoreInstance()
                .OccursOnce();

            var lifeCycleManager = new LifeCycleManager(_authenticationProvider, ENTITY_TYPE);
            lifeCycleManager._calloutExecutor = _calloutExecutor;
            ThrowsAssert.Throws<InvalidOperationException>(() => lifeCycleManager.
                RequestStateChange(SAMPLE_ENTITY_URI, SAMPLE_ENTITY, CONTINUE_CONDITION, TENANT_ID));

            Assert.AreEqual(JobStateEnum.Failed.ToString(), updatedJob.State);

            Mock.Assert(_coreService);
            Mock.Assert(_calloutExecutor);
            Mock.Assert(() => CurrentUserDataProvider.GetCurrentUsername());
            Mock.Assert(() => HttpContext.Current.User.Identity);
            Mock.Assert(CredentialCache.DefaultCredentials);
            Mock.Assert(_windowsIdentity);
        }

        [TestMethod]
        [WorkItem(13)]
        public void RequestStateChangeForNonLockedEntityWithoutPreCalloutDefinitionChangesStateAndExecutesPostCallout()
        {
            Job createdJob = null;
            Mock.Arrange(() => CurrentUserDataProvider.GetCurrentUsername())
                .Returns("Administrator")
                .MustBeCalled();

            Mock.Arrange(() => _coreService.StateChangeLocks)
                .IgnoreInstance()
                .ReturnsCollection(new List<StateChangeLock>(new List<StateChangeLock>
                {
                    CreateStateChangeLock(SAMPLE_ENTITY_URI_2)
                }))
                .MustBeCalled();

            Mock.Arrange(() => HttpContext.Current.User.Identity)
                .Returns(_windowsIdentity)
                .MustBeCalled();

            Mock.Arrange(() => CredentialCache.DefaultCredentials)
                .MustBeCalled();

            Mock.Arrange(() => _windowsIdentity.Impersonate())
                .MustBeCalled();

            Mock.Arrange(() => _coreService.CalloutDefinitions)
                .IgnoreInstance()
                .ReturnsCollection(new List<CalloutDefinition>(new List<CalloutDefinition>
                {
                    CreateCalloutDefinition(SAMPLE_ENTITY_URI.ToString(), 
                    Model.CalloutDefinition.CalloutDefinitionType.Post.ToString())
                }))
                .MustBeCalled();

            Mock.Arrange(() => _coreService.AddToStateChangeLocks(Arg.IsAny<StateChangeLock>()))
                .IgnoreInstance()
                .MustBeCalled();

            Mock.Arrange(() => _coreService.SaveChanges())
                .IgnoreInstance()
                .MustBeCalled();

            Mock.Arrange(() => _entityController.UpdateEntity(SAMPLE_ENTITY_URI, UPDATED_ENTITY))
                .IgnoreInstance()
                .MustBeCalled();

            Mock.Arrange(() => _coreService.AddToJobs(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .DoInstead((Job j) => { createdJob = j; })
                .MustBeCalled();

            Mock.Arrange(() => _coreService.Jobs)
                .IgnoreInstance()
                .ReturnsCollection(new List<Job>(new List<Job> { CreateJob(SAMPLE_ENTITY_URI.ToString()) }))
                .MustBeCalled();

            Mock.Arrange(() => _calloutExecutor.ExecuteCallout(CALLOUT_DEFINITION, Arg.IsAny<CalloutData>()))
                .MustBeCalled();

            var lifeCycleManager = new LifeCycleManager(_authenticationProvider, ENTITY_TYPE);
            lifeCycleManager._calloutExecutor = _calloutExecutor;
            lifeCycleManager.RequestStateChange(SAMPLE_ENTITY_URI, SAMPLE_ENTITY, CONTINUE_CONDITION, TENANT_ID);

            Assert.AreEqual(EXPECTED_POST_CALLOUT_DATA, createdJob.Parameters);
            Assert.AreEqual(SAMPLE_ENTITY_URI.ToString(), createdJob.ReferencedItemId);
            Assert.AreEqual(JobStateEnum.Running.ToString(), createdJob.State);
            Assert.AreEqual(CALLOUT_JOB_TYPE, createdJob.Type);
            Assert.AreEqual(TENANT_ID, createdJob.TenantId);

            Mock.Assert(_coreService);
            Mock.Assert(_calloutExecutor);
            Mock.Assert(_entityController);
            Mock.Assert(() => CurrentUserDataProvider.GetCurrentUsername());
        }

        [TestMethod]
        [WorkItem(15)]
        [WorkItem(17)]
        [WorkItem(23)]
        public void OnAllowCallbackForRunningPreCalloutJobFinishesJobChangesEntityStateCreatesPostCalloutJobAndExecutesPostCallout()
        {
            Job updatedJob = null;
            Job createdJob = null;
            Mock.Arrange(() => CurrentUserDataProvider.GetCurrentUsername())
                .Returns("Administrator")
                .MustBeCalled();

            Mock.Arrange(() => _coreService.Jobs)
                .IgnoreInstance()
                .ReturnsCollection(new List<Job>(new List<Job> { CreateJob(SAMPLE_ENTITY_URI.ToString()) }))
                .MustBeCalled();

            Mock.Arrange(() => _coreService.UpdateObject(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .DoInstead((Job j) => { updatedJob = j; })
                .MustBeCalled();

            Mock.Arrange(() => _entityController.LoadEntity(SAMPLE_ENTITY_URI))
                .IgnoreInstance()
                .Returns(SAMPLE_ENTITY)
                .MustBeCalled();

            Mock.Arrange(() => HttpContext.Current.User.Identity)
                .Returns(_windowsIdentity)
                .MustBeCalled();

            Mock.Arrange(() => CredentialCache.DefaultCredentials)
                .MustBeCalled();

            Mock.Arrange(() => _windowsIdentity.Impersonate())
                .MustBeCalled();

            Mock.Arrange(() => _coreService.CalloutDefinitions)
                .IgnoreInstance()
                .ReturnsCollection(new List<CalloutDefinition>(new List<CalloutDefinition>
                {
                    CreateCalloutDefinition(SAMPLE_ENTITY_URI.ToString(), 
                    Model.CalloutDefinition.CalloutDefinitionType.Post.ToString())
                }))
                .MustBeCalled();

            Mock.Arrange(() => _coreService.SaveChanges())
                .IgnoreInstance()
                .Occurs(2);

            Mock.Arrange(() => _entityController.UpdateEntity(SAMPLE_ENTITY_URI, UPDATED_ENTITY))
                .IgnoreInstance()
                .MustBeCalled();

            Mock.Arrange(() => _coreService.AddToJobs(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .DoInstead((Job j) => { createdJob = j; })
                .MustBeCalled();

            Mock.Arrange(() => _calloutExecutor.ExecuteCallout(CALLOUT_DEFINITION, Arg.IsAny<CalloutData>()))
                .MustBeCalled();

            var lifeCycleManager = new LifeCycleManager(_authenticationProvider, ENTITY_TYPE);
            lifeCycleManager._calloutExecutor = _calloutExecutor;
            lifeCycleManager.OnAllowCallback(CreateJob(SAMPLE_ENTITY_URI.ToString()));

            Assert.AreEqual(JobStateEnum.Finished.ToString(), updatedJob.State);

            Assert.AreEqual(EXPECTED_POST_CALLOUT_DATA, createdJob.Parameters);
            Assert.AreEqual(SAMPLE_ENTITY_URI.ToString(), createdJob.ReferencedItemId);
            Assert.AreEqual(JobStateEnum.Running.ToString(), createdJob.State);
            Assert.AreEqual(CALLOUT_JOB_TYPE, createdJob.Type);
            Assert.AreEqual(TENANT_ID, createdJob.TenantId);

            Mock.Assert(_coreService);
            Mock.Assert(_calloutExecutor);
            Mock.Assert(_entityController);
            Mock.Assert(() => CurrentUserDataProvider.GetCurrentUsername());
            Mock.Assert(() => HttpContext.Current.User.Identity);
            Mock.Assert(CredentialCache.DefaultCredentials);
            Mock.Assert(_windowsIdentity);
        }

        [TestMethod]
        [WorkItem(24)]
        public void OnAllowCallbackForPostCalloutFinishesJobAndUnlocksEntity()
        {
            Job updatedJob = null;
            Mock.Arrange(() => _coreService.Jobs)
                .IgnoreInstance()
                .ReturnsCollection(new List<Job>(new List<Job> { CreateJob(SAMPLE_ENTITY_URI.ToString()) }))
                .MustBeCalled();

            Mock.Arrange(() => _coreService.UpdateObject(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .DoInstead((Job j) => { updatedJob = j; })
                .MustBeCalled();

            Mock.Arrange(() => _coreService.SaveChanges())
                .IgnoreInstance()
                .Occurs(2);

            var stateChangeLockToBeDeleted = CreateStateChangeLock(SAMPLE_ENTITY_URI);
            Mock.Arrange(() => _coreService.StateChangeLocks)
                .IgnoreInstance()
                .ReturnsCollection(new List<StateChangeLock>(new List<StateChangeLock>
                {
                    stateChangeLockToBeDeleted
                }))
                .InSequence()
                .MustBeCalled();

            Mock.Arrange(() => _coreService.DeleteObject(stateChangeLockToBeDeleted))
                .IgnoreInstance()
                .OccursOnce();

            var lifeCycleManager = new LifeCycleManager(_authenticationProvider, ENTITY_TYPE);
            lifeCycleManager.OnAllowCallback(CreateJob(SAMPLE_ENTITY_URI.ToString(), false));

            Assert.AreEqual(JobStateEnum.Finished.ToString(), updatedJob.State);

            Mock.Assert(_coreService);
        }

        [TestMethod]
        [WorkItem(14)]
        public void OnAllowCallbackForPreCalloutRevertsTransactionAndThrowsInvalidOperationExceptionIfPostCalloutFails()
        {
            Job createdJob = null;
            Job updatedJob = null;
            Mock.Arrange(() => CurrentUserDataProvider.GetCurrentUsername())
                .Returns("Administrator")
                .MustBeCalled();

            Mock.Arrange(() => _coreService.Jobs)
                .IgnoreInstance()
                .ReturnsCollection(new List<Job>(new List<Job> { CreateJob(SAMPLE_ENTITY_URI.ToString()) }))
                .InSequence()
                .OccursOnce();

            Mock.Arrange(() => _coreService.UpdateObject(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .InSequence()
                .OccursOnce();

            Mock.Arrange(() => _entityController.LoadEntity(SAMPLE_ENTITY_URI))
                .IgnoreInstance()
                .Returns(SAMPLE_ENTITY)
                .MustBeCalled();

            Mock.Arrange(() => HttpContext.Current.User.Identity)
                .Returns(_windowsIdentity)
                .MustBeCalled();

            Mock.Arrange(() => CredentialCache.DefaultCredentials)
                .MustBeCalled();

            Mock.Arrange(() => _windowsIdentity.Impersonate())
                .MustBeCalled();

            Mock.Arrange(() => _coreService.CalloutDefinitions)
                .IgnoreInstance()
                .ReturnsCollection(new List<CalloutDefinition>(new List<CalloutDefinition>
                {
                    CreateCalloutDefinition(SAMPLE_ENTITY_URI.ToString(), 
                    Model.CalloutDefinition.CalloutDefinitionType.Post.ToString())
                }))
                .MustBeCalled();

            Mock.Arrange(() => _coreService.SaveChanges())
                .IgnoreInstance()
                .Occurs(4);

            Mock.Arrange(() => _entityController.UpdateEntity(SAMPLE_ENTITY_URI, UPDATED_ENTITY))
                .IgnoreInstance()
                .InSequence()
                .OccursOnce();

            Mock.Arrange(() => _coreService.AddToJobs(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .DoInstead((Job j) => { createdJob = j; })
                .MustBeCalled();

            Mock.Arrange(() => _calloutExecutor.ExecuteCallout(CALLOUT_DEFINITION, Arg.IsAny<CalloutData>()))
                .Throws<HttpRequestException>()
                .MustBeCalled();

            Mock.Arrange(() => _coreService.Jobs)
                .IgnoreInstance()
                .ReturnsCollection(new List<Job>(new List<Job> { CreateJob(SAMPLE_ENTITY_URI.ToString(), false) }))
                .InSequence()
                .OccursOnce();

            Mock.Arrange(() => _coreService.UpdateObject(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .DoInstead((Job j) => { updatedJob = j; })
                .InSequence()
                .OccursOnce();

            Mock.Arrange(() => _entityController.UpdateEntity(SAMPLE_ENTITY_URI, SAMPLE_ENTITY))
                .IgnoreInstance()
                .OccursOnce();

            var stateChangeLockToBeDeleted = CreateStateChangeLock(SAMPLE_ENTITY_URI);
            Mock.Arrange(() => _coreService.StateChangeLocks)
                .IgnoreInstance()
                .ReturnsCollection(new List<StateChangeLock>(new List<StateChangeLock>
                {
                    stateChangeLockToBeDeleted
                }))
                .InSequence()
                .MustBeCalled();

            Mock.Arrange(() => _coreService.DeleteObject(stateChangeLockToBeDeleted))
                .IgnoreInstance()
                .OccursOnce();

            var lifeCycleManager = new LifeCycleManager(_authenticationProvider, ENTITY_TYPE);
            lifeCycleManager._calloutExecutor = _calloutExecutor;
            ThrowsAssert.Throws<InvalidOperationException>(() => lifeCycleManager.OnAllowCallback(CreateJob(SAMPLE_ENTITY_URI.ToString())));

            Assert.AreEqual(JobStateEnum.Failed.ToString(), updatedJob.State);

            Assert.AreEqual(EXPECTED_POST_CALLOUT_DATA, createdJob.Parameters);
            Assert.AreEqual(SAMPLE_ENTITY_URI.ToString(), createdJob.ReferencedItemId);
            Assert.AreEqual(JobStateEnum.Running.ToString(), createdJob.State);
            Assert.AreEqual(CALLOUT_JOB_TYPE, createdJob.Type);
            Assert.AreEqual(TENANT_ID, createdJob.TenantId);

            Mock.Assert(_coreService);
            Mock.Assert(_calloutExecutor);
            Mock.Assert(_entityController);
            Mock.Assert(() => CurrentUserDataProvider.GetCurrentUsername());
            Mock.Assert(() => HttpContext.Current.User.Identity);
            Mock.Assert(CredentialCache.DefaultCredentials);
            Mock.Assert(_windowsIdentity);
        }

        [TestMethod]
        [WorkItem(14)]
        public void OnAllowCallbackForPreCalloutRevertsTransactionAndThrowsInvalidOperationExceptionIfChangingStateFails()
        {
            Mock.Arrange(() => _coreService.Jobs)
                .IgnoreInstance()
                .ReturnsCollection(new List<Job>(new List<Job> { CreateJob(SAMPLE_ENTITY_URI.ToString()) }))
                .InSequence()
                .OccursOnce();

            Mock.Arrange(() => _coreService.UpdateObject(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .InSequence()
                .OccursOnce();

            Mock.Arrange(() => _entityController.LoadEntity(SAMPLE_ENTITY_URI))
                .IgnoreInstance()
                .Returns(UPDATED_ENTITY)
                .MustBeCalled();

            Mock.Arrange(() => _coreService.CalloutDefinitions)
                .IgnoreInstance()
                .ReturnsCollection(new List<CalloutDefinition>(new List<CalloutDefinition>
                {
                    CreateCalloutDefinition(SAMPLE_ENTITY_URI.ToString(), 
                    Model.CalloutDefinition.CalloutDefinitionType.Post.ToString())
                }))
                .MustBeCalled();

            Mock.Arrange(() => _coreService.SaveChanges())
                .IgnoreInstance()
                .Occurs(3);

            Mock.Arrange(() => _entityController.UpdateEntity(SAMPLE_ENTITY_URI, UPDATED_ENTITY))
                .IgnoreInstance()
                .Throws<HttpRequestException>()
                .OccursOnce();

            var stateChangeLockToBeDeleted = CreateStateChangeLock(SAMPLE_ENTITY_URI);
            Mock.Arrange(() => _coreService.StateChangeLocks)
                .IgnoreInstance()
                .ReturnsCollection(new List<StateChangeLock>(new List<StateChangeLock>
                {
                    stateChangeLockToBeDeleted
                }))
                .InSequence()
                .MustBeCalled();

            Mock.Arrange(() => _coreService.DeleteObject(stateChangeLockToBeDeleted))
                .IgnoreInstance()
                .OccursOnce();

            var lifeCycleManager = new LifeCycleManager(_authenticationProvider, ENTITY_TYPE);
            lifeCycleManager._calloutExecutor = _calloutExecutor;
            ThrowsAssert.Throws<InvalidOperationException>(() => lifeCycleManager.OnAllowCallback(CreateJob(SAMPLE_ENTITY_URI.ToString())));

            Mock.Assert(_coreService);
            Mock.Assert(_calloutExecutor);
            Mock.Assert(_entityController);
        }

        [TestMethod]
        [WorkItem(14)]
        public void OnDeclineCallbackForPreCalloutSetsJobToCanceledAndUnlocksEntity()
        {
            Job updatedJob = null;
            Mock.Arrange(() => _coreService.Jobs)
                .IgnoreInstance()
                .ReturnsCollection(new List<Job>(new List<Job> { CreateJob(SAMPLE_ENTITY_URI.ToString()) }))
                .OccursOnce();

            Mock.Arrange(() => _coreService.UpdateObject(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .DoInstead((Job j) => { updatedJob = j; })
                .OccursOnce();

            Mock.Arrange(() => _coreService.SaveChanges())
                .IgnoreInstance()
                .Occurs(2);

            var stateChangeLockToBeDeleted = CreateStateChangeLock(SAMPLE_ENTITY_URI);
            Mock.Arrange(() => _coreService.StateChangeLocks)
                .IgnoreInstance()
                .ReturnsCollection(new List<StateChangeLock>(new List<StateChangeLock>
                {
                    stateChangeLockToBeDeleted
                }))
                .InSequence()
                .MustBeCalled();

            Mock.Arrange(() => _coreService.DeleteObject(stateChangeLockToBeDeleted))
                .IgnoreInstance()
                .OccursOnce();

            var lifeCycleManager = new LifeCycleManager(_authenticationProvider, ENTITY_TYPE);
            lifeCycleManager.OnDeclineCallback(CreateJob(SAMPLE_ENTITY_URI.ToString()));

            Assert.AreEqual(JobStateEnum.Canceled.ToString(), updatedJob.State);

            Mock.Assert(_coreService);
        }

        [TestMethod]
        [WorkItem(14)]
        public void OnDeclineCallbackForPostCalloutRevertsActionSetsJobToCanceledAndUnlocksEntity()
        {
            Mock.Arrange(() => _entityController.LoadEntity(SAMPLE_ENTITY_URI))
                .IgnoreInstance()
                .Returns(UPDATED_ENTITY)
                .MustBeCalled();

            Mock.Arrange(() => _entityController.UpdateEntity(SAMPLE_ENTITY_URI, SAMPLE_ENTITY))
                .IgnoreInstance()
                .OccursOnce();

            Job updatedJob = null;
            Mock.Arrange(() => _coreService.Jobs)
                .IgnoreInstance()
                .ReturnsCollection(new List<Job>(new List<Job> { CreateJob(SAMPLE_ENTITY_URI.ToString(), false) }))
                .OccursOnce();

            Mock.Arrange(() => _coreService.UpdateObject(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .DoInstead((Job j) => { updatedJob = j; })
                .OccursOnce();

            Mock.Arrange(() => _coreService.SaveChanges())
                .IgnoreInstance()
                .Occurs(2);

            var stateChangeLockToBeDeleted = CreateStateChangeLock(SAMPLE_ENTITY_URI);
            Mock.Arrange(() => _coreService.StateChangeLocks)
                .IgnoreInstance()
                .ReturnsCollection(new List<StateChangeLock>(new List<StateChangeLock>
                {
                    stateChangeLockToBeDeleted
                }))
                .InSequence()
                .MustBeCalled();

            Mock.Arrange(() => _coreService.DeleteObject(stateChangeLockToBeDeleted))
                .IgnoreInstance()
                .OccursOnce();

            var lifeCycleManager = new LifeCycleManager(_authenticationProvider, ENTITY_TYPE);
            lifeCycleManager.OnDeclineCallback(CreateJob(SAMPLE_ENTITY_URI.ToString(), false));

            Assert.AreEqual(JobStateEnum.Canceled.ToString(), updatedJob.State);

            Mock.Assert(_coreService);
            Mock.Assert(_entityController);
        }

        private StateChangeLock CreateStateChangeLock(Uri entityUri)
        {
            return new StateChangeLock { EntityId = entityUri.ToString() };
        }

        private Job CreateJob(String entityId, Boolean preCalloutJob = true)
        {
            String parameters = preCalloutJob ? EXPECTED_PRE_CALLOUT_DATA : EXPECTED_POST_CALLOUT_DATA;

            return new Job
            {
                Id = 1,
                Type = CALLOUT_JOB_TYPE,
                ReferencedItemId = entityId,
                TenantId = TENANT_ID,
                State = JobStateEnum.Running.ToString(),
                Token = SAMPLE_TOKEN,
                Parameters = parameters
            };
        }

        private CalloutDefinition CreateCalloutDefinition(String entityId, String type)
        {
            return new CalloutDefinition { 
                EntityId = entityId,
                TenantId = TENANT_ID,
                CalloutType = type,
                Condition = "Continue",
                Parameters = CALLOUT_DEFINITION 
            };
        }
    }
}
