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
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http.OData;
using System.Web.Http.Results;
using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Entity;
using biz.dfch.CS.Entity.LifeCycleManager.Controller;
using biz.dfch.CS.Entity.LifeCycleManager.Model;
using biz.dfch.CS.Entity.LifeCycleManager.UserData;
using biz.dfch.CS.Entity.LifeCycleManager.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Telerik.JustMock;
using Job = biz.dfch.CS.Entity.LifeCycleManager.CumulusCoreService.Job;

namespace biz.dfch.CS.Entity.LifeCycleManager.Tests.Controller
{
    [TestClass]
    public class LifeCyclesControllerTest : BaseControllerTest<LifeCycle>
    {
        private LifeCyclesController _lifeCyclesController;
        private CumulusCoreService.Core _coreService;

        private const String ENTITY_ID = "http://test/api/ApplicationData.svc/Users(1)";
        private const String INVALID_ENTITY_ID = "test";
        private const String ENTITY = "{}";
        private const String CONTINUE_CONDITION = "Continue";
        private const String LIFE_CYCLE_UPDATE_PERMISSION = "CumulusCore:LifeCycleCanUpdate";
        private const String LIFE_CYCLE_NEXT_PERMISSION = "CumulusCore:LifeCycleCanNext";
        private const String LIFE_CYCLE_CANCEL_PERMISSION = "CumulusCore:LifeCycleCanCancel";
        private const String LIFE_CYCLE_ALLOW_PERMISSION = "CumulusCore:LifeCycleCanAllow";
        private const String LIFE_CYCLE_DECLINE_PERMISSION = "CumulusCore:LifeCycleCanDecline";

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            Mock.SetupStatic(typeof(ODataControllerHelper));
        }

