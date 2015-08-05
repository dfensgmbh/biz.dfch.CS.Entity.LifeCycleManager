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
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Loaders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Telerik.JustMock;

namespace biz.dfch.CS.Entity.LifeCycleManager.Tests
{
    [TestClass]
    public class LifeCycleManagerTest
    {
        private static LifeCycleManager _lifeCycleManager;
            
        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            //_lifeCycleManager = new LifeCycleManager();
        }

        [TestMethod]
        public void LifeCycleManagerConstructorInitializesStateMachineWithDefaultConfigurationIfNoConfigurationDefinedExplicit()
        {
            //PrivateObject lifecycleManager = new PrivateObject(_lifeCycleManager);
            //var stateMachine = (StateMachine.StateMachine)lifecycleManager.GetField("_stateMachine");
            //Assert.IsNotNull(stateMachine);
            //Assert.AreEqual(new StateMachine.StateMachine().GetStringRepresentation(), stateMachine.GetStringRepresentation());
        }

        [TestMethod]
        public void LifeCycleManagerConstructorCallsStateMachineConfigLoaderToLoadStateMachineConfigurationAccordingEntityType()
        {

        }

        [TestMethod]
        public void LifeCycleManagerConstructorInitializesStateMachineWithLoadedConfigurationIfAvailable()
        {
            
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
