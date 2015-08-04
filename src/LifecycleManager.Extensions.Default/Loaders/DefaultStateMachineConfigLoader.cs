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
﻿using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Loaders;

namespace LifeCycleManager.Extensions.Default.Loaders
{
    [Export(typeof(IStateMachineConfigLoader))]
    public class DefaultStateMachineConfigLoader : IStateMachineConfigLoader
    {
        public String LoadConfiguration(Type type)
        {
            // DFTODO implement default loader (in memory list or something similiar)
            throw new NotImplementedException();
        }
    }
}
