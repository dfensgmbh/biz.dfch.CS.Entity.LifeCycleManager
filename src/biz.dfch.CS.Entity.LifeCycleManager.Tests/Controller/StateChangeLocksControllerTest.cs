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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
﻿using biz.dfch.CS.Entity.LifeCycleManager.Context;
﻿using biz.dfch.CS.Entity.LifeCycleManager.Controller;
﻿using biz.dfch.CS.Entity.LifeCycleManager.UserData;
﻿using biz.dfch.CS.Entity.LifeCycleManager.Util;
﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
﻿using Telerik.JustMock;

namespace biz.dfch.CS.Entity.LifeCycleManager.Tests.Controller
{
    public class StateChangeLocksControllerTest
    {
        private StateChangeLocksController _stateChangeLocksController;
        private LifeCycleContext _lifeCycleContext;

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            Mock.SetupStatic(typeof(ODataControllerHelper));
            Mock.SetupStatic(typeof(CurrentUserDataProvider));
        }

        [TestInitialize]
        public void TestInitialize()
        {
            _stateChangeLocksController = new StateChangeLocksController();
            _lifeCycleContext = Mock.Create<LifeCycleContext>();
        }
    }
}
