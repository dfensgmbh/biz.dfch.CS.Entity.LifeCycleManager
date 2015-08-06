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
using System.Runtime.CompilerServices;
using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Loaders;
using biz.dfch.CS.Entity.LifeCycleManager.Controller;
using biz.dfch.CS.Entity.LifeCycleManager.Loader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTestExtensions;
using Telerik.JustMock;

namespace biz.dfch.CS.Entity.LifeCycleManager.Tests
{
    [TestClass]
    public class LifeCycleManagerTest
    {
        private const String CUSTOM_STATE_MACHINE_CONFIG = "{\"Created-Continue\":\"Running\",\"Created-Cancel\":\"InternalErrorState\",\"Running-Continue\":\"Completed\"}";
        private const String ENTITY_TYPE = "EntityType";
        private const String STATE_MACHINE_FIELD = "_stateMachine";
        private const String ENTITY_CONTROLLER_FIELD = "_entityController";
        private Uri SAMPLE_ENTITY_URI = new Uri("http://test/api/Entity(1)");

        private IStateMachineConfigLoader _stateMachineConfigLoader;

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {

        }

        [TestInitialize]
        public void TestInitialize()
        {
            _stateMachineConfigLoader = Mock.Create<IStateMachineConfigLoader>();
        }

        [TestMethod]
        [WorkItem(18)]
        public void LifeCycleManagerConstructorInitializesStateMachineWithDefaultConfigurationIfNoConfigurationDefinedExplicit()
        {
            Mock.Arrange(() => _stateMachineConfigLoader.LoadConfiguration(Arg.AnyString)).Returns((String)null);
            var lifeCycleManager = new LifeCycleManager(_stateMachineConfigLoader, ENTITY_TYPE);
            var lifeCycleManagerWithPrivatAccess = new PrivateObject(lifeCycleManager);
            var stateMachine = (StateMachine.StateMachine)lifeCycleManagerWithPrivatAccess.GetField(STATE_MACHINE_FIELD);

            Assert.IsNotNull(stateMachine);
            Assert.AreEqual(new StateMachine.StateMachine().GetStringRepresentation(), stateMachine.GetStringRepresentation());
        }

        [TestMethod]
        [WorkItem(11)]
        public void LifeCycleManagerConstructorCallsStateMachineConfigLoaderToLoadStateMachineConfigurationAccordingEntityType()
        {
            Mock.Arrange(() => _stateMachineConfigLoader.LoadConfiguration(ENTITY_TYPE)).MustBeCalled();
            new LifeCycleManager(_stateMachineConfigLoader, ENTITY_TYPE);

            Mock.Assert(_stateMachineConfigLoader);
        }

        [TestMethod]
        [WorkItem(12)]
        public void LifeCycleManagerConstructorInitializesStateMachineWithLoadedConfigurationIfAvailable()
        {
            Mock.Arrange(() => _stateMachineConfigLoader.LoadConfiguration(ENTITY_TYPE)).Returns(CUSTOM_STATE_MACHINE_CONFIG);
            var lifeCycleManager = new LifeCycleManager(_stateMachineConfigLoader, ENTITY_TYPE);
            PrivateObject lifecycleManager = new PrivateObject(lifeCycleManager);
            var stateMachine = (StateMachine.StateMachine)lifecycleManager.GetField(STATE_MACHINE_FIELD);

            Assert.IsNotNull(stateMachine);
            Assert.AreEqual(CUSTOM_STATE_MACHINE_CONFIG, stateMachine.GetStringRepresentation());
        }

        [TestMethod]
        [WorkItem(12)]
        public void LifeCycleManagerConstructorLoadedInvalidStateMachineConfigurationThrowsException()
        {
            Mock.Arrange(() => _stateMachineConfigLoader.LoadConfiguration(ENTITY_TYPE)).Returns("Invalid state machine configuration");
            ThrowsAssert.Throws<ArgumentException>(() => new LifeCycleManager(_stateMachineConfigLoader, ENTITY_TYPE), "Invalid state machine configuration");
        }

        [TestMethod]
        [WorkItem(28)]
        public void ChangeStateCallsEntityLoader()
        {
            var entityLoader = Mock.Create<EntityController>();
            Mock.Arrange(() => entityLoader.LoadEntity(SAMPLE_ENTITY_URI)).Returns("{}").MustBeCalled();
            Mock.Arrange(() => _stateMachineConfigLoader.LoadConfiguration(Arg.AnyString)).Returns((String)null);

            var lifeCycleManager = new LifeCycleManager(_stateMachineConfigLoader, ENTITY_TYPE);
            PrivateObject lifeCycleManagerWithPrivateAccess = new PrivateObject(lifeCycleManager);
            lifeCycleManagerWithPrivateAccess.SetField(ENTITY_CONTROLLER_FIELD, entityLoader);
            lifeCycleManager.ChangeState(SAMPLE_ENTITY_URI, "Condition");

            Mock.Assert(entityLoader);
            Mock.Assert(_stateMachineConfigLoader);
        }

        [TestMethod]
        [WorkItem(21)]
        public void ChangeStateForLockedEntityThrowsException()
        {

            // DFTODO Define which exception should be thrown (Adjust test method name)
        }

        [TestMethod]
        [WorkItem(21)]
        public void ChangeStateForNonLockedEntityLocksEntity()
        {

        }

        [TestMethod]
        public void ChangeStateForNonLockedEntityCallsCalloutDefinitionLoaderToLoadPreCalloutDefinition()
        {

        }

        [TestMethod]
        public void ChangeStateForNonLockedEntityAndPreCalloutDefinitionDoesPreCallout()
        {

        }

        [TestMethod]
        public void ChangeStateForNonLockedEntityWithoutPreCalloutDefinitionChangesState()
        {

        }

        [TestMethod]
        public void ChangeStateForNonLockedEntityWithoutPreCalloutDefinitionCallsCalloutDefinitionLoaderToLoadPostCalloutDefinition()
        {

        }

        [TestMethod]
        public void ChangeStateForNonLockedEntityWithoutPreCalloutDefinitionDoesPostCallout()
        {

        }

        /**
         * For entity framework mocking see here: https://github.com/tailsu/Telerik.JustMock.EntityFramework
         **/
    }
}