        [TestInitialize]
        public void TestInitialize()
        {
            _lifeCyclesController = new LifeCyclesController();
            _coreService = Mock.Create<CumulusCoreService.Core>();
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
        public void PutWithoutUpdatePermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_UPDATE_PERMISSION))
                .Returns(false)
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Put(ENTITY_ID,
                new LifeCycle { Id = ENTITY_ID })
                .Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);
            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_UPDATE_PERMISSION));
        }

        [TestMethod]
        public void PutWithInvalidUriReturnsBadRequest()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_UPDATE_PERMISSION))
                .Returns(true)
                .MustBeCalled();
            
            var actionResult = _lifeCyclesController.Put(INVALID_ENTITY_ID,
                new LifeCycle { Id = INVALID_ENTITY_ID })
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(BadRequestErrorMessageResult));
            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_UPDATE_PERMISSION));
        }

        [TestMethod]
        public void PutReturnsBadRequestIfEntityCouldNotBeLoaded()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_UPDATE_PERMISSION))
                .Returns(true)
                .MustBeCalled();

            var mockedEntityController = Mock.Create<EntityController>();
            Mock.Arrange(() => mockedEntityController.LoadEntity(Arg.IsAny<Uri>()))
                .IgnoreInstance()
                .Throws<HttpRequestException>();

            var actionResult = _lifeCyclesController.Put(ENTITY_ID,
                new LifeCycle { Id = ENTITY_ID })
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(BadRequestErrorMessageResult));
            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_UPDATE_PERMISSION));
        }

        [TestMethod]
        [WorkItem(28)]
        public void PutWithValidKeyLoadsEntity()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_UPDATE_PERMISSION))
                .Returns(true)
                .MustBeCalled();

            var mockedEntityController = Mock.Create<EntityController>();
            Mock.Arrange(() => mockedEntityController.LoadEntity(Arg.IsAny<Uri>()))
                .IgnoreInstance()
                .Returns(ENTITY)
                .MustBeCalled();

            var mockedLifeCycleManager = Mock.Create<LifeCycleManager>();
            Mock.Arrange(() => mockedLifeCycleManager.RequestStateChange(new Uri(ENTITY_ID), ENTITY, CONTINUE_CONDITION))
                .IgnoreInstance()
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Put(ENTITY_ID,
                new LifeCycle
                {
                    Id = ENTITY_ID,
                    Condition = CONTINUE_CONDITION
                })
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkResult));

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_UPDATE_PERMISSION));
            Mock.Assert(mockedEntityController);
            Mock.Assert(mockedLifeCycleManager);
        }

        [TestMethod]
        public void PutWithValidKeyCreatesLifecycleManagerAndExecutesStateChange()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_UPDATE_PERMISSION))
                .Returns(true)
                .MustBeCalled();

            var mockedEntityController = Mock.Create<EntityController>();
            Mock.Arrange(() => mockedEntityController.LoadEntity(Arg.IsAny<Uri>()))
                .IgnoreInstance()
                .Returns(ENTITY)
                .MustBeCalled();

            var mockedLifeCycleManager = Mock.Create<LifeCycleManager>();
            Mock.Arrange(() => mockedLifeCycleManager.RequestStateChange(new Uri(ENTITY_ID), ENTITY, CONTINUE_CONDITION))
                .IgnoreInstance()
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Put(ENTITY_ID,
                new LifeCycle { Id = ENTITY_ID, Condition = CONTINUE_CONDITION })
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkResult));

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_UPDATE_PERMISSION));
            Mock.Assert(mockedEntityController);
            Mock.Assert(mockedLifeCycleManager);
        }

        [TestMethod]
        public void PutReturnsBadReuqestForFailingLifecycleManagerOperation()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_UPDATE_PERMISSION))
                .Returns(true)
                .MustBeCalled();

            var mockedEntityController = Mock.Create<EntityController>();
            Mock.Arrange(() => mockedEntityController.LoadEntity(Arg.IsAny<Uri>()))
                .IgnoreInstance()
                .Returns(ENTITY)
                .MustBeCalled();

            var mockedLifeCycleManager = Mock.Create<LifeCycleManager>();
            Mock.Arrange(() => mockedLifeCycleManager.RequestStateChange(new Uri(ENTITY_ID), ENTITY, CONTINUE_CONDITION))
                .IgnoreInstance()
                .Throws<InvalidOperationException>()
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Put(ENTITY_ID,
                new LifeCycle { Id = ENTITY_ID, Condition = CONTINUE_CONDITION })
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(BadRequestErrorMessageResult));

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_UPDATE_PERMISSION));
            Mock.Assert(mockedEntityController);
            Mock.Assert(mockedLifeCycleManager);
        }

        [TestMethod]
        public void PostLifeCycleByIdReturnsNotImplemented()
        {
            var actionResult = _lifeCyclesController.Post(new LifeCycle { Id = ENTITY_ID })
                .Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.NotImplemented);
        }

        [TestMethod]
        public void PatchWithoutUpdatePermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_UPDATE_PERMISSION))
                .Returns(false)
                .MustBeCalled();

            var delta = new Delta<LifeCycle>(typeof(LifeCycle));
            delta.TrySetPropertyValue("Condition", CONTINUE_CONDITION);
            var actionResult = _lifeCyclesController.Patch(INVALID_ENTITY_ID, delta)
                .Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);
            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_UPDATE_PERMISSION));
        }

        [TestMethod]
        public void PatchWithInvalidUriReturnsBadRequest()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_UPDATE_PERMISSION))
                .Returns(true)
                .MustBeCalled();

            var delta = new Delta<LifeCycle>(typeof(LifeCycle));
            delta.TrySetPropertyValue("Condition", CONTINUE_CONDITION);
            var actionResult = _lifeCyclesController.Patch(INVALID_ENTITY_ID, delta)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(BadRequestErrorMessageResult));
            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_UPDATE_PERMISSION));
        }

        [TestMethod]
        public void PatchReturnsBadRequestIfEntityCouldNotBeLoaded()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_UPDATE_PERMISSION))
                .Returns(true)
                .MustBeCalled();

            var mockedEntityController = Mock.Create<EntityController>();
            Mock.Arrange(() => mockedEntityController.LoadEntity(Arg.IsAny<Uri>()))
                .IgnoreInstance()
                .Throws<HttpRequestException>();

            var delta = new Delta<LifeCycle>(typeof(LifeCycle));
            delta.TrySetPropertyValue("Condition", CONTINUE_CONDITION);
            var actionResult = _lifeCyclesController.Patch(ENTITY_ID, delta)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(BadRequestErrorMessageResult));

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_UPDATE_PERMISSION));
        }

        [TestMethod]
        [WorkItem(28)]
        public void PatchWithValidKeyLoadsEntity()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_UPDATE_PERMISSION))
                .Returns(true)
                .MustBeCalled();

            var mockedEntityController = Mock.Create<EntityController>();
            Mock.Arrange(() => mockedEntityController.LoadEntity(Arg.IsAny<Uri>()))
                .IgnoreInstance()
                .Returns(ENTITY)
                .OccursOnce();

            var mockedLifeCycleManager = Mock.Create<LifeCycleManager>();
            Mock.Arrange(() => mockedLifeCycleManager.RequestStateChange(new Uri(ENTITY_ID), ENTITY, CONTINUE_CONDITION))
                .IgnoreInstance()
                .MustBeCalled();

            var delta = new Delta<LifeCycle>(typeof(LifeCycle));
            delta.TrySetPropertyValue("Condition", CONTINUE_CONDITION);
            var actionResult = _lifeCyclesController.Patch(ENTITY_ID, delta)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkResult));

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_UPDATE_PERMISSION));
            Mock.Assert(mockedEntityController);
            Mock.Assert(mockedLifeCycleManager);
        }

        [TestMethod]
        public void PatchWithValidKeyCreatesLifecycleManagerAndExecutesStateChange()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_UPDATE_PERMISSION))
                .Returns(true)
                .MustBeCalled();

            var mockedEntityController = Mock.Create<EntityController>();
            Mock.Arrange(() => mockedEntityController.LoadEntity(Arg.IsAny<Uri>()))
                .IgnoreInstance()
                .Returns(ENTITY)
                .MustBeCalled();

            var mockedLifeCycleManager = Mock.Create<LifeCycleManager>();
            Mock.Arrange(() => mockedLifeCycleManager.RequestStateChange(new Uri(ENTITY_ID), ENTITY, CONTINUE_CONDITION))
                .IgnoreInstance()
                .MustBeCalled();

            var delta = new Delta<LifeCycle>(typeof(LifeCycle));
            delta.TrySetPropertyValue("Condition", CONTINUE_CONDITION);
            var actionResult = _lifeCyclesController.Patch(ENTITY_ID, delta)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkResult));

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_UPDATE_PERMISSION));
            Mock.Assert(mockedEntityController);
            Mock.Assert(mockedLifeCycleManager);
        }

        [TestMethod]
        public void PatchReturnsBadReuqestForFailingLifecycleManagerOperation()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_UPDATE_PERMISSION))
                .Returns(true)
                .MustBeCalled();

            var mockedEntityController = Mock.Create<EntityController>();
            Mock.Arrange(() => mockedEntityController.LoadEntity(Arg.IsAny<Uri>()))
                .IgnoreInstance()
                .Returns(ENTITY)
                .MustBeCalled();

            var mockedLifeCycleManager = Mock.Create<LifeCycleManager>();
            Mock.Arrange(() => mockedLifeCycleManager.RequestStateChange(new Uri(ENTITY_ID), ENTITY, CONTINUE_CONDITION))
                .IgnoreInstance()
                .Throws<InvalidOperationException>()
                .MustBeCalled();

            var delta = new Delta<LifeCycle>(typeof(LifeCycle));
            delta.TrySetPropertyValue("Condition", CONTINUE_CONDITION);
            var actionResult = _lifeCyclesController.Patch(ENTITY_ID, delta)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(BadRequestErrorMessageResult));

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_UPDATE_PERMISSION));
            Mock.Assert(mockedEntityController);
            Mock.Assert(mockedLifeCycleManager);
        }

        [TestMethod]
        public void DeleteLifeCycleByIdReturnsNotImplemented()
        {
            var actionResult = _lifeCyclesController.Delete(ENTITY_ID)
                .Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.NotImplemented);
        }

        [TestMethod]
        public void NextWithoutNextPermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_NEXT_PERMISSION))
                .Returns(false)
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Next(ENTITY_ID, null)
                .Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);
            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_NEXT_PERMISSION));
        }

        [TestMethod]
        public void NextWithInvalidUriReturnsBadRequest()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_NEXT_PERMISSION))
                .Returns(true)
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Next(INVALID_ENTITY_ID, null)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(BadRequestErrorMessageResult));
            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_NEXT_PERMISSION));
        }

        [TestMethod]
        public void NextReturnsBadRequestIfEntityCouldNotBeLoaded()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_NEXT_PERMISSION))
                .Returns(true)
                .MustBeCalled();

            var mockedEntityController = Mock.Create<EntityController>();
            Mock.Arrange(() => mockedEntityController.LoadEntity(Arg.IsAny<Uri>()))
                .IgnoreInstance()
                .Throws<HttpRequestException>();

            var actionResult = _lifeCyclesController.Next(ENTITY_ID, null)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(BadRequestErrorMessageResult));

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_NEXT_PERMISSION));
        }

        [TestMethod]
        [WorkItem(28)]
        public void NextWithValidKeyLoadsEntity()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_NEXT_PERMISSION))
                .Returns(true)
                .MustBeCalled();

            var mockedEntityController = Mock.Create<EntityController>();
            Mock.Arrange(() => mockedEntityController.LoadEntity(Arg.IsAny<Uri>()))
                .IgnoreInstance()
                .Returns(ENTITY)
                .OccursOnce();

            var mockedLifeCycleManager = Mock.Create<LifeCycleManager>();
            Mock.Arrange(() => mockedLifeCycleManager.Next(new Uri(ENTITY_ID), ENTITY))
                .IgnoreInstance()
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Next(ENTITY_ID, null)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkResult));

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_NEXT_PERMISSION));
            Mock.Assert(mockedEntityController);
            Mock.Assert(mockedLifeCycleManager);
        }

        [TestMethod]
        public void NextWithValidKeyCreatesLifecycleManagerAndExecutesNextMethod()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_NEXT_PERMISSION))
                .Returns(true)
                .MustBeCalled();

            var mockedEntityController = Mock.Create<EntityController>();
            Mock.Arrange(() => mockedEntityController.LoadEntity(Arg.IsAny<Uri>()))
                .IgnoreInstance()
                .Returns(ENTITY)
                .MustBeCalled();

            var mockedLifeCycleManager = Mock.Create<LifeCycleManager>();
            Mock.Arrange(() => mockedLifeCycleManager.Next(new Uri(ENTITY_ID), ENTITY))
                .IgnoreInstance()
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Next(ENTITY_ID, null)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkResult));

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_NEXT_PERMISSION));
            Mock.Assert(mockedEntityController);
            Mock.Assert(mockedLifeCycleManager);
        }

        [TestMethod]
        public void NextReturnsBadReuqestForFailingLifecycleManagerOperation()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_NEXT_PERMISSION))
                .Returns(true)
                .MustBeCalled();

            var mockedEntityController = Mock.Create<EntityController>();
            Mock.Arrange(() => mockedEntityController.LoadEntity(Arg.IsAny<Uri>()))
                .IgnoreInstance()
                .Returns(ENTITY)
                .MustBeCalled();

            var mockedLifeCycleManager = Mock.Create<LifeCycleManager>();
            Mock.Arrange(() => mockedLifeCycleManager.Next(new Uri(ENTITY_ID), ENTITY))
                .IgnoreInstance()
                .Throws<InvalidOperationException>()
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Next(ENTITY_ID, null)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(BadRequestErrorMessageResult));

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_NEXT_PERMISSION));
            Mock.Assert(mockedEntityController);
            Mock.Assert(mockedLifeCycleManager);
        }

        [TestMethod]
        public void CancelWithoutCancelPermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_CANCEL_PERMISSION))
                .Returns(false)
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Cancel(ENTITY_ID, null)
                .Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);
            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_CANCEL_PERMISSION));
        }

        [TestMethod]
        public void CancelWithInvalidUriReturnsBadRequest()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_CANCEL_PERMISSION))
                .Returns(true)
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Cancel(INVALID_ENTITY_ID, null)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(BadRequestErrorMessageResult));
            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_CANCEL_PERMISSION));
        }

        [TestMethod]
        public void CancelReturnsBadRequestIfEntityCouldNotBeLoaded()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_CANCEL_PERMISSION))
                .Returns(true)
                .MustBeCalled();

            var mockedEntityController = Mock.Create<EntityController>();
            Mock.Arrange(() => mockedEntityController.LoadEntity(Arg.IsAny<Uri>()))
                .IgnoreInstance()
                .Throws<HttpRequestException>();

            var actionResult = _lifeCyclesController.Cancel(ENTITY_ID, null)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(BadRequestErrorMessageResult));

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_CANCEL_PERMISSION));
        }

        [TestMethod]
        [WorkItem(28)]
        public void CancelWithValidKeyLoadsEntity()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_CANCEL_PERMISSION))
                .Returns(true)
                .MustBeCalled();

            var mockedEntityController = Mock.Create<EntityController>();
            Mock.Arrange(() => mockedEntityController.LoadEntity(Arg.IsAny<Uri>()))
                .IgnoreInstance()
                .Returns(ENTITY)
                .OccursOnce();

            var mockedLifeCycleManager = Mock.Create<LifeCycleManager>();
            Mock.Arrange(() => mockedLifeCycleManager.Cancel(new Uri(ENTITY_ID), ENTITY))
                .IgnoreInstance()
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Cancel(ENTITY_ID, null)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkResult));

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_CANCEL_PERMISSION));
            Mock.Assert(mockedEntityController);
            Mock.Assert(mockedLifeCycleManager);
        }

        [TestMethod]
        public void CancelWithValidKeyCreatesLifecycleManagerAndExecutesCancelMethod()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_CANCEL_PERMISSION))
                .Returns(true)
                .MustBeCalled();

            var mockedEntityController = Mock.Create<EntityController>();
            Mock.Arrange(() => mockedEntityController.LoadEntity(Arg.IsAny<Uri>()))
                .IgnoreInstance()
                .Returns(ENTITY)
                .MustBeCalled();

            var mockedLifeCycleManager = Mock.Create<LifeCycleManager>();
            Mock.Arrange(() => mockedLifeCycleManager.Cancel(new Uri(ENTITY_ID), ENTITY))
                .IgnoreInstance()
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Cancel(ENTITY_ID, null)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkResult));

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_CANCEL_PERMISSION));
            Mock.Assert(mockedEntityController);
            Mock.Assert(mockedLifeCycleManager);
        }

        [TestMethod]
        public void CancelReturnsBadReuqestForFailingLifecycleManagerOperation()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_CANCEL_PERMISSION))
                .Returns(true)
                .MustBeCalled();

            var mockedEntityController = Mock.Create<EntityController>();
            Mock.Arrange(() => mockedEntityController.LoadEntity(Arg.IsAny<Uri>()))
                .IgnoreInstance()
                .Returns(ENTITY)
                .MustBeCalled();

            var mockedLifeCycleManager = Mock.Create<LifeCycleManager>();
            Mock.Arrange(() => mockedLifeCycleManager.Cancel(new Uri(ENTITY_ID), ENTITY))
                .IgnoreInstance()
                .Throws<InvalidOperationException>()
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Cancel(ENTITY_ID, null)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(BadRequestErrorMessageResult));

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_CANCEL_PERMISSION));
            Mock.Assert(mockedEntityController);
            Mock.Assert(mockedLifeCycleManager);
        }

        [TestMethod]
        [WorkItem(19)]
        [WorkItem(20)]
        public void AllowWithoutAllowPermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_ALLOW_PERMISSION))
                .Returns(false)
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Allow(1, null)
                .Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);
            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_ALLOW_PERMISSION));
        }

        [TestMethod]
        [WorkItem(19)]
        [WorkItem(20)]
        public void AllowWithKeyOfNonExistingJobReturnsNotFound()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_ALLOW_PERMISSION))
                .Returns(true)
                .MustBeCalled();

            Mock.Arrange(() => _coreService.Jobs)
                .IgnoreInstance()
                .ReturnsCollection(new List<Job>(new List<CumulusCoreService.Job>()))
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Allow(1, null)
                .Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.NotFound);
            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_ALLOW_PERMISSION));
            Mock.Assert(_coreService);
        }

        [TestMethod]
        [WorkItem(19)]
        [WorkItem(20)]
        public void AllowWithValidKeyCreatesLifecycleManagerAndExecutesOnAllowCallbackMethod()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_ALLOW_PERMISSION))
                .Returns(true)
                .MustBeCalled();

            Mock.Arrange(() => _coreService.Jobs)
                .IgnoreInstance()
                .ReturnsCollection(new List<Job>(new List<CumulusCoreService.Job>{ CreateSampleJob() }))
                .MustBeCalled();

            var mockedLifeCycleManager = Mock.Create<LifeCycleManager>();
            Mock.Arrange(() => mockedLifeCycleManager.OnAllowCallback(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Allow(1, null)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkResult));

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_ALLOW_PERMISSION));
            Mock.Assert(mockedLifeCycleManager);
            Mock.Assert(_coreService);
        }

        [TestMethod]
        [WorkItem(17)]
        [WorkItem(19)]
        [WorkItem(20)]
        public void AllowReturnsBadReuqestForFailingLifecycleManagerOperation()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_ALLOW_PERMISSION))
                .Returns(true)
                .MustBeCalled();

            Mock.Arrange(() => _coreService.Jobs)
                .IgnoreInstance()
                .ReturnsCollection(new List<Job>(new List<CumulusCoreService.Job> { CreateSampleJob() }))
                .MustBeCalled();

            var mockedLifeCycleManager = Mock.Create<LifeCycleManager>();
            Mock.Arrange(() => mockedLifeCycleManager.OnAllowCallback(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .Throws<InvalidOperationException>()
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Allow(1, null)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(BadRequestErrorMessageResult));

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_ALLOW_PERMISSION));
            Mock.Assert(mockedLifeCycleManager);
            Mock.Assert(_coreService);
        }

        [TestMethod]
        [WorkItem(19)]
        [WorkItem(20)]
        public void DeclineWithoutCancelPermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_DECLINE_PERMISSION))
                .Returns(false)
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Decline(1, null)
                .Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);
            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_DECLINE_PERMISSION));
        }

        [TestMethod]
        [WorkItem(19)]
        [WorkItem(20)]
        public void DeclineWithKeyOfNonExistingJobReturnsNotFound()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_DECLINE_PERMISSION))
                .Returns(true)
                .MustBeCalled();

            Mock.Arrange(() => _coreService.Jobs)
                .IgnoreInstance()
                .ReturnsCollection(new List<Job>(new List<CumulusCoreService.Job>()))
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Decline(1, null)
                .Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.NotFound);
            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_DECLINE_PERMISSION));
            Mock.Assert(_coreService);
        }

        [TestMethod]
        [WorkItem(19)]
        [WorkItem(20)]
        public void DeclinewWithValidKeyCreatesLifecycleManagerAndExecutesOnDeclineCallbackMethod()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_DECLINE_PERMISSION))
                .Returns(true)
                .MustBeCalled();

            Mock.Arrange(() => _coreService.Jobs)
                .IgnoreInstance()
                .ReturnsCollection(new List<Job>(new List<CumulusCoreService.Job>{ CreateSampleJob() }))
                .MustBeCalled();

            var mockedLifeCycleManager = Mock.Create<LifeCycleManager>();
            Mock.Arrange(() => mockedLifeCycleManager.OnDeclineCallback(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Decline(1, null)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof (OkResult));

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_DECLINE_PERMISSION));
            Mock.Assert(mockedLifeCycleManager);
            Mock.Assert(_coreService);
        }

        [TestMethod]
        [WorkItem(17)]
        [WorkItem(19)]
        [WorkItem(20)]
        public void DeclineReturnsBadReuqestForFailingLifecycleManagerOperation()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_DECLINE_PERMISSION))
                .Returns(true)
                .MustBeCalled();

            Mock.Arrange(() => _coreService.Jobs)
                .IgnoreInstance()
                .ReturnsCollection(new List<Job>(new List<CumulusCoreService.Job> { CreateSampleJob() }))
                .MustBeCalled();

            var mockedLifeCycleManager = Mock.Create<LifeCycleManager>();
            Mock.Arrange(() => mockedLifeCycleManager.OnDeclineCallback(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .Throws<InvalidOperationException>()
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Decline(1, null)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(BadRequestErrorMessageResult));

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(LIFE_CYCLE_DECLINE_PERMISSION));
            Mock.Assert(mockedLifeCycleManager);
            Mock.Assert(_coreService);
        }

        private Job CreateSampleJob()
        {
            var calloutData = new CalloutData
            {
                EntityType = "EntityType",
                EntityId = ENTITY_ID
            };

            return new Job
            {
                Id = 1,
                State = "Running",
                Parameters = JsonConvert.SerializeObject(calloutData)
            };
        }

        [TestMethod]
        public void ExtractTypeFromUriStringReturnsType()
        {
            var lifecycleControllerWithPrivateAccess = new PrivateObject(_lifeCyclesController);
            Assert.AreEqual("User", lifecycleControllerWithPrivateAccess.Invoke("ExtractTypeFromUriString", ENTITY_ID));
        }
    }
}
