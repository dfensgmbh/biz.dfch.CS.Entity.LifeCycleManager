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
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http.OData;
using System.Web.Http.Results;
using biz.dfch.CS.Entity.LifeCycleManager.Context;
 using biz.dfch.CS.Entity.LifeCycleManager.Controller;
using biz.dfch.CS.Entity.LifeCycleManager.Model;
using biz.dfch.CS.Entity.LifeCycleManager.UserData;
﻿using biz.dfch.CS.Entity.LifeCycleManager.Util;
﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
﻿using Telerik.JustMock;

namespace biz.dfch.CS.Entity.LifeCycleManager.Tests.Controller
{
    [TestClass]
    public class StateChangeLocksControllerTest : BaseControllerTest<StateChangeLock>
    {
        private StateChangeLocksController _stateChangeLocksController;
        private LifeCycleContext _lifeCycleContext;
        private const String STATE_CHANGE_LOCK_READ_PERMISSION = "LightSwitchApplication:StateChangeLockCanRead";
        private const String STATE_CHANGE_LOCK_CREATE_PERMISSION = "LightSwitchApplication:StateChangeLockCanCreate";
        private const String STATE_CHANGE_LOCK_DELETE_PERMISSION = "LightSwitchApplication:StateChangeLockCanDelete";
        private const String ENTITY_ID = "http://test/api/ApplicationData.svc/TestEntities(1)";

        [TestInitialize]
        public void TestInitialize()
        {
            Mock.SetupStatic(typeof(ODataControllerHelper));
            Mock.SetupStatic(typeof(CurrentUserDataProvider));
            Mock.SetupStatic(typeof(HttpContext));

            Mock.Arrange(() => HttpContext.Current.Request.Headers.Get(TENANT_ID_HEADER_KEY))
                .Returns(TENANT_ID)
                .OccursOnce();

            _stateChangeLocksController = new StateChangeLocksController();
            _lifeCycleContext = Mock.Create<LifeCycleContext>();

            Mock.Assert(() => HttpContext.Current.Request.Headers.Get(TENANT_ID_HEADER_KEY));
        }

