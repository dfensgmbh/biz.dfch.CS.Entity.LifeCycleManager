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

﻿using System;
﻿using System.ComponentModel.Composition;
﻿using System.ComponentModel.Composition.Hosting;
﻿using System.Configuration;
﻿using System.IO;
﻿using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Loaders;
﻿using biz.dfch.CS.Entity.LifeCycleManager.Credentials;

namespace biz.dfch.CS.Entity.LifeCycleManager
{
    public class LifeCycleManagerFactory
    {
        private CompositionContainer _container;

        [Import(typeof(IStateMachineConfigLoader))]
        private IStateMachineConfigLoader _stateMachineConfigLoader;

        public LifeCycleManagerFactory()
        {
            LoadAndComposeParts();
        }

        private void LoadAndComposeParts()
        {
            var assemblyCatalog = new AggregateCatalog();

            // Adds all the parts found in the given directory
            var folder = ConfigurationManager.AppSettings["LifeCycleManager.ExtensionsFolder"];
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
                // DFTODO replace with log4net!
                System.Diagnostics.Trace.WriteLine(String.Format("WARNING: Loading extensions from '{0}' FAILED.\n{1}", folder, ex.Message));
            }

            _container = new CompositionContainer(assemblyCatalog);

            try
            {
                _container.ComposeParts(this);
            }
            catch (CompositionException compositionException)
            {
                Console.WriteLine(compositionException.ToString());
            }
        }

        public LifeCycleManager CreateLifeCycleManager(ICredentialProvider credentialProvider, String entityType)
        {
            return new LifeCycleManager(_stateMachineConfigLoader, credentialProvider, entityType);
        }
    }
}
