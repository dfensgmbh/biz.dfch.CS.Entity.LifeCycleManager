﻿/**
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
using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Loaders;
using biz.dfch.CS.Entity.LifeCycleManager.Controller;
using biz.dfch.CS.Entity.LifeCycleManager.CumulusCoreService;
using biz.dfch.CS.Entity.LifeCycleManager.UserData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTestExtensions;
using Telerik.JustMock;

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
        private const String SAMPLE_ENTITY = "{}";
        private const String CONTINUE_CONDITION = "Continue";

        private Uri SAMPLE_ENTITY_URI = new Uri("http://test/api/EntityType(1)");
        private CumulusCoreService.Core _coreService;

        private IStateMachineConfigLoader _stateMachineConfigLoader;
        private ICredentialProvider _credentialProvider;

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
                    CreateStateChangeLock(new Uri("http://test/api/EntityType(2)"), ENTITY_TYPE)
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
                    CreateStateChangeLock(SAMPLE_ENTITY_URI, ENTITY_TYPE)
                }))
                .MustBeCalled();

            Mock.Arrange(() => _coreService.AddToStateChangeLocks(Arg.IsAny<StateChangeLock>()))
                .IgnoreInstance()
                .MustBeCalled();

            var lifeCycleManager = new LifeCycleManager(_credentialProvider, ENTITY_TYPE);
            lifeCycleManager.RequestStateChange(SAMPLE_ENTITY_URI, SAMPLE_ENTITY, CONTINUE_CONDITION);

            Mock.Assert(_coreService);
        }

        [TestMethod]
        [WorkItem(15)]
        public void ChangeStateForExistingPreCalloutDefinitionCreatesJobForCalloutWithCalloutDataInParameters()
        {

        }

        [TestMethod]
        [WorkItem(17)]
        [WorkItem(22)]
        public void ChangeStateForNonLockedEntityWithCalloutDefinitionConfiguresCalloutExecutorAndDoesCallout()
        {

        }

        [TestMethod]
        [WorkItem(17)]
        public void ChangeStateForNonLockedEntityThrowsInvalidOperationExceptionAndRevertsTransactionIfCalloutFails()
        {

        }

        [TestMethod]
        public void ChangeStateForNonLockedEntityWithoutPreCalloutDefinitionChangesStateAndExecutesPostCallout()
        {
            
        }

        [TestMethod]
        [WorkItem(30)]
        public void OnAllowCallbackWithNonRunningJobThrowsInvalidOperationException()
        {

        }

        [TestMethod]
        [WorkItem(30)]
        public void OnDeclineCallbackWithNonRunningJobThrowsInvalidOperationException()
        {

        }

        [TestMethod]
        [WorkItem(15)]
        public void OnAllowCallbackForRunningJobFinishesJob()
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

        }

        [TestMethod]
        [WorkItem(15)]
        public void OnAllowCallbackForPreCalloutCreatesJob()
        {
            
        }

        [TestMethod]
        [WorkItem(23)]
        public void OnAllowCallbackForPreCalloutChangesState()
        {

        }

        [TestMethod]
        [WorkItem(23)]
        [WorkItem(17)]
        public void OnAllowCallbackForPreCalloutExecutesPostCalloutExecutor()
        {

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
        }

        [TestMethod]
        [WorkItem(24)]
        public void OnAllowCallbackForPostCalloutUnlocksEntity()
        {

        }

        private StateChangeLock CreateStateChangeLock(Uri entityUri, string entityType)
        {
            return new StateChangeLock { EntityId = entityUri.ToString(), EntityType = entityType};
        }
    }
}
