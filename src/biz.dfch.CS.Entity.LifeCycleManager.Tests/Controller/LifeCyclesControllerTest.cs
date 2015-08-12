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
using System.Net;
using System.Net.Http;
using System.Web.Http.Results;
using biz.dfch.CS.Entity.LifeCycleManager.Controller;
using biz.dfch.CS.Entity.LifeCycleManager.Model;
using biz.dfch.CS.Entity.LifeCycleManager.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Telerik.JustMock;

namespace biz.dfch.CS.Entity.LifeCycleManager.Tests.Controller
{
    [TestClass]
    public class LifeCyclesControllerTest : BaseControllerTest<LifeCycle>
    {
        private LifeCyclesController _lifeCyclesController;
        private const String ENTITY_ID = "http://test/api/ApplicationData.svc/Users(1)";
        private const String INVALID_ENTITY_ID = "test";
        private const String ENTITY = "{}";

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
        public void GetLifeCycleByIdReturnsNotImplemented()
        {
            var actionResult = _lifeCyclesController.GetLifeCycle("1",
                CreateODataQueryOptions("http://localhost/api/Core.svc/LifeCycles(1)"))
                .Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.NotImplemented);
        }

        [TestMethod]
        public void PutWithDifferentIdsInUrlAndBodyReturnsBadRequest()
        {
            var actionResult = _lifeCyclesController.Put("1",
                new LifeCycle{Id = ENTITY_ID})
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(BadRequestResult));
        }

        [TestMethod]
        public void PutWithInvalidUriReturnsBadRequest()
        {
            var actionResult = _lifeCyclesController.Put(INVALID_ENTITY_ID,
                new LifeCycle { Id = INVALID_ENTITY_ID })
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(BadRequestErrorMessageResult));
        }

        [TestMethod]
        public void PutReturnsBadRequestIfEntityCouldNotBeLoaded()
        {
            var mockedEntityController = Mock.Create<EntityController>();
            Mock.Arrange(() => mockedEntityController.LoadEntity(Arg.IsAny<Uri>()))
                .IgnoreInstance()
                .Throws<HttpRequestException>();

            var actionResult = _lifeCyclesController.Put(ENTITY_ID,
                new LifeCycle { Id = ENTITY_ID })
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(BadRequestErrorMessageResult));
        }

        [TestMethod]
        public void PutWithValidKeyLoadsEntity()
        {
            var mockedEntityController = Mock.Create<EntityController>();
            Mock.Arrange(() => mockedEntityController.LoadEntity(Arg.IsAny<Uri>()))
                .IgnoreInstance()
                .Returns(ENTITY)
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Put(ENTITY_ID,
                new LifeCycle { Id = ENTITY_ID })
                .Result;

            Mock.Assert(mockedEntityController);
        }

        // DFTODO Tests for Put

        [TestMethod]
        public void PostLifeCycleByIdReturnsNotImplemented()
        {
            var actionResult = _lifeCyclesController.Post(new LifeCycle { Id = ENTITY_ID })
                .Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.NotImplemented);
        }

        // DFTODO Tests for Patch

        [TestMethod]
        public void DeleteLifeCycleByIdReturnsNotImplemented()
        {
            var actionResult = _lifeCyclesController.Delete(ENTITY_ID)
                .Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.NotImplemented);
        }

        // DFTODO Tests for Next
        
        // DFTODO Tests for Cancel
        
        // DFTODO Tests for Allow
        
        // DFTODO Tests for Decline
    }
}
