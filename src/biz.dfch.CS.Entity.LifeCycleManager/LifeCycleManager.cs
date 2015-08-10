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
using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Loaders;
using biz.dfch.CS.Entity.LifeCycleManager.Controller;
using biz.dfch.CS.Entity.LifeCycleManager.Logging;
using biz.dfch.CS.Entity.LifeCycleManager.UserData;
using Newtonsoft.Json;

namespace biz.dfch.CS.Entity.LifeCycleManager
{
    public class LifeCycleManager
    {
        private CompositionContainer _container;

        private static Object _lock = new Object();

        [Import(typeof(IStateMachineConfigLoader))]
        private IStateMachineConfigLoader _stateMachineConfigLoader;

        private static IStateMachineConfigLoader _staticStateMachineConfigLoader = null;
        private StateMachine.StateMachine _stateMachine;
        private EntityController _entityController;

        public LifeCycleManager(ICredentialProvider credentialProvider, String entityType)
        {
            lock (_lock)
            {
                if (null == _staticStateMachineConfigLoader)
                {
                    LoadAndComposeParts();
                    _staticStateMachineConfigLoader = _stateMachineConfigLoader;
                }
                else
                {
                    _stateMachineConfigLoader = _staticStateMachineConfigLoader;
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

            _container = new CompositionContainer(assemblyCatalog);

            try
            {
                Debug.WriteLine("Composing MEF parts...");
                _container.ComposeParts(this);
                Debug.WriteLine("Composition successfully completed!");
            }
            catch (CompositionException compositionException)
            {
                Trace.WriteLine(compositionException.ToString());
            }
        }

        private void ConfigureStateMachine(String entityType)
        {
            Debug.WriteLine("Configuring state machine");
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
                    Debug.WriteLine("Error occured while parsing state machine configuraiton for entity type '{0}' : {1}",
                        entityType,
                        e.Message);
                    throw new ArgumentException("Invalid state machine configuration", e);
                }
            }
        }

        public void ChangeState(Uri entityUri, String condition)
        {
            Debug.WriteLine("Changing state for entity with Uri: '{0}' and condition: '{1}'", entityUri, condition);
            var entity = _entityController.LoadEntity(entityUri);
            // DFTODO create job of type Lifecycle (extract data from JSON)
            // DFTODO lock entity
            // DFTODO load callout definition
            // DFTODO execute Pre callout
            // DFTODO if no pre callout found -> call preCalloutCallback method
            // DFTODO on callout -> if exception: job = failed, unlock
        }

        // DFTODO preCalloutCallback: finish job, change state, persist, load callout definition and execute post callout + create new job
        // DFTODO postCalloutCallback: unlock entity
    }
}
