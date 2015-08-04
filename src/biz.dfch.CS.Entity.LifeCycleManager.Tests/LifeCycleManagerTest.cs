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
using biz.dfch.CS.Entity.LifeCycleManager.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace biz.dfch.CS.Entity.LifeCycleManager.Tests
{
    [TestClass]
    public class LifeCycleManagerTest
    {
        // DFTODO Pass entity as parameter (Maybe all entities have to inherit a BaseEntity or implement a BaseEntityInterface)
        // DFTODO Loading configuration of StateMachine for the given entity Type
        // DFTODO Handle state change transaction
            // [IF STATE LOCKED] Wait or Abort depending on flag
            // [IF STATE NOT LOCKED] Lock state change (Set property/Call method)
            // [IF STATE NOT LOCKED] Execute pre hook/callout
            // [ON/AFTER PRE CALLBACK] Execute stateChange
            // [AFTER STATE CHANGE] Execute post hook/callout
            // [ON/AFTER POST CALLBACK] Unlock

        // DFTODO job repo pluggable
        // DFTODO callout definition loader pluggable
        // DFTODO state machine config loader pluggable
        // DFTODO callback executors pluggable
        // DFTODO Loading Hook/Callout executor based on entity type, entity state and tenant information

        // DFTODO HOWTO access entity to change/revert state?

        // DFTODO job repo holds all running jobs
        // DFTODO expose controllers for callbacks
        // DFTODO implement methods 'Next', 'Continue', 'Cancel', 'ChangeState'
        
        // DFTODO Check where to load hook/callout definition from
        // DFTODO Check what's the payload of a callout (tenantId, entity, entity type, targetState)
        
        // DFTODO Transaction aware (Possibility to revert, if declined)

        // DFTODO Check the possibility of integrating the following code snippet
        /**
         * fsm.StateChange()
         * fsm.TryStateChange()
         * 
         * bool StateChange(bool WaitOnLockedState = true) { }
         * 
         * bool TryStateChange() 
         * {
         *     return this.StateChange(false);
         * }
         **/

        private static LifeCycleManager<BaseEntity> _lifeCycleManager;
            
        [ClassInitialize]
        public static void ClassInitialize()
        {
            _lifeCycleManager = new LifeCycleManager<BaseEntity>();
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
        public void LifeCycleManagerConstructorInitializesStateMachineWithDefaultConfigurationIfNoConfigurationDefinedExplicit()
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
