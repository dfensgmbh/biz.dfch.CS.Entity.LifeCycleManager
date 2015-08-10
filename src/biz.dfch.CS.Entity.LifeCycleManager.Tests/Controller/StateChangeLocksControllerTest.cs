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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http.OData;
using System.Web.Http.OData.Builder;
using System.Web.Http.OData.Query;
using System.Web.Http.Results;
using biz.dfch.CS.Entity.LifeCycleManager.Context;
﻿using biz.dfch.CS.Entity.LifeCycleManager.Controller;
using biz.dfch.CS.Entity.LifeCycleManager.Model;
using biz.dfch.CS.Entity.LifeCycleManager.UserData;
﻿using biz.dfch.CS.Entity.LifeCycleManager.Util;
﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
﻿using Telerik.JustMock;

namespace biz.dfch.CS.Entity.LifeCycleManager.Tests.Controller
{
    [TestClass]
    public class StateChangeLocksControllerTest
    {
        private StateChangeLocksController _stateChangeLocksController;
        private LifeCycleContext _lifeCycleContext;
        private const String STATE_CHANGE_LOCK_READ_PERMISSION = "CumulusCore:StateChangeLockCanRead";
        private const String STATE_CHANGE_LOCK_CREATE_PERMISSION = "CumulusCore:StateChangeLockCanCreate";
        private const String STATE_CHANGE_LOCK_DELETE_PERMISSION = "CumulusCore:StateChangeLockCanDelete";
        private const String CURRENT_USER_ID = "currentUser";
        private const String ANOTHER_USER_ID = "anotherUser";
        private const String ENTITY_TYPE = "TestEntity";

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

