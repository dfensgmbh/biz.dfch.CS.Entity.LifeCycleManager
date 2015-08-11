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


using System.Net;
using biz.dfch.CS.Entity.LifeCycleManager.Context;
using biz.dfch.CS.Entity.LifeCycleManager.Controller;
using biz.dfch.CS.Entity.LifeCycleManager.Model;
using biz.dfch.CS.Entity.LifeCycleManager.UserData;
using biz.dfch.CS.Entity.LifeCycleManager.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Telerik.JustMock;

namespace biz.dfch.CS.Entity.LifeCycleManager.Tests.Controller
{
    [TestClass]
    public class LifeCyclesControllerTest : BaseControllerTest<LifeCycle>
    {
        private LifeCyclesController _lifeCyclesController;

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            Mock.SetupStatic(typeof(ODataControllerHelper));
        }

        [TestInitialize]
        public void TestInitialize()
        {
            _lifeCyclesController = new LifeCyclesController();
        }

        [TestMethod]
        public void GetLifeCyclesReturnsNotImplemented()
        {
            var actionResult = _lifeCyclesController.GetLifeCycles(
                CreateODataQueryOptions("http://localhost/api/Core.svc/LifeCycles"))
                .Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.NotImplemented);
        }

        [TestMethod]
        public void GetJobsByIdReturnsNotImplemented()
        {
            var actionResult = _lifeCyclesController.GetLifeCycle("1",
                CreateODataQueryOptions("http://localhost/api/Core.svc/LifeCycles(1)"))
                .Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.NotImplemented);
        }


    }
}
