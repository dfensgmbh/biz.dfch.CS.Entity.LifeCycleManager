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
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http.OData;
using System.Web.Http.Results;
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
    public class CalloutDefinitionsControllerTest : BaseControllerTest<CalloutDefinition>
    {
        private CalloutDefinitionsController _calloutDefinitionsController;
        private LifeCycleContext _lifeCycleContext;
        private const String CALLOUT_DEFINITION_READ_PERMISSION = "LightSwitchApplication:CalloutDefinitionCanRead";
        private const String CALLOUT_DEFINITION_UPDATE_PERMISSION = "LightSwitchApplication:CalloutDefinitionCanUpdate";
        private const String CALLOUT_DEFINITION_CREATE_PERMISSION = "LightSwitchApplication:CalloutDefinitionCanCreate";
        private const String CALLOUT_DEFINITION_DELETE_PERMISSION = "LightSwitchApplication:CalloutDefinitionCanDelete";
        private const String SAMPLE_ENTITY_TYPE = "User";
        private const String ENTITY_ID_1 = "1";

        [TestInitialize]
        public void TestInitialize()
        {
            Mock.SetupStatic(typeof(ODataControllerHelper));
            Mock.SetupStatic(typeof(CurrentUserDataProvider));

            _calloutDefinitionsController = new CalloutDefinitionsController();
            _lifeCycleContext = Mock.Create<LifeCycleContext>();
        }

        [TestMethod]
        public void GetCalloutDefinitionsForUserWithReadPermissionReturnsCalloutDefinitionsTheUserIsAuthorizedFor()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> {CALLOUT_DEFINITION_READ_PERMISSION},
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.GetEntitiesForUser(Arg.IsAny<DbSet<CalloutDefinition>>(), CURRENT_USER_ID, EMPTY_TENANT_ID))
                .ReturnsCollection(CreateSampleCalloutDefinitionDbSet().ToList())
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.CalloutDefinitions)
                .IgnoreInstance()
                .ReturnsCollection(CreateSampleCalloutDefinitionDbSet())
                .MustBeCalled();

            var actionResult = _calloutDefinitionsController.GetCalloutDefinitions(
                CreateODataQueryOptions("http://localhost/api/Core.svc/CalloutDefinitions"))
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkNegotiatedContentResult<IEnumerable<CalloutDefinition>>));

            var response = actionResult as OkNegotiatedContentResult<IEnumerable<CalloutDefinition>>;
            Assert.IsNotNull(response);
            Assert.AreEqual(2, response.Content.Count());

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(() => CurrentUserDataProvider.GetEntitiesForUser(Arg.IsAny<DbSet<CalloutDefinition>>(), CURRENT_USER_ID, EMPTY_TENANT_ID));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void GetCalloutDefinitionsForUserWithoutReadPermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String>(),
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();

            var actionResult = _calloutDefinitionsController.GetCalloutDefinitions(
                CreateODataQueryOptions("http://localhost/api/Core.svc/CalloutDefinitions"))
                .Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
        }

        [TestMethod]
        public void GetCalloutDefinitionsWithNonExistingCalloutDefinitionsForCurrentUserReturnsEmptyList()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { CALLOUT_DEFINITION_READ_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.GetEntitiesForUser(Arg.IsAny<DbSet<CalloutDefinition>>(), CURRENT_USER_ID, EMPTY_TENANT_ID))
                .ReturnsCollection(new List<CalloutDefinition>())
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.CalloutDefinitions)
                .IgnoreInstance()
                .ReturnsCollection(CreateSampleCalloutDefinitionDbSet())
                .MustBeCalled();

            var actionResult = _calloutDefinitionsController.GetCalloutDefinitions(
                CreateODataQueryOptions("http://localhost/api/Core.svc/CalloutDefinitions"))
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkNegotiatedContentResult<IEnumerable<CalloutDefinition>>));

            var response = actionResult as OkNegotiatedContentResult<IEnumerable<CalloutDefinition>>;
            Assert.IsNotNull(response);
            Assert.AreEqual(0, response.Content.Count());

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(() => CurrentUserDataProvider.GetEntitiesForUser(Arg.IsAny<DbSet<CalloutDefinition>>(), CURRENT_USER_ID, EMPTY_TENANT_ID));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void GetCalloutDefinitionByIdForAuthorizedUserWithReadPermissionReturnsRequestedCalloutDefinition()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { CALLOUT_DEFINITION_READ_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<CalloutDefinition>()))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.CalloutDefinitions.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleCalloutDefinitionDbSet()[0])
                .MustBeCalled();

            var actionResult = _calloutDefinitionsController.GetCalloutDefinition(1,
                CreateODataQueryOptions("http://localhost/api/Core.svc/CalloutDefinitions(1)"))
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkNegotiatedContentResult<CalloutDefinition>));

            var response = actionResult as OkNegotiatedContentResult<CalloutDefinition>;
            var calloutDefinition = response.Content;
            Assert.AreEqual(1, calloutDefinition.Id);
            Assert.AreEqual(CURRENT_USER_ID, calloutDefinition.CreatedBy);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<CalloutDefinition>()));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void GetCalloutDefinitionByIdForUnAuthorizedUserReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { CALLOUT_DEFINITION_READ_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<CalloutDefinition>()))
                .Returns(false)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.CalloutDefinitions.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleCalloutDefinitionDbSet()[0])
                .MustBeCalled();

            var actionResult = _calloutDefinitionsController.GetCalloutDefinition(1,
                CreateODataQueryOptions("http://localhost/api/Core.svc/CalloutDefinitions(1)"))
                .Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<CalloutDefinition>()));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void GetCalloutDefinitionByIdForNonExistingCalloutDefinitionIdReturnsNotFound()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { CALLOUT_DEFINITION_READ_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.CalloutDefinitions.Find(1))
                .IgnoreInstance()
                .Returns((CalloutDefinition)null)
                .MustBeCalled();

            var actionResult = _calloutDefinitionsController.GetCalloutDefinition(1,
                CreateODataQueryOptions("http://localhost/api/Core.svc/CalloutDefinitions(1)"))
                .Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.NotFound);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void GetCalloutDefinitionByIdForUserWithoutReadPermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String>(),
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();

            var actionResult = _calloutDefinitionsController.GetCalloutDefinition(1,
                CreateODataQueryOptions("http://localhost/api/Core.svc/CalloutDefinitions(1)"))
                .Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
        }

        [TestMethod]
        public void PutCalloutDefinitionForAuthorizedUserWithUpdatePermissionUpdatesCalloutDefinition()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { CALLOUT_DEFINITION_UPDATE_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<CalloutDefinition>()))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.CalloutDefinitions.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleCalloutDefinitionDbSet()[0])
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.CalloutDefinitions.Attach(Arg.IsAny<CalloutDefinition>()))
                .IgnoreInstance()
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Entry(Arg.IsAny<CalloutDefinition>()))
                .IgnoreInstance()
                .Returns(Mock.Create<DbEntityEntry<CalloutDefinition>>())
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.SaveChanges())
                .IgnoreInstance()
                .MustBeCalled();

            var actionResult = _calloutDefinitionsController.Put(1,
                new CalloutDefinition
                {
                    Id = 1,
                    CreatedBy = ANOTHER_USER_ID,
                    Created = DateTimeOffset.Parse("05/01/2008"),
                    ModifiedBy = ANOTHER_USER_ID,
                    Modified = DateTimeOffset.Parse("05/01/2008"),
                    Tid = ANOTHER_TENANT_ID,
                    EntityType = SAMPLE_ENTITY_TYPE,
                    EntityId = ENTITY_ID_1,
                    TenantId = TENANT_ID,
                    Parameters = "testparameters",
                }).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkNegotiatedContentResult<CalloutDefinition>));

            var response = actionResult as OkNegotiatedContentResult<CalloutDefinition>;
            var calloutDefinition = response.Content;
            Assert.AreEqual(1, calloutDefinition.Id);
            Assert.AreEqual(CURRENT_USER_ID, calloutDefinition.CreatedBy);
            Assert.AreNotEqual(DateTimeOffset.Parse("05/01/2008"), calloutDefinition.Created);
            Assert.AreEqual(CURRENT_USER_ID, calloutDefinition.ModifiedBy);
            Assert.AreNotEqual(DateTimeOffset.Parse("05/01/2008"), calloutDefinition.Modified);
            Assert.AreEqual(TENANT_ID, calloutDefinition.Tid);
            Assert.AreEqual(SAMPLE_ENTITY_TYPE, calloutDefinition.EntityType);
            Assert.AreEqual(ENTITY_ID_1, calloutDefinition.EntityId);
            Assert.AreEqual(TENANT_ID, calloutDefinition.TenantId);
            Assert.AreEqual("testparameters", calloutDefinition.Parameters);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<CalloutDefinition>()));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void PutCalloutDefinitionForUserWithoutUpdatePermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String>(),
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();

            var actionResult = _calloutDefinitionsController.Put(1,
                new CalloutDefinition { Id = 1 })
                .Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
        }

        [TestMethod]
        public void PutCalloutDefinitionForUnauthorizedUserReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { CALLOUT_DEFINITION_UPDATE_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<CalloutDefinition>()))
                .Returns(false)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.CalloutDefinitions.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleCalloutDefinitionDbSet()[0])
                .MustBeCalled();

            var actionResult = _calloutDefinitionsController.Put(1,
                new CalloutDefinition
                {
                    Id = 1,
                    CreatedBy = ANOTHER_USER_ID,
                    Created = DateTimeOffset.Parse("05/01/2008"),
                    ModifiedBy = ANOTHER_USER_ID,
                    Modified = DateTimeOffset.Parse("05/01/2008"),
                    EntityType = SAMPLE_ENTITY_TYPE,
                    EntityId = ENTITY_ID_1,
                    TenantId = TENANT_ID,
                    Parameters = "testparameters",
                }).Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<CalloutDefinition>()));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void PutCalloutDefinitionForAuthorizedUserSetsUpdatedDate()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { CALLOUT_DEFINITION_UPDATE_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<CalloutDefinition>()))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.CalloutDefinitions.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleCalloutDefinitionDbSet()[0])
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.CalloutDefinitions.Attach(Arg.IsAny<CalloutDefinition>()))
                .IgnoreInstance()
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Entry(Arg.IsAny<CalloutDefinition>()))
                .IgnoreInstance()
                .Returns(Mock.Create<DbEntityEntry<CalloutDefinition>>())
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.SaveChanges())
                .IgnoreInstance()
                .MustBeCalled();

            var actionResult = _calloutDefinitionsController.Put(1,
                new CalloutDefinition
                {
                    Id = 1,
                    EntityType = SAMPLE_ENTITY_TYPE,
                    EntityId = ENTITY_ID_1,
                    TenantId = TENANT_ID,
                    Parameters = "testparameters",
                }).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkNegotiatedContentResult<CalloutDefinition>));

            var response = actionResult as OkNegotiatedContentResult<CalloutDefinition>;
            var calloutDefinition = response.Content;
            Assert.AreEqual(1, calloutDefinition.Id);
            Assert.AreEqual(CURRENT_USER_ID, calloutDefinition.CreatedBy);
            Assert.AreEqual(DateTimeOffset.Now.Date, calloutDefinition.Modified.Date);
            Assert.AreEqual(CURRENT_USER_ID, calloutDefinition.ModifiedBy);
            Assert.AreEqual(SAMPLE_ENTITY_TYPE, calloutDefinition.EntityType);
            Assert.AreEqual(ENTITY_ID_1, calloutDefinition.EntityId);
            Assert.AreEqual(TENANT_ID, calloutDefinition.TenantId);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<CalloutDefinition>()));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void PutCalloutDefinitionForNonExistingCalloutDefinitionIdReturnsNotFound()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { CALLOUT_DEFINITION_UPDATE_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.CalloutDefinitions.Find(1))
                .IgnoreInstance()
                .Returns((CalloutDefinition)null)
                .MustBeCalled();

            var actionResult = _calloutDefinitionsController.Put(1,
                new CalloutDefinition
                {
                    Id = 1,
                    EntityType = SAMPLE_ENTITY_TYPE,
                    EntityId = ENTITY_ID_1,
                    TenantId = TENANT_ID,
                    Parameters = "testparameters",
                }).Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.NotFound);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void PutCalloutDefinitionWithDifferentCalloutDefinitionIdsInUrlAndBodyReturnsBadRequest()
        {
            var actionResult = _calloutDefinitionsController.Put(2,
                new CalloutDefinition
                {
                    Id = 1,
                    EntityType = SAMPLE_ENTITY_TYPE,
                    EntityId = ENTITY_ID_1,
                    TenantId = TENANT_ID,
                    Parameters = "testparameters",
                }).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(BadRequestResult));
        }

        [TestMethod]
        public void PostCalloutDefinitionForUserWithoutCreatePermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String>(),
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();

            var actionResult = _calloutDefinitionsController.Post(
                new CalloutDefinition
                {
                    Id = 1,
                    EntityType = SAMPLE_ENTITY_TYPE,
                    EntityId = ENTITY_ID_1,
                    TenantId = TENANT_ID,
                    Parameters = "testparameters"
                }).Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
        }

        [TestMethod]
        public void PostCalloutDefinitionForUserWithCreatePermissionCreatesCalloutDefinitionAndReturnsCreated()
        {
            CalloutDefinition createdCalloutDefinition = null;

            Mock.Arrange(() => ODataControllerHelper.ResponseCreated(
                Arg.IsAny<ODataController>(), Arg.IsAny<CalloutDefinition>(),
                Arg.IsAny<String>())).Returns(new HttpResponseMessage(HttpStatusCode.Created));
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { CALLOUT_DEFINITION_CREATE_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.CalloutDefinitions.Add(Arg.IsAny<CalloutDefinition>()))
                .IgnoreInstance()
                .DoInstead((CalloutDefinition j) => createdCalloutDefinition = j)
                .Returns((CalloutDefinition j) => j)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.SaveChanges())
                .IgnoreInstance()
                .MustBeCalled();

            var actionResult = _calloutDefinitionsController.Post(
                new CalloutDefinition
                {
                    Id = 1,
                    CreatedBy = ANOTHER_USER_ID,
                    Modified = DateTimeOffset.Now,
                    Tid = ANOTHER_TENANT_ID,
                    ModifiedBy = CURRENT_USER_ID,
                    EntityType = SAMPLE_ENTITY_TYPE,
                    EntityId = ENTITY_ID_1,
                    TenantId = TENANT_ID,
                    Parameters = "testparameters"
                }).Result;

            Assert.AreEqual(CURRENT_USER_ID, createdCalloutDefinition.CreatedBy);
            Assert.AreEqual(DateTimeOffset.Now.Date, createdCalloutDefinition.Created.Date);
            Assert.IsNull(createdCalloutDefinition.ModifiedBy);
            Assert.AreEqual(TENANT_ID, createdCalloutDefinition.Tid);
            Assert.AreEqual(SAMPLE_ENTITY_TYPE, createdCalloutDefinition.EntityType);
            Assert.AreEqual(ENTITY_ID_1, createdCalloutDefinition.EntityId);
            Assert.AreEqual(TENANT_ID, createdCalloutDefinition.TenantId);
            Assert.AreEqual("testparameters", createdCalloutDefinition.Parameters);

            Assert.IsTrue(actionResult.GetType() == typeof(ResponseMessageResult));
            var response = actionResult as ResponseMessageResult;
            Assert.AreEqual(HttpStatusCode.Created, response.Response.StatusCode);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void PatchCalloutDefinitionForAuthorizedUserWithUpdatePermissionUpdatesDeliveredFields()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { CALLOUT_DEFINITION_UPDATE_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<CalloutDefinition>()))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.CalloutDefinitions.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleCalloutDefinitionDbSet()[0])
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.CalloutDefinitions.Attach(Arg.IsAny<CalloutDefinition>()))
                .IgnoreInstance()
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Entry(Arg.IsAny<CalloutDefinition>()))
                .IgnoreInstance()
                .Returns(Mock.Create<DbEntityEntry<CalloutDefinition>>())
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.SaveChanges())
                .IgnoreInstance()
                .MustBeCalled();

            var delta = new Delta<CalloutDefinition>(typeof(CalloutDefinition));
            delta.TrySetPropertyValue("Id", "3");
            delta.TrySetPropertyValue("CreatedBy", ANOTHER_USER_ID);
            delta.TrySetPropertyValue("ModifiedBy", ANOTHER_USER_ID);
            delta.TrySetPropertyValue("Tid", ANOTHER_TENANT_ID);
            delta.TrySetPropertyValue("Parameters", "testparameters");
            delta.TrySetPropertyValue("EntityType", SAMPLE_ENTITY_TYPE);

            var actionResult = _calloutDefinitionsController.Patch(1, delta).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkNegotiatedContentResult<CalloutDefinition>));

            var response = actionResult as OkNegotiatedContentResult<CalloutDefinition>;
            var calloutDefinition = response.Content;
            Assert.AreEqual(1, calloutDefinition.Id);
            Assert.AreEqual(DateTimeOffset.Now.Date, calloutDefinition.Modified.Date);
            Assert.AreEqual(CURRENT_USER_ID, calloutDefinition.CreatedBy);
            Assert.AreEqual(CURRENT_USER_ID, calloutDefinition.ModifiedBy);
            Assert.AreEqual(TENANT_ID, calloutDefinition.Tid);
            Assert.AreEqual("testparameters", calloutDefinition.Parameters);
            Assert.AreEqual(SAMPLE_ENTITY_TYPE, calloutDefinition.EntityType);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<CalloutDefinition>()));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void PatchCalloutDefinitionForUserWithoutUpdatePermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String>(),
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();

            var actionResult = _calloutDefinitionsController.Patch(1, new Delta<CalloutDefinition>()).Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
        }

        [TestMethod]
        public void PatchCalloutDefinitionForUnauthorizedUserReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { CALLOUT_DEFINITION_UPDATE_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<CalloutDefinition>()))
                .Returns(false)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.CalloutDefinitions.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleCalloutDefinitionDbSet()[0])
                .MustBeCalled();
            var actionResult = _calloutDefinitionsController.Patch(1, new Delta<CalloutDefinition>()).Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<CalloutDefinition>()));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void PatchForNonExistingCalloutDefinitionIdReturnsNotFound()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { CALLOUT_DEFINITION_UPDATE_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.CalloutDefinitions.Find(1))
                .IgnoreInstance()
                .Returns((CalloutDefinition)null)
                .MustBeCalled();
            var actionResult = _calloutDefinitionsController.Patch(1, new Delta<CalloutDefinition>()).Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.NotFound);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void DeleteCalloutDefinitionForAuthorizedUserWithDeletePermissionDeletesCalloutDefinition()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { CALLOUT_DEFINITION_DELETE_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<CalloutDefinition>()))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.CalloutDefinitions.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleCalloutDefinitionDbSet()[0])
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.CalloutDefinitions.Remove(Arg.IsAny<CalloutDefinition>()))
                .IgnoreInstance()
                .MustBeCalled();
            var actionResult = _calloutDefinitionsController.Delete(1).Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.NoContent);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<CalloutDefinition>()));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void DeleteCalloutDefinitionForUserWithoutDeletePermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String>(),
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();

            var actionResult = _calloutDefinitionsController.Delete(1).Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
        }

        [TestMethod]
        public void DeleteCalloutDefinitionForUnauthorizedUserReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { CALLOUT_DEFINITION_DELETE_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<CalloutDefinition>()))
                .Returns(false)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.CalloutDefinitions.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleCalloutDefinitionDbSet()[0])
                .MustBeCalled();
            var actionResult = _calloutDefinitionsController.Delete(1).Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<CalloutDefinition>()));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void DeleteForNonExistingCalloutDefinitionIdReturnsNotFound()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { CALLOUT_DEFINITION_DELETE_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.CalloutDefinitions.Find(1))
                .IgnoreInstance()
                .Returns((CalloutDefinition)null)
                .MustBeCalled();
            var actionResult = _calloutDefinitionsController.Delete(1).Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.NotFound);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(_lifeCycleContext);
        }

        private IList<CalloutDefinition> CreateSampleCalloutDefinitionDbSet()
        {
            var dbSet = new List<CalloutDefinition>();
            dbSet.Add(new CalloutDefinition { Id = 1, Tid = TENANT_ID, CreatedBy = CURRENT_USER_ID, EntityType = SAMPLE_ENTITY_TYPE });
            dbSet.Add(new CalloutDefinition { Id = 2, Tid = TENANT_ID, CreatedBy = CURRENT_USER_ID, EntityId = ENTITY_ID_1});
            return dbSet;
        }
    }
}
