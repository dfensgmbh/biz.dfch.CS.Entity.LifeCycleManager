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
using System.Runtime.CompilerServices;
using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Loaders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTestExtensions;
using Newtonsoft.Json;
using Telerik.JustMock;

namespace biz.dfch.CS.Entity.LifeCycleManager.Tests
{
    [TestClass]
    public class LifeCycleManagerTest
    {
        private const String CUSTOM_STATE_MACHINE_CONFIG = "{\"Created-Continue\":\"Running\",\"Created-Cancel\":\"InternalErrorState\",\"Running-Continue\":\"Completed\"}";
        private const String ENTITY_TYPE = "EntityType";
        private const String STATE_MACHINE_FIELD = "_stateMachine";

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            
        }

        [TestMethod]
        [WorkItem(18)]
        public void LifeCycleManagerConstructorInitializesStateMachineWithDefaultConfigurationIfNoConfigurationDefinedExplicit()
        {
            var stateMachineConfigLoader = Mock.Create<IStateMachineConfigLoader>();
            Mock.Arrange(() => stateMachineConfigLoader.LoadConfiguration(Arg.AnyString)).Returns((String)null);
            var lifeCycleManager = new LifeCycleManager(stateMachineConfigLoader, ENTITY_TYPE);
            PrivateObject lifecycleManager = new PrivateObject(lifeCycleManager);
            var stateMachine = (StateMachine.StateMachine)lifecycleManager.GetField(STATE_MACHINE_FIELD);
            
            Assert.IsNotNull(stateMachine);
            Assert.AreEqual(new StateMachine.StateMachine().GetStringRepresentation(), stateMachine.GetStringRepresentation());
        }

        [TestMethod]
        [WorkItem(11)]
        public void LifeCycleManagerConstructorCallsStateMachineConfigLoaderToLoadStateMachineConfigurationAccordingEntityType()
        {
            var stateMachineConfigLoader = Mock.Create<IStateMachineConfigLoader>();
            Mock.Arrange(() => stateMachineConfigLoader.LoadConfiguration(ENTITY_TYPE)).MustBeCalled();
            new LifeCycleManager(stateMachineConfigLoader, ENTITY_TYPE);
            
            Mock.Assert(stateMachineConfigLoader);
        }

        [TestMethod]
        [WorkItem(12)]
        public void LifeCycleManagerConstructorInitializesStateMachineWithLoadedConfigurationIfAvailable()
        {
            var stateMachineConfigLoader = Mock.Create<IStateMachineConfigLoader>();
            Mock.Arrange(() => stateMachineConfigLoader.LoadConfiguration(ENTITY_TYPE)).Returns(CUSTOM_STATE_MACHINE_CONFIG);
            var lifeCycleManager = new LifeCycleManager(stateMachineConfigLoader, ENTITY_TYPE);
            PrivateObject lifecycleManager = new PrivateObject(lifeCycleManager);
            var stateMachine = (StateMachine.StateMachine)lifecycleManager.GetField(STATE_MACHINE_FIELD);

            Assert.IsNotNull(stateMachine);
            Assert.AreEqual(CUSTOM_STATE_MACHINE_CONFIG, stateMachine.GetStringRepresentation());
        }

        [TestMethod]
        public void LifeCycleManagerConstructorLoadedInvalidStateMachineConfigurationThrowsException()
        {
            var stateMachineConfigLoader = Mock.Create<IStateMachineConfigLoader>();
            Mock.Arrange(() => stateMachineConfigLoader.LoadConfiguration(ENTITY_TYPE)).Returns("Invalid state machine configuration");
            ThrowsAssert.Throws<ArgumentException>(() => new LifeCycleManager(stateMachineConfigLoader, ENTITY_TYPE), "Invalid state machine configuration");
        }

        [TestMethod]
        public void ChangeStateForLockedEntityThrowsException()
        {
            // DFTODO Define which exception should be thrown (Adjust test method name)
        }

        [TestMethod]
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
    }
}
