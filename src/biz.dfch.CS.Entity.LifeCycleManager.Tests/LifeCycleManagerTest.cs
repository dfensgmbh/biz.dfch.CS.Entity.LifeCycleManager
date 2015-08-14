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
using System.Net.Http;
using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Entity;
using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Executors;
using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Loaders;
using biz.dfch.CS.Entity.LifeCycleManager.Controller;
using biz.dfch.CS.Entity.LifeCycleManager.Model;
using biz.dfch.CS.Entity.LifeCycleManager.UserData;
using Microsoft.Data.OData.Query.SemanticAst;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTestExtensions;
using Newtonsoft.Json;
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
        private const String ENTITY_STATE_CREATED = "Created";
        private const String ENTITY_STATE_RUNNING = "Running";
        private const String SAMPLE_ENTITY = "{\"State\":\""+ ENTITY_STATE_CREATED + "\"}";
        private const String UPDATED_ENTITY = "{\"State\":\""+ ENTITY_STATE_RUNNING + "\"}";
        private const String CONTINUE_CONDITION = "Continue";
        private const String CALLOUT_DEFINITION = "{\"callout-url\":\"test.com/callout\"}";
        private const String CALLOUT_JOB_TYPE = "CalloutData";
        private const String SAMPLE_TOKEN = "5H7l7uZ61JTRS716D498WZ6RYa53p9QA";
        private const String EXPECTED_PRE_CALLOUT_DATA = "{\"EntityId\":\"http://test/api/ApplicationData.svc/EntityType(1)\",\"EntityType\":\"EntityType\",\"Action\":\"Continue\",\"CallbackUrl\":\"http://test/api/Core.svc/Jobs(" + SAMPLE_TOKEN + ")\",\"UserId\":\"Administrator\",\"TenantId\":null,\"Type\":\"Pre\",\"OriginalState\":\"Created\"}";
        private const String EXPECTED_POST_CALLOUT_DATA = "{\"EntityId\":\"http://test/api/ApplicationData.svc/EntityType(1)\",\"EntityType\":\"EntityType\",\"Action\":\"Continue\",\"CallbackUrl\":\"http://test/api/Core.svc/Jobs(" + SAMPLE_TOKEN + ")\",\"UserId\":\"Administrator\",\"TenantId\":null,\"Type\":\"Post\",\"OriginalState\":\"Created\"}";

        private Uri SAMPLE_ENTITY_URI = new Uri("http://test/api/ApplicationData.svc/EntityType(1)");
        private Uri SAMPLE_ENTITY_URI_2 = new Uri("http://test/api/ApplicationData.svc/EntityType(2)");
        
        private CumulusCoreService.Core _coreService;
        private ICalloutExecutor _calloutExecutor;
        private IStateMachineConfigLoader _stateMachineConfigLoader;
        private ICredentialProvider _credentialProvider;
        private StateMachine.StateMachine _stateMachine;
        private EntityController _entityController;

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
        }

        [TestInitialize]
        public void TestInitialize()
        {
            _stateMachineConfigLoader = Mock.Create<IStateMachineConfigLoader>();
            _credentialProvider = Mock.Create<ICredentialProvider>();
            _coreService = Mock.Create<CumulusCoreService.Core>();
            _calloutExecutor = Mock.Create<ICalloutExecutor>();
            _stateMachine = Mock.Create<StateMachine.StateMachine>();
            _entityController = Mock.Create<EntityController>();
            Mock.SetupStatic(typeof(Convert));
            Mock.Arrange(() => Convert.ToBase64String(Arg.IsAny<byte[]>()))
                .Returns(SAMPLE_TOKEN);
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

                var lifeCycleManager = new LifeCycleManager(_credentialProvider, ENTITY_TYPE);
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

                var lifeCycleManager = new LifeCycleManager(_credentialProvider, ENTITY_TYPE);
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

            var lifeCycleManager = new LifeCycleManager(_credentialProvider, ENTITY_TYPE);
            var lifeCycleManagerWithPrivatAccess = new PrivateObject(lifeCycleManager);
            var stateMachine = (StateMachine.StateMachine)lifeCycleManagerWithPrivatAccess.GetField(STATE_MACHINE_FIELD);

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

            new LifeCycleManager(_credentialProvider, ENTITY_TYPE);

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

            var lifeCycleManager = new LifeCycleManager(_credentialProvider, ENTITY_TYPE);
            PrivateObject lifecycleManager = new PrivateObject(lifeCycleManager);
            var stateMachine = (StateMachine.StateMachine)lifecycleManager.GetField(STATE_MACHINE_FIELD);

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
            
            ThrowsAssert.Throws<ArgumentException>(() => new LifeCycleManager(_credentialProvider, ENTITY_TYPE), "Invalid state machine configuration");
            Mock.Assert(_stateMachineConfigLoader);
        }

        [TestMethod]
        public void LifeCycleManagerConstructorInitializesEntityController()
        {
            Mock.Arrange(() => _stateMachineConfigLoader.LoadConfiguration(ENTITY_TYPE))
                .IgnoreInstance()
                .Returns(CUSTOM_STATE_MACHINE_CONFIG)
                .MustBeCalled();

            var lifeCycleManager = new LifeCycleManager(_credentialProvider, ENTITY_TYPE);
            PrivateObject lifecycleManager = new PrivateObject(lifeCycleManager);
            var entityController = (EntityController)lifecycleManager.GetField(ENTITY_CONTROLLER_FIELD);
            
            Assert.IsNotNull(entityController);
        }

        [TestMethod]
        [WorkItem(21)]
        public void ChangeStateForLockedEntityThrowsInvalidOperationException()
        {
            Mock.Arrange(() => _coreService.StateChangeLocks)
                .IgnoreInstance()
                .ReturnsCollection(new List<StateChangeLock>(new List<StateChangeLock>
                {
                    CreateStateChangeLock(new Uri("http://test/api/EntityType(2)"))
                } ))
                .MustBeCalled();

            var lifeCycleManager = new LifeCycleManager(_credentialProvider, ENTITY_TYPE);

            ThrowsAssert.Throws<InvalidOperationException>(() => lifeCycleManager.RequestStateChange(SAMPLE_ENTITY_URI, SAMPLE_ENTITY, CONTINUE_CONDITION));
            Mock.Assert(_coreService);
        }

        [TestMethod]
        [WorkItem(21)]
        public void ChangeStateForNonLockedEntityLocksEntity()
        {
            Mock.Arrange(() => _coreService.StateChangeLocks)
                .IgnoreInstance()
                .ReturnsCollection(new List<StateChangeLock>(new List<StateChangeLock>
                {
                    CreateStateChangeLock(SAMPLE_ENTITY_URI_2)
                }))
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

            var lifeCycleManager = new LifeCycleManager(_credentialProvider, ENTITY_TYPE);
            lifeCycleManager._calloutExecutor = _calloutExecutor;
            lifeCycleManager.RequestStateChange(SAMPLE_ENTITY_URI, SAMPLE_ENTITY, CONTINUE_CONDITION);

            Mock.Assert(_coreService);
            Mock.Assert(_calloutExecutor);
        }

        [TestMethod]
        [WorkItem(15)]
        [WorkItem(17)]
        [WorkItem(22)]
        public void ChangeStateForExistingPreCalloutDefinitionCreatesJobForCalloutWithCalloutDataInParameters()
        {
            Job createdJob = null;
            Mock.Arrange(() => _coreService.StateChangeLocks)
                .IgnoreInstance()
                .ReturnsCollection(new List<StateChangeLock>(new List<StateChangeLock>
                {
                    CreateStateChangeLock(SAMPLE_ENTITY_URI_2)
                }))
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

            var lifeCycleManager = new LifeCycleManager(_credentialProvider, ENTITY_TYPE);
            lifeCycleManager._calloutExecutor = _calloutExecutor;
            lifeCycleManager.RequestStateChange(SAMPLE_ENTITY_URI, SAMPLE_ENTITY, CONTINUE_CONDITION);

            Assert.AreEqual(EXPECTED_PRE_CALLOUT_DATA, createdJob.Parameters);
            Assert.AreEqual(SAMPLE_ENTITY_URI.ToString() ,createdJob.ReferencedItemId);
            Assert.AreEqual(JobStateEnum.Running.ToString(), createdJob.State);
            Assert.AreEqual(CALLOUT_JOB_TYPE ,createdJob.Type);

            Mock.Assert(_coreService);
            Mock.Assert(_calloutExecutor);
        }

        [TestMethod]
        [WorkItem(17)]
        public void ChangeStateForNonLockedEntityRevertsTransactionAndThrowsInvalidOperationExceptionIfPreCalloutFails()
        {
            Job updatedJob = null;
            Mock.Arrange(() => _coreService.StateChangeLocks)
                .IgnoreInstance()
                .ReturnsCollection(new List<StateChangeLock>(new List<StateChangeLock>
                {
                    CreateStateChangeLock(SAMPLE_ENTITY_URI_2)
                }))
                .InSequence()
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
                .DoInstead((Job j) => { updatedJob = j; });

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

            var lifeCycleManager = new LifeCycleManager(_credentialProvider, ENTITY_TYPE);
            lifeCycleManager._calloutExecutor = _calloutExecutor;
            ThrowsAssert.Throws<InvalidOperationException>(() => lifeCycleManager.RequestStateChange(SAMPLE_ENTITY_URI, SAMPLE_ENTITY, CONTINUE_CONDITION));

            Assert.AreEqual(JobStateEnum.Failed.ToString(), updatedJob.State);

            Mock.Assert(_coreService);
            Mock.Assert(_calloutExecutor);
        }

        [TestMethod]
        [WorkItem(13)]
        public void ChangeStateForNonLockedEntityWithoutPreCalloutDefinitionChangesStateAndExecutesPostCallout()
        {
            Job createdJob = null;
            Mock.Arrange(() => _coreService.StateChangeLocks)
                .IgnoreInstance()
                .ReturnsCollection(new List<StateChangeLock>(new List<StateChangeLock>
                {
                    CreateStateChangeLock(SAMPLE_ENTITY_URI_2)
                }))
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

            var lifeCycleManager = new LifeCycleManager(_credentialProvider, ENTITY_TYPE);
            lifeCycleManager._calloutExecutor = _calloutExecutor;
            lifeCycleManager.RequestStateChange(SAMPLE_ENTITY_URI, SAMPLE_ENTITY, CONTINUE_CONDITION);

            Assert.AreEqual(EXPECTED_POST_CALLOUT_DATA, createdJob.Parameters);
            Assert.AreEqual(SAMPLE_ENTITY_URI.ToString(), createdJob.ReferencedItemId);
            Assert.AreEqual(JobStateEnum.Running.ToString(), createdJob.State);
            Assert.AreEqual(CALLOUT_JOB_TYPE, createdJob.Type);

            Mock.Assert(_coreService);
            Mock.Assert(_calloutExecutor);
            Mock.Assert(_entityController);
        }

        [TestMethod]
        [WorkItem(15)]
        [WorkItem(17)]
        [WorkItem(23)]
        public void OnAllowCallbackForRunningPreCalloutJobFinishesJobChangesEntityStateCreatesPostCalloutJobAndExecutesPostCallout()
        {
            Job updatedJob = null;
            Job createdJob = null;
            Mock.Arrange(() => _coreService.Jobs)
                .IgnoreInstance()
                .ReturnsCollection(new List<Job>(new List<Job> { CreateJob(SAMPLE_ENTITY_URI.ToString()) }))
                .MustBeCalled();

            Mock.Arrange(() => _coreService.UpdateObject(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .DoInstead((Job j) => { updatedJob = j; });

            Mock.Arrange(() => _entityController.LoadEntity(SAMPLE_ENTITY_URI))
                .IgnoreInstance()
                .Returns(SAMPLE_ENTITY)
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

            var lifeCycleManager = new LifeCycleManager(_credentialProvider, ENTITY_TYPE);
            lifeCycleManager._calloutExecutor = _calloutExecutor;
            lifeCycleManager.OnAllowCallback(CreateJob(SAMPLE_ENTITY_URI.ToString()));

            Assert.AreEqual(JobStateEnum.Finished.ToString(), updatedJob.State);

            Assert.AreEqual(EXPECTED_POST_CALLOUT_DATA, createdJob.Parameters);
            Assert.AreEqual(SAMPLE_ENTITY_URI.ToString(), createdJob.ReferencedItemId);
            Assert.AreEqual(JobStateEnum.Running.ToString(), createdJob.State);
            Assert.AreEqual(CALLOUT_JOB_TYPE, createdJob.Type);

            Mock.Assert(_coreService);
            Mock.Assert(_calloutExecutor);
            Mock.Assert(_entityController);
        }

        [TestMethod]
        [WorkItem(24)]
        public void OnAllowCallbackForPostCalloutFinishesJobAndUnlocksEntity()
        {

        }

        [TestMethod]
        [WorkItem(14)]
        public void OnDeclineCallbackForRunningJobCancelsJob()
        {
            
        }

        [TestMethod]
        [WorkItem(14)]
        public void OnDeclineCallbackForPostCalloutRevertsActionAndUnlocksEntity()
        {

        }

        [TestMethod]
        [WorkItem(14)]
        public void OnDeclineCallbackForPreCalloutUnlocksEntity()
        {
            // DFTODO commit resolves #14
        }

        [TestMethod]
        [WorkItem(14)]
        public void OnAllowCallbackForPreCalloutRevertsTransactionAndThrowsInvalidOperationExceptionIfCalloutFails()
        {
            // CANCEL/DELETE JOB
            // UNLOCK ENTITY
        }

        [TestMethod]
        [WorkItem(14)]
        public void OnAllowCallbackForPreCalloutRevertsTransactionAndThrowsInvalidOperationExceptionIfChangingStateFails()
        {
            // CANCEL/DELETE JOB
            // UNLOCK ENTITY
            // DFTODO commit resolves #30
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
                State = JobStateEnum.Running.ToString(),
                Token = SAMPLE_TOKEN,
                Parameters = parameters
            };
        }

        private CalloutDefinition CreateCalloutDefinition(String entityId, String type)
        {
            return new CalloutDefinition { EntityId = entityId, CalloutType = type, Parameters = CALLOUT_DEFINITION };
        }
    }
}
