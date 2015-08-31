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
using System.Web;
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
        private const String CALLOUT_JOB_TYPE = "CalloutData";
        private const String LIFE_CYCLE_UPDATE_PERMISSION = "LightSwitchApplication:LifeCycleCanUpdate";
        private const String LIFE_CYCLE_NEXT_PERMISSION = "LightSwitchApplication:LifeCycleCanNext";
        private const String LIFE_CYCLE_CANCEL_PERMISSION = "LightSwitchApplication:LifeCycleCanCancel";
        private const String LIFE_CYCLE_ALLOW_PERMISSION = "LightSwitchApplication:LifeCycleCanAllow";
        private const String LIFE_CYCLE_DECLINE_PERMISSION = "LightSwitchApplication:LifeCycleCanDecline";

        [TestInitialize]
        public void TestInitialize()
        {
            Mock.SetupStatic(typeof(ODataControllerHelper));
            Mock.SetupStatic(typeof(CurrentUserDataProvider));
            Mock.SetupStatic(typeof(HttpContext));

            Mock.Arrange(() => HttpContext.Current.Request.Headers.Get(TENANT_ID_HEADER_KEY))
                .Returns(TENANT_ID)
                .OccursOnce();

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
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String>(),
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Put(ENTITY_ID,
                new LifeCycle { Id = ENTITY_ID })
                .Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);
            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
        }

        [TestMethod]
        public void PutWithInvalidUriReturnsBadRequest()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { LIFE_CYCLE_UPDATE_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            
            var actionResult = _lifeCyclesController.Put(INVALID_ENTITY_ID,
                new LifeCycle { Id = INVALID_ENTITY_ID })
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(BadRequestErrorMessageResult));
            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
        }

        [TestMethod]
        public void PutReturnsBadRequestIfEntityCouldNotBeLoaded()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { LIFE_CYCLE_UPDATE_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();

            var mockedEntityController = Mock.Create<EntityController>();
            Mock.Arrange(() => mockedEntityController.LoadEntity(Arg.IsAny<Uri>()))
                .IgnoreInstance()
                .Throws<HttpRequestException>();

            var actionResult = _lifeCyclesController.Put(ENTITY_ID,
                new LifeCycle { Id = ENTITY_ID })
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(BadRequestErrorMessageResult));
            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
        }

        [TestMethod]
        [WorkItem(28)]
        public void PutWithValidKeyLoadsEntity()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { LIFE_CYCLE_UPDATE_PERMISSION },
                    Username = CURRENT_USER_ID,
                    Tid = TENANT_ID
                })
                .MustBeCalled();

            var mockedEntityController = Mock.Create<EntityController>();
            Mock.Arrange(() => mockedEntityController.LoadEntity(Arg.IsAny<Uri>()))
                .IgnoreInstance()
                .Returns(ENTITY)
                .MustBeCalled();

            var mockedLifeCycleManager = Mock.Create<LifeCycleManager>();
            Mock.Arrange(() => mockedLifeCycleManager.RequestStateChange(new Uri(ENTITY_ID), ENTITY, CONTINUE_CONDITION, TENANT_ID))
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

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
            Mock.Assert(mockedEntityController);
            Mock.Assert(mockedLifeCycleManager);
        }

        [TestMethod]
        public void PutWithValidKeyCreatesLifecycleManagerAndExecutesStateChange()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { LIFE_CYCLE_UPDATE_PERMISSION },
                    Username = CURRENT_USER_ID,
                    Tid = TENANT_ID
                })
                .MustBeCalled();

            var mockedEntityController = Mock.Create<EntityController>();
            Mock.Arrange(() => mockedEntityController.LoadEntity(Arg.IsAny<Uri>()))
                .IgnoreInstance()
                .Returns(ENTITY)
                .MustBeCalled();

            var mockedLifeCycleManager = Mock.Create<LifeCycleManager>();
            Mock.Arrange(() => mockedLifeCycleManager.RequestStateChange(new Uri(ENTITY_ID), ENTITY, CONTINUE_CONDITION, TENANT_ID))
                .IgnoreInstance()
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Put(ENTITY_ID,
                new LifeCycle { Id = ENTITY_ID, Condition = CONTINUE_CONDITION })
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkResult));

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
            Mock.Assert(mockedEntityController);
            Mock.Assert(mockedLifeCycleManager);
        }

        [TestMethod]
        public void PutReturnsBadReuqestForFailingLifecycleManagerOperation()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { LIFE_CYCLE_UPDATE_PERMISSION },
                    Username = CURRENT_USER_ID,
                    Tid = TENANT_ID
                })
                .MustBeCalled();

            var mockedEntityController = Mock.Create<EntityController>();
            Mock.Arrange(() => mockedEntityController.LoadEntity(Arg.IsAny<Uri>()))
                .IgnoreInstance()
                .Returns(ENTITY)
                .MustBeCalled();

            var mockedLifeCycleManager = Mock.Create<LifeCycleManager>();
            Mock.Arrange(() => mockedLifeCycleManager.RequestStateChange(new Uri(ENTITY_ID), ENTITY, CONTINUE_CONDITION, TENANT_ID))
                .IgnoreInstance()
                .Throws<InvalidOperationException>()
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Put(ENTITY_ID,
                new LifeCycle { Id = ENTITY_ID, Condition = CONTINUE_CONDITION })
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(BadRequestErrorMessageResult));

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
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
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String>(),
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();

            var delta = new Delta<LifeCycle>(typeof(LifeCycle));
            delta.TrySetPropertyValue("Condition", CONTINUE_CONDITION);
            var actionResult = _lifeCyclesController.Patch(INVALID_ENTITY_ID, delta)
                .Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);
            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
        }

        [TestMethod]
        public void PatchWithInvalidUriReturnsBadRequest()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { LIFE_CYCLE_UPDATE_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();

            var delta = new Delta<LifeCycle>(typeof(LifeCycle));
            delta.TrySetPropertyValue("Condition", CONTINUE_CONDITION);
            var actionResult = _lifeCyclesController.Patch(INVALID_ENTITY_ID, delta)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(BadRequestErrorMessageResult));
            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
        }

        [TestMethod]
        public void PatchReturnsBadRequestIfEntityCouldNotBeLoaded()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { LIFE_CYCLE_UPDATE_PERMISSION },
                    Username = CURRENT_USER_ID
                })
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

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
        }

        [TestMethod]
        [WorkItem(28)]
        public void PatchWithValidKeyLoadsEntity()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { LIFE_CYCLE_UPDATE_PERMISSION },
                    Username = CURRENT_USER_ID,
                    Tid = TENANT_ID
                })
                .MustBeCalled();

            var mockedEntityController = Mock.Create<EntityController>();
            Mock.Arrange(() => mockedEntityController.LoadEntity(Arg.IsAny<Uri>()))
                .IgnoreInstance()
                .Returns(ENTITY)
                .OccursOnce();

            var mockedLifeCycleManager = Mock.Create<LifeCycleManager>();
            Mock.Arrange(() => mockedLifeCycleManager.RequestStateChange(new Uri(ENTITY_ID), ENTITY, CONTINUE_CONDITION, TENANT_ID))
                .IgnoreInstance()
                .MustBeCalled();

            var delta = new Delta<LifeCycle>(typeof(LifeCycle));
            delta.TrySetPropertyValue("Condition", CONTINUE_CONDITION);
            var actionResult = _lifeCyclesController.Patch(ENTITY_ID, delta)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkResult));

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
            Mock.Assert(mockedEntityController);
            Mock.Assert(mockedLifeCycleManager);
        }

        [TestMethod]
        public void PatchWithValidKeyCreatesLifecycleManagerAndExecutesStateChange()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { LIFE_CYCLE_UPDATE_PERMISSION },
                    Username = CURRENT_USER_ID,
                    Tid = TENANT_ID
                })
                .MustBeCalled();

            var mockedEntityController = Mock.Create<EntityController>();
            Mock.Arrange(() => mockedEntityController.LoadEntity(Arg.IsAny<Uri>()))
                .IgnoreInstance()
                .Returns(ENTITY)
                .MustBeCalled();

            var mockedLifeCycleManager = Mock.Create<LifeCycleManager>();
            Mock.Arrange(() => mockedLifeCycleManager.RequestStateChange(new Uri(ENTITY_ID), ENTITY, CONTINUE_CONDITION, TENANT_ID))
                .IgnoreInstance()
                .MustBeCalled();

            var delta = new Delta<LifeCycle>(typeof(LifeCycle));
            delta.TrySetPropertyValue("Condition", CONTINUE_CONDITION);
            var actionResult = _lifeCyclesController.Patch(ENTITY_ID, delta)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkResult));

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
            Mock.Assert(mockedEntityController);
            Mock.Assert(mockedLifeCycleManager);
        }

        [TestMethod]
        public void PatchReturnsBadReuqestForFailingLifecycleManagerOperation()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { LIFE_CYCLE_UPDATE_PERMISSION },
                    Username = CURRENT_USER_ID,
                    Tid = TENANT_ID
                })
                .MustBeCalled();

            var mockedEntityController = Mock.Create<EntityController>();
            Mock.Arrange(() => mockedEntityController.LoadEntity(Arg.IsAny<Uri>()))
                .IgnoreInstance()
                .Returns(ENTITY)
                .MustBeCalled();

            var mockedLifeCycleManager = Mock.Create<LifeCycleManager>();
            Mock.Arrange(() => mockedLifeCycleManager.RequestStateChange(new Uri(ENTITY_ID), ENTITY, CONTINUE_CONDITION, TENANT_ID))
                .IgnoreInstance()
                .Throws<InvalidOperationException>()
                .MustBeCalled();

            var delta = new Delta<LifeCycle>(typeof(LifeCycle));
            delta.TrySetPropertyValue("Condition", CONTINUE_CONDITION);
            var actionResult = _lifeCyclesController.Patch(ENTITY_ID, delta)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(BadRequestErrorMessageResult));

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
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
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String>(),
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Next(ENTITY_ID, null)
                .Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);
            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
        }

        [TestMethod]
        public void NextWithInvalidUriReturnsBadRequest()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { LIFE_CYCLE_NEXT_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Next(INVALID_ENTITY_ID, null)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(BadRequestErrorMessageResult));
            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
        }

        [TestMethod]
        public void NextReturnsBadRequestIfEntityCouldNotBeLoaded()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { LIFE_CYCLE_NEXT_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();

            var mockedEntityController = Mock.Create<EntityController>();
            Mock.Arrange(() => mockedEntityController.LoadEntity(Arg.IsAny<Uri>()))
                .IgnoreInstance()
                .Throws<HttpRequestException>();

            var actionResult = _lifeCyclesController.Next(ENTITY_ID, null)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(BadRequestErrorMessageResult));

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
        }

        [TestMethod]
        [WorkItem(28)]
        public void NextWithValidKeyLoadsEntity()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { LIFE_CYCLE_NEXT_PERMISSION },
                    Username = CURRENT_USER_ID,
                    Tid = TENANT_ID
                })
                .MustBeCalled();

            var mockedEntityController = Mock.Create<EntityController>();
            Mock.Arrange(() => mockedEntityController.LoadEntity(Arg.IsAny<Uri>()))
                .IgnoreInstance()
                .Returns(ENTITY)
                .OccursOnce();

            var mockedLifeCycleManager = Mock.Create<LifeCycleManager>();
            Mock.Arrange(() => mockedLifeCycleManager.Next(new Uri(ENTITY_ID), ENTITY, TENANT_ID))
                .IgnoreInstance()
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Next(ENTITY_ID, null)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkResult));

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
            Mock.Assert(mockedEntityController);
            Mock.Assert(mockedLifeCycleManager);
        }

        [TestMethod]
        public void NextWithValidKeyCreatesLifecycleManagerAndExecutesNextMethod()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { LIFE_CYCLE_NEXT_PERMISSION },
                    Username = CURRENT_USER_ID,
                    Tid = TENANT_ID
                })
                .MustBeCalled();

            var mockedEntityController = Mock.Create<EntityController>();
            Mock.Arrange(() => mockedEntityController.LoadEntity(Arg.IsAny<Uri>()))
                .IgnoreInstance()
                .Returns(ENTITY)
                .MustBeCalled();

            var mockedLifeCycleManager = Mock.Create<LifeCycleManager>();
            Mock.Arrange(() => mockedLifeCycleManager.Next(new Uri(ENTITY_ID), ENTITY, TENANT_ID))
                .IgnoreInstance()
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Next(ENTITY_ID, null)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkResult));

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
            Mock.Assert(mockedEntityController);
            Mock.Assert(mockedLifeCycleManager);
        }

        [TestMethod]
        public void NextReturnsBadReuqestForFailingLifecycleManagerOperation()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { LIFE_CYCLE_NEXT_PERMISSION },
                    Username = CURRENT_USER_ID,
                    Tid = TENANT_ID
                })
                .MustBeCalled();

            var mockedEntityController = Mock.Create<EntityController>();
            Mock.Arrange(() => mockedEntityController.LoadEntity(Arg.IsAny<Uri>()))
                .IgnoreInstance()
                .Returns(ENTITY)
                .MustBeCalled();

            var mockedLifeCycleManager = Mock.Create<LifeCycleManager>();
            Mock.Arrange(() => mockedLifeCycleManager.Next(new Uri(ENTITY_ID), ENTITY, TENANT_ID))
                .IgnoreInstance()
                .Throws<InvalidOperationException>()
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Next(ENTITY_ID, null)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(BadRequestErrorMessageResult));

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
            Mock.Assert(mockedEntityController);
            Mock.Assert(mockedLifeCycleManager);
        }

        [TestMethod]
        public void CancelWithoutCancelPermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String>(),
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Cancel(ENTITY_ID, null)
                .Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);
            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
        }

        [TestMethod]
        public void CancelWithInvalidUriReturnsBadRequest()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { LIFE_CYCLE_CANCEL_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Cancel(INVALID_ENTITY_ID, null)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(BadRequestErrorMessageResult));
            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
        }

        [TestMethod]
        public void CancelReturnsBadRequestIfEntityCouldNotBeLoaded()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { LIFE_CYCLE_CANCEL_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();

            var mockedEntityController = Mock.Create<EntityController>();
            Mock.Arrange(() => mockedEntityController.LoadEntity(Arg.IsAny<Uri>()))
                .IgnoreInstance()
                .Throws<HttpRequestException>();

            var actionResult = _lifeCyclesController.Cancel(ENTITY_ID, null)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(BadRequestErrorMessageResult));

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
        }

        [TestMethod]
        [WorkItem(28)]
        public void CancelWithValidKeyLoadsEntity()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { LIFE_CYCLE_CANCEL_PERMISSION },
                    Username = CURRENT_USER_ID,
                    Tid = TENANT_ID
                })
                .MustBeCalled();

            var mockedEntityController = Mock.Create<EntityController>();
            Mock.Arrange(() => mockedEntityController.LoadEntity(Arg.IsAny<Uri>()))
                .IgnoreInstance()
                .Returns(ENTITY)
                .OccursOnce();

            var mockedLifeCycleManager = Mock.Create<LifeCycleManager>();
            Mock.Arrange(() => mockedLifeCycleManager.Cancel(new Uri(ENTITY_ID), ENTITY, TENANT_ID))
                .IgnoreInstance()
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Cancel(ENTITY_ID, null)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkResult));

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
            Mock.Assert(mockedEntityController);
            Mock.Assert(mockedLifeCycleManager);
        }

        [TestMethod]
        public void CancelWithValidKeyCreatesLifecycleManagerAndExecutesCancelMethod()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { LIFE_CYCLE_CANCEL_PERMISSION },
                    Username = CURRENT_USER_ID,
                    Tid = TENANT_ID
                })
                .MustBeCalled();

            var mockedEntityController = Mock.Create<EntityController>();
            Mock.Arrange(() => mockedEntityController.LoadEntity(Arg.IsAny<Uri>()))
                .IgnoreInstance()
                .Returns(ENTITY)
                .MustBeCalled();

            var mockedLifeCycleManager = Mock.Create<LifeCycleManager>();
            Mock.Arrange(() => mockedLifeCycleManager.Cancel(new Uri(ENTITY_ID), ENTITY, TENANT_ID))
                .IgnoreInstance()
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Cancel(ENTITY_ID, null)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkResult));

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
            Mock.Assert(mockedEntityController);
            Mock.Assert(mockedLifeCycleManager);
        }

        [TestMethod]
        public void CancelReturnsBadReuqestForFailingLifecycleManagerOperation()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { LIFE_CYCLE_CANCEL_PERMISSION },
                    Username = CURRENT_USER_ID,
                    Tid = TENANT_ID
                })
                .MustBeCalled();

            var mockedEntityController = Mock.Create<EntityController>();
            Mock.Arrange(() => mockedEntityController.LoadEntity(Arg.IsAny<Uri>()))
                .IgnoreInstance()
                .Returns(ENTITY)
                .MustBeCalled();

            var mockedLifeCycleManager = Mock.Create<LifeCycleManager>();
            Mock.Arrange(() => mockedLifeCycleManager.Cancel(new Uri(ENTITY_ID), ENTITY, TENANT_ID))
                .IgnoreInstance()
                .Throws<InvalidOperationException>()
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Cancel(ENTITY_ID, null)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(BadRequestErrorMessageResult));

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
            Mock.Assert(mockedEntityController);
            Mock.Assert(mockedLifeCycleManager);
        }

        [TestMethod]
        [WorkItem(19)]
        [WorkItem(20)]
        public void AllowWithoutAllowPermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String>(),
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Allow(SAMPLE_TOKEN, null)
                .Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);
            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
        }

        [TestMethod]
        [WorkItem(19)]
        [WorkItem(20)]
        [WorkItem(30)]
        public void AllowWithTokenOfNonExistingJobReturnsNotFound()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { LIFE_CYCLE_ALLOW_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();

            Mock.Arrange(() => _coreService.Jobs)
                .IgnoreInstance()
                .ReturnsCollection(new List<Job>(new List<CumulusCoreService.Job>()))
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Allow(SAMPLE_TOKEN, null)
                .Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.NotFound);
            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
            Mock.Assert(_coreService);
        }

        [TestMethod]
        [WorkItem(19)]
        [WorkItem(20)]
        public void AllowWithValidTokenCreatesLifecycleManagerAndExecutesOnAllowCallbackMethod()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { LIFE_CYCLE_ALLOW_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();

            Mock.Arrange(() => _coreService.Jobs)
                .IgnoreInstance()
                .ReturnsCollection(new List<Job>(new List<CumulusCoreService.Job>{ CreateSampleJob() }))
                .MustBeCalled();

            var mockedLifeCycleManager = Mock.Create<LifeCycleManager>();
            Mock.Arrange(() => mockedLifeCycleManager.OnAllowCallback(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Allow(SAMPLE_TOKEN, null)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkResult));

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
            Mock.Assert(mockedLifeCycleManager);
            Mock.Assert(_coreService);
        }

        [TestMethod]
        [WorkItem(17)]
        [WorkItem(19)]
        [WorkItem(20)]
        public void AllowReturnsBadReuqestForFailingLifecycleManagerOperation()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { LIFE_CYCLE_ALLOW_PERMISSION },
                    Username = CURRENT_USER_ID
                })
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

            var actionResult = _lifeCyclesController.Allow(SAMPLE_TOKEN, null)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(BadRequestErrorMessageResult));

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
            Mock.Assert(mockedLifeCycleManager);
            Mock.Assert(_coreService);
        }

        [TestMethod]
        [WorkItem(19)]
        [WorkItem(20)]
        public void DeclineWithoutCancelPermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String>(),
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Decline(SAMPLE_TOKEN, null)
                .Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);
            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
        }

        [TestMethod]
        [WorkItem(19)]
        [WorkItem(20)]
        public void DeclineWithTokenOfNonExistingJobReturnsNotFound()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { LIFE_CYCLE_DECLINE_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();

            Mock.Arrange(() => _coreService.Jobs)
                .IgnoreInstance()
                .ReturnsCollection(new List<Job>(new List<CumulusCoreService.Job>()))
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Decline(SAMPLE_TOKEN, null)
                .Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.NotFound);
            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
            Mock.Assert(_coreService);
        }

        [TestMethod]
        [WorkItem(19)]
        [WorkItem(20)]
        public void DeclinewWithValidTokenCreatesLifecycleManagerAndExecutesOnDeclineCallbackMethod()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { LIFE_CYCLE_DECLINE_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();

            Mock.Arrange(() => _coreService.Jobs)
                .IgnoreInstance()
                .ReturnsCollection(new List<Job>(new List<CumulusCoreService.Job>{ CreateSampleJob() }))
                .MustBeCalled();

            var mockedLifeCycleManager = Mock.Create<LifeCycleManager>();
            Mock.Arrange(() => mockedLifeCycleManager.OnDeclineCallback(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .MustBeCalled();

            var actionResult = _lifeCyclesController.Decline(SAMPLE_TOKEN, null)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof (OkResult));

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
            Mock.Assert(mockedLifeCycleManager);
            Mock.Assert(_coreService);
        }

        [TestMethod]
        [WorkItem(17)]
        [WorkItem(19)]
        [WorkItem(20)]
        public void DeclineReturnsBadReuqestForFailingLifecycleManagerOperation()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { LIFE_CYCLE_DECLINE_PERMISSION },
                    Username = CURRENT_USER_ID
                })
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

            var actionResult = _lifeCyclesController.Decline(SAMPLE_TOKEN, null)
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(BadRequestErrorMessageResult));

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
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
                Type = CALLOUT_JOB_TYPE,
                State = JobStateEnum.Running.ToString(),
                Parameters = JsonConvert.SerializeObject(calloutData),
                Token = SAMPLE_TOKEN
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