        [TestMethod]
        public void GetStateChangeLocksForAuthorizedUserWithReadPermissionReturnsStateChangeLocksTheUserIsAuthorizedFor()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { STATE_CHANGE_LOCK_READ_PERMISSION },
                    Username = CURRENT_USER_ID,
                    Tid = TENANT_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.GetEntitiesForUser(Arg.IsAny<DbSet<StateChangeLock>>(), CURRENT_USER_ID, TENANT_ID))
                .ReturnsCollection(CreateSampleStateChangeLockDbSet().ToList())
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.StateChangeLocks)
                .IgnoreInstance()
                .ReturnsCollection(CreateSampleStateChangeLockDbSet())
                .MustBeCalled();

            var actionResult = _stateChangeLocksController.GetStateChangeLocks(
                CreateODataQueryOptions("http://localhost/api/Core.svc/StateChangeLocks"))
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkNegotiatedContentResult<IEnumerable<StateChangeLock>>));

            var response = actionResult as OkNegotiatedContentResult<IEnumerable<StateChangeLock>>;
            Assert.IsNotNull(response);
            Assert.AreEqual(2, response.Content.Count());

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
            Mock.Assert(() => CurrentUserDataProvider.GetEntitiesForUser(Arg.IsAny<DbSet<StateChangeLock>>(), CURRENT_USER_ID, TENANT_ID));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void GetStateChangeLocksForUserWithoutReadPermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> (),
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();

            var actionResult = _stateChangeLocksController.GetStateChangeLocks(
                CreateODataQueryOptions("http://localhost/api/Core.svc/StateChangeLocks"))
                .Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
        }

        [TestMethod]
        public void GetStateChangeLocksForUserWithoutStateChangeLocksReturnsEmptyList()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { STATE_CHANGE_LOCK_READ_PERMISSION },
                    Username = CURRENT_USER_ID,
                    Tid = TENANT_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.GetEntitiesForUser(Arg.IsAny<DbSet<StateChangeLock>>(), CURRENT_USER_ID, TENANT_ID))
                .ReturnsCollection(new List<StateChangeLock>())
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.StateChangeLocks)
                .IgnoreInstance()
                .ReturnsCollection(CreateSampleStateChangeLockDbSet())
                .MustBeCalled();

            var actionResult = _stateChangeLocksController.GetStateChangeLocks(
                CreateODataQueryOptions("http://localhost/api/Core.svc/StateChangeLocks"))
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkNegotiatedContentResult<IEnumerable<StateChangeLock>>));

            var response = actionResult as OkNegotiatedContentResult<IEnumerable<StateChangeLock>>;
            Assert.IsNotNull(response);
            Assert.AreEqual(0, response.Content.Count());

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
            Mock.Assert(() => CurrentUserDataProvider.GetEntitiesForUser(Arg.IsAny<DbSet<StateChangeLock>>(), CURRENT_USER_ID, TENANT_ID));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void PostStateChangeLockForUserWithoutCreatePermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String>(),
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();

            var actionResult = _stateChangeLocksController.Post(
                new StateChangeLock
                {
                    Id = 1,
                    CreatedBy = ANOTHER_USER_ID,
                    Modified = DateTimeOffset.Now,
                    ModifiedBy = CURRENT_USER_ID,
                    EntityId = ENTITY_ID
                }).Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
        }

        [TestMethod]
        public void PostStateChangeLockForUserWithCreatePermissionCreatesEntityAndReturnsCreated()
        {
            StateChangeLock createdEntity = null;

            Mock.Arrange(() => ODataControllerHelper.ResponseCreated(
                Arg.IsAny<ODataController>(), Arg.IsAny<StateChangeLock>(),
                Arg.IsAny<String>())).Returns(new HttpResponseMessage(HttpStatusCode.Created));
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { STATE_CHANGE_LOCK_CREATE_PERMISSION },
                    Username = CURRENT_USER_ID,
                    Tid = TENANT_ID
                })
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
                    Tid = ANOTHER_TENANT_ID,
                    EntityId = ENTITY_ID
                }).Result;

            Assert.AreEqual(CURRENT_USER_ID, createdEntity.CreatedBy);
            Assert.AreEqual(DateTimeOffset.Now.Date, createdEntity.Created.Value.Date);
            Assert.IsNull(createdEntity.ModifiedBy);
            Assert.AreEqual(TENANT_ID, createdEntity.Tid);
            Assert.AreEqual(ENTITY_ID, createdEntity.EntityId);
            Assert.AreNotEqual(1, createdEntity.Id);

            Assert.IsTrue(actionResult.GetType() == typeof(ResponseMessageResult));
            var response = actionResult as ResponseMessageResult;
            Assert.AreEqual(HttpStatusCode.Created, response.Response.StatusCode);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void DeleteStateChangeLockForAuthorizedUserWithDeletePermissionDeletesStateChangeLock()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { STATE_CHANGE_LOCK_DELETE_PERMISSION },
                    Username = CURRENT_USER_ID,
                    Tid = TENANT_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, TENANT_ID, Arg.IsAny<StateChangeLock>()))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.StateChangeLocks.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleStateChangeLockDbSet()[0])
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.StateChangeLocks.Remove(Arg.IsAny<StateChangeLock>()))
                .IgnoreInstance()
                .MustBeCalled();
            var actionResult = _stateChangeLocksController.Delete(1).Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.NoContent);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
            Mock.Assert(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, TENANT_ID, Arg.IsAny<StateChangeLock>()));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void DeleteStateChangeLockForUserWithoutDeletePermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String>(),
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();

            var actionResult = _stateChangeLocksController.Delete(1).Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
        }

        [TestMethod]
        public void DeleteStateChangeLockForUnauthorizedUserReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { STATE_CHANGE_LOCK_DELETE_PERMISSION },
                    Username = CURRENT_USER_ID,
                    Tid = TENANT_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, TENANT_ID, Arg.IsAny<StateChangeLock>()))
                .Returns(false)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.StateChangeLocks.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleStateChangeLockDbSet()[0])
                .MustBeCalled();
            var actionResult = _stateChangeLocksController.Delete(1).Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
            Mock.Assert(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, TENANT_ID, Arg.IsAny<StateChangeLock>()));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void DeleteForNonExistingStateChangeLockIdReturnsNotFound()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { STATE_CHANGE_LOCK_DELETE_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.StateChangeLocks.Find(1))
                .IgnoreInstance()
                .Returns((StateChangeLock)null)
                .MustBeCalled();
            var actionResult = _stateChangeLocksController.Delete(1).Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.NotFound);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
            Mock.Assert(_lifeCycleContext);
        }

        private IList<StateChangeLock> CreateSampleStateChangeLockDbSet()
        {
            var dbSet = new List<StateChangeLock>();
            dbSet.Add(new StateChangeLock { Id = 1, CreatedBy = CURRENT_USER_ID, Tid = TENANT_ID, EntityId = ENTITY_ID });
            dbSet.Add(new StateChangeLock { Id = 2, CreatedBy = CURRENT_USER_ID, Tid = TENANT_ID, EntityId = "http://test/api/ApplicationData.svc/Tests(1)" });
            return dbSet;
        }
    }
}
