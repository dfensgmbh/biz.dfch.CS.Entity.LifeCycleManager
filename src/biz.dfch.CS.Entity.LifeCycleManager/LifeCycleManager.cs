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
using System.Linq.Expressions;
using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Loaders;
using biz.dfch.CS.Entity.LifeCycleManager.Controller;
using biz.dfch.CS.Entity.LifeCycleManager.Loader;
using Newtonsoft.Json;

namespace biz.dfch.CS.Entity.LifeCycleManager
{
    public class LifeCycleManager
    {
        private StateMachine.StateMachine _stateMachine;
        private EntityController _entityController = new EntityController();

        private IStateMachineConfigLoader _stateMachineConfigLoader;

        public LifeCycleManager(IStateMachineConfigLoader stateMachineConfigLoader, String entityType)
        {
            _stateMachine = new StateMachine.StateMachine();
            _stateMachineConfigLoader = stateMachineConfigLoader;
            var config = _stateMachineConfigLoader.LoadConfiguration(entityType);
            
            if (null != config)
            {
                try
                {
                    _stateMachine.SetupStateMachine(config);
                }
                catch (JsonReaderException)
                {
                    throw new ArgumentException("Invalid state machine configuration");
                }
            }
        }

        public void ChangeState(Uri entityUri, String condition)
        {
            var entity = _entityController.LoadEntity(entityUri);
        }
    }
}