        [TestMethod]
        public void GetStateChangeLocksForUserWithReadPermissionReturnsHisStateChangeLocks()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(STATE_CHANGE_LOCK_READ_PERMISSION))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.GetCurrentUserId())
                .Returns(CURRENT_USER_ID)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.StateChangeLocks)
                .IgnoreInstance()
                .ReturnsCollection(CreateSampleStateChangeLockDbSetForUser(CURRENT_USER_ID))
                .MustBeCalled();

            var context = new ODataQueryContext(GetBuilder().GetEdmModel(), typeof(StateChangeLock));
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/Core.svc/StateChangeLocks");

            var actionResult = _stateChangeLocksController.GetStateChangeLocks(new ODataQueryOptions<StateChangeLock>(context, request)).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkNegotiatedContentResult<IEnumerable<StateChangeLock>>));

            var response = actionResult as OkNegotiatedContentResult<IEnumerable<StateChangeLock>>;
            Assert.IsNotNull(response);
            Assert.AreEqual(2, response.Content.Count());

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(STATE_CHANGE_LOCK_READ_PERMISSION));
            Mock.Assert(() => CurrentUserDataProvider.GetCurrentUserId());
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void GetStateChangeLocksForUserWithoutReadPermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(STATE_CHANGE_LOCK_READ_PERMISSION))
                .Returns(false)
                .MustBeCalled();

            var context = new ODataQueryContext(GetBuilder().GetEdmModel(), typeof(StateChangeLock));
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/Core.svc/StateChangeLocks");

            var actionResult = _stateChangeLocksController.GetStateChangeLocks(new ODataQueryOptions<StateChangeLock>(context, request)).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(StatusCodeResult));
            var response = (StatusCodeResult)actionResult;
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(STATE_CHANGE_LOCK_READ_PERMISSION));
        }

        [TestMethod]
        public void GetStateChangeLocksForUserWithoutStateChangeLocksReturnsEmptyList()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(STATE_CHANGE_LOCK_READ_PERMISSION))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.GetCurrentUserId())
                .Returns(CURRENT_USER_ID)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.StateChangeLocks)
                .IgnoreInstance()
                .ReturnsCollection(CreateSampleStateChangeLockDbSetForUser(ANOTHER_USER_ID))
                .MustBeCalled();

            var context = new ODataQueryContext(GetBuilder().GetEdmModel(), typeof(StateChangeLock));
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/Core.svc/StateChangeLocks");

            var actionResult = _stateChangeLocksController.GetStateChangeLocks(new ODataQueryOptions<StateChangeLock>(context, request)).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkNegotiatedContentResult<IEnumerable<StateChangeLock>>));

            var response = actionResult as OkNegotiatedContentResult<IEnumerable<StateChangeLock>>;
            Assert.IsNotNull(response);
            Assert.AreEqual(0, response.Content.Count());

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(STATE_CHANGE_LOCK_READ_PERMISSION));
            Mock.Assert(() => CurrentUserDataProvider.GetCurrentUserId());
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void PostStateChangeLockForUserWithoutCreatePermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(STATE_CHANGE_LOCK_CREATE_PERMISSION))
                .Returns(false)
                .MustBeCalled();

            var actionResult = _stateChangeLocksController.Post(
                new StateChangeLock
                {
                    Id = 1,
                    CreatedBy = ANOTHER_USER_ID,
                    Modified = DateTimeOffset.Now,
                    ModifiedBy = CURRENT_USER_ID,
                    EntityType = ENTITY_TYPE,
                    EntityId = 3
                }).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(StatusCodeResult));
            var response = (StatusCodeResult)actionResult;
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(STATE_CHANGE_LOCK_CREATE_PERMISSION));
        }

        [TestMethod]
        public void PostStateChangeLockForUserWithCreatePermissionCreatesEntityAndReturnsCreated()
        {
            StateChangeLock createdEntity = null;

            Mock.Arrange(() => ODataControllerHelper.ResponseCreated(
                Arg.IsAny<ODataController>(), Arg.IsAny<StateChangeLock>(),
                Arg.IsAny<String>())).Returns(new HttpResponseMessage(HttpStatusCode.Created));
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(STATE_CHANGE_LOCK_CREATE_PERMISSION))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.GetCurrentUserId())
                .Returns(CURRENT_USER_ID)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.StateChangeLocks.Add(Arg.IsAny<StateChangeLock>()))
                .IgnoreInstance()
                .DoInstead((StateChangeLock j) => createdEntity = j)
                .Returns((j) => j)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.SaveChanges())
                .IgnoreInstance()
                .MustBeCalled();

            var actionResult = _stateChangeLocksController.Post(
                new StateChangeLock
                {
                    Id = 1,
                    CreatedBy = ANOTHER_USER_ID,
                    Modified = DateTimeOffset.Now,
                    ModifiedBy = CURRENT_USER_ID,
                    EntityType = ENTITY_TYPE,
                    EntityId = 3
                }).Result;

            Assert.AreEqual(CURRENT_USER_ID, createdEntity.CreatedBy);
            Assert.AreEqual(DateTimeOffset.Now.Date, createdEntity.Created.Date);
            Assert.IsNull(createdEntity.ModifiedBy);
            Assert.AreEqual(ENTITY_TYPE, createdEntity.EntityType);
            Assert.AreEqual(3, createdEntity.EntityId);
            Assert.AreNotEqual(1, createdEntity.Id);

            Assert.IsTrue(actionResult.GetType() == typeof(ResponseMessageResult));
            var response = actionResult as ResponseMessageResult;
            Assert.AreEqual(HttpStatusCode.Created, response.Response.StatusCode);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(STATE_CHANGE_LOCK_CREATE_PERMISSION));
            Mock.Assert(() => CurrentUserDataProvider.GetCurrentUserId());
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void DeleteStateChangeLockForUserWithDeletePermissionAndOwnershipDeletesStateChangeLock()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(STATE_CHANGE_LOCK_DELETE_PERMISSION))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.GetCurrentUserId())
                .Returns(ANOTHER_USER_ID)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.StateChangeLocks.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleStateChangeLockDbSetForUser(CURRENT_USER_ID)[0])
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.StateChangeLocks.Remove(Arg.IsAny<StateChangeLock>()))
                .IgnoreInstance()
                .MustBeCalled();
            var actionResult = _stateChangeLocksController.Delete(1).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(StatusCodeResult));
            var response = (StatusCodeResult)actionResult;
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(STATE_CHANGE_LOCK_DELETE_PERMISSION));
            Mock.Assert(() => CurrentUserDataProvider.GetCurrentUserId());
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void DeleteStateChangeLockForUserWithoutDeletePermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(STATE_CHANGE_LOCK_DELETE_PERMISSION))
                .Returns(false)
                .MustBeCalled();

            var actionResult = _stateChangeLocksController.Delete(1).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(StatusCodeResult));
            var response = (StatusCodeResult)actionResult;
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(STATE_CHANGE_LOCK_DELETE_PERMISSION));
        }

        [TestMethod]
        public void DeleteStateChangeLockForUserWithoutOwnershipReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(STATE_CHANGE_LOCK_DELETE_PERMISSION))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.GetCurrentUserId())
                .Returns(ANOTHER_USER_ID)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.StateChangeLocks.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleStateChangeLockDbSetForUser(CURRENT_USER_ID)[0])
                .MustBeCalled();
            var actionResult = _stateChangeLocksController.Delete(1).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(StatusCodeResult));
            var response = (StatusCodeResult)actionResult;
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(STATE_CHANGE_LOCK_DELETE_PERMISSION));
            Mock.Assert(() => CurrentUserDataProvider.GetCurrentUserId());
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void DeleteForNonExistingStateChangeLockIdReturnsNotFound()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(STATE_CHANGE_LOCK_DELETE_PERMISSION))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.StateChangeLocks.Find(1))
                .IgnoreInstance()
                .Returns((StateChangeLock)null)
                .MustBeCalled();
            var actionResult = _stateChangeLocksController.Delete(1).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(StatusCodeResult));
            var response = (StatusCodeResult)actionResult;
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(STATE_CHANGE_LOCK_DELETE_PERMISSION));
            Mock.Assert(_lifeCycleContext);
        }

        private ODataConventionModelBuilder GetBuilder()
        {
            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<StateChangeLock>("StateChangeLocks");
            return builder;
        }

        private IList<StateChangeLock> CreateSampleStateChangeLockDbSetForUser(String ownerId)
        {
            var dbSet = new List<StateChangeLock>();
            dbSet.Add(new StateChangeLock { Id = 1, CreatedBy = ownerId, EntityType = "TestEntity", EntityId = 1});
            dbSet.Add(new StateChangeLock { Id = 2, CreatedBy = ownerId, EntityType = "Test" , EntityId = 2});
            return dbSet;
        }
    }
}
