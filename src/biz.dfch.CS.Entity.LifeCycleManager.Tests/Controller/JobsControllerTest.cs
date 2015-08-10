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
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http.OData;
using System.Web.Http.OData.Builder;
using System.Web.Http.OData.Query;
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
    public class JobsControllerTest
    {
        private JobsController _jobsController;
        private LifeCycleContext _lifeCycleContext;
        private const String JOB_READ_PERMISSION = "CumulusCore:JobCanRead";
        private const String JOB_UPDATE_PERMISSION = "CumulusCore:JobCanUpdate";
        private const String JOB_CREATE_PERMISSION = "CumulusCore:JobCanCreate";
        private const String JOB_DELETE_PERMISSION = "CumulusCore:JobCanDelete";
        private const String JOB_RUN_PERMISSION = "CumulusCore:JobCanRun";
        private const String DEFAULT_JOB_TYPE = "DefaultJob";
        private const String CURRENT_USER_ID = "currentUser";
        private const String ANOTHER_USER_ID = "anotherUser";

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            Mock.SetupStatic(typeof (ODataControllerHelper));
            Mock.SetupStatic(typeof (CurrentUserDataProvider));
        }

        [TestInitialize]
        public void TestInitialize()
        {
            _jobsController = new JobsController();
            _lifeCycleContext = Mock.Create<LifeCycleContext>();
        }

        [TestMethod]
        public void GetJobsForUserWithReadPermissionReturnsHisJobs()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_READ_PERMISSION))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.GetCurrentUserId())
                .Returns(CURRENT_USER_ID)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs)
                .IgnoreInstance()
                .ReturnsCollection(CreateSampleJobDbSetForUser(CURRENT_USER_ID))
                .MustBeCalled();

            var context = new ODataQueryContext(GetBuilder().GetEdmModel(), typeof(Job));
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/Core.svc/Jobs");

            var actionResult = _jobsController.GetJobs(new ODataQueryOptions<Job>(context, request)).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkNegotiatedContentResult<IEnumerable<Job>>));

            var response = actionResult as OkNegotiatedContentResult<IEnumerable<Job>>;
            Assert.IsNotNull(response);
            Assert.AreEqual(2, response.Content.Count());

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_READ_PERMISSION));
            Mock.Assert(() => CurrentUserDataProvider.GetCurrentUserId());
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void GetJobsForUserWithoutReadPermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_READ_PERMISSION))
                .Returns(false)
                .MustBeCalled();

            var context = new ODataQueryContext(GetBuilder().GetEdmModel(), typeof(Job));
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/Core.svc/Jobs");

            var actionResult = _jobsController.GetJobs(new ODataQueryOptions<Job>(context, request)).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(StatusCodeResult));
            var response = (StatusCodeResult)actionResult;
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_READ_PERMISSION));
        }

        [TestMethod]
        public void GetJobsWithNonExistingJobsForCurrentUserReturnsEmptyList()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_READ_PERMISSION))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.GetCurrentUserId())
                .Returns(CURRENT_USER_ID)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs)
                .IgnoreInstance()
                .ReturnsCollection(CreateSampleJobDbSetForUser(ANOTHER_USER_ID))
                .MustBeCalled();

            var context = new ODataQueryContext(GetBuilder().GetEdmModel(), typeof(Job));
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/Core.svc/Jobs");

            var actionResult = _jobsController.GetJobs(new ODataQueryOptions<Job>(context, request)).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkNegotiatedContentResult<IEnumerable<Job>>));

            var response = actionResult as OkNegotiatedContentResult<IEnumerable<Job>>;
            Assert.IsNotNull(response);
            Assert.AreEqual(0, response.Content.Count());

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_READ_PERMISSION));
            Mock.Assert(() => CurrentUserDataProvider.GetCurrentUserId());
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void GetJobByIdForUserWithOwnershipAndReadPermissionReturnsRequestedJob()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_READ_PERMISSION))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.GetCurrentUserId())
                .Returns(CURRENT_USER_ID)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleJobDbSetForUser(CURRENT_USER_ID)[0])
                .MustBeCalled();

            var context = new ODataQueryContext(GetBuilder().GetEdmModel(), typeof(Job));
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/Core.svc/Jobs(1)");

            var actionResult = _jobsController.GetJob(1, new ODataQueryOptions<Job>(context, request)).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkNegotiatedContentResult<Job>));

            var response = actionResult as OkNegotiatedContentResult<Job>;
            var job = response.Content;
            Assert.AreEqual(1, job.Id);
            Assert.AreEqual(CURRENT_USER_ID, job.CreatedBy);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_READ_PERMISSION));
            Mock.Assert(() => CurrentUserDataProvider.GetCurrentUserId());
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void GetJobByIdForUserWithoutOwnershipReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_READ_PERMISSION))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.GetCurrentUserId())
                .Returns(ANOTHER_USER_ID)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleJobDbSetForUser(CURRENT_USER_ID)[0])
                .MustBeCalled();

            var context = new ODataQueryContext(GetBuilder().GetEdmModel(), typeof(Job));
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/Core.svc/Jobs(1)");

            var actionResult = _jobsController.GetJob(1, new ODataQueryOptions<Job>(context, request)).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(StatusCodeResult));
            var response = (StatusCodeResult)actionResult;
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_READ_PERMISSION));
            Mock.Assert(() => CurrentUserDataProvider.GetCurrentUserId());
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void GetJobByIdForNonExistingJobIdReturnsNotFound()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_READ_PERMISSION))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns((Job)null)
                .MustBeCalled();

            var context = new ODataQueryContext(GetBuilder().GetEdmModel(), typeof(Job));
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/Core.svc/Jobs(1)");

            var actionResult = _jobsController.GetJob(1, new ODataQueryOptions<Job>(context, request)).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(StatusCodeResult));
            var response = (StatusCodeResult)actionResult;
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_READ_PERMISSION));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void GetJobByIdForUserWithoutReadPermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_READ_PERMISSION))
                .Returns(false)
                .MustBeCalled();

            var context = new ODataQueryContext(GetBuilder().GetEdmModel(), typeof(Job));
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/Core.svc/Jobs(1)");

            var actionResult = _jobsController.GetJob(1, new ODataQueryOptions<Job>(context, request)).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(StatusCodeResult));
            var response = (StatusCodeResult)actionResult;
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_READ_PERMISSION));
        }

        [TestMethod]
        public void PutJobForUserWithUpdatePermissionAndOwnershipUpdatesJob()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_UPDATE_PERMISSION))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.GetCurrentUserId())
                .Returns(CURRENT_USER_ID)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleJobDbSetForUser(CURRENT_USER_ID)[0])
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Attach(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Entry(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .Returns(Mock.Create<DbEntityEntry<Job>>())
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.SaveChanges())
                .IgnoreInstance()
                .MustBeCalled();

            var actionResult = _jobsController.Put(1,
                new Job
                {
                    Id = 1,
                    Type = "Test",
                    CreatedBy = ANOTHER_USER_ID,
                    Created = DateTimeOffset.Parse("05/01/2008"),
                    ModifiedBy = ANOTHER_USER_ID,
                    Modified = DateTimeOffset.Parse("05/01/2008"),
                    State = StateEnum.Canceled.ToString(),
                    Parameters = "testparameters",
                }).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkNegotiatedContentResult<Job>));

            var response = actionResult as OkNegotiatedContentResult<Job>;
            var job = response.Content;
            Assert.AreEqual(1, job.Id);
            Assert.AreEqual("Test", job.Type);
            Assert.AreEqual(CURRENT_USER_ID, job.CreatedBy);
            Assert.AreNotEqual(DateTimeOffset.Parse("05/01/2008"), job.Created);
            Assert.AreEqual(CURRENT_USER_ID, job.ModifiedBy);
            Assert.AreNotEqual(DateTimeOffset.Parse("05/01/2008"), job.Modified);
            Assert.AreEqual(StateEnum.Canceled.ToString(), job.State);
            Assert.AreEqual("testparameters", job.Parameters);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_UPDATE_PERMISSION));
            Mock.Assert(() => CurrentUserDataProvider.GetCurrentUserId());
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void PutJobForUserWithoutUpdatePermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_UPDATE_PERMISSION))
                .Returns(false)
                .MustBeCalled();

            var actionResult = _jobsController.Put(1,
                new Job { Id = 1 })
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(StatusCodeResult));
            var response = (StatusCodeResult)actionResult;
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_UPDATE_PERMISSION));
        }

        [TestMethod]
        public void PutJobForUserWithoutOwnershipReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_UPDATE_PERMISSION))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.GetCurrentUserId())
                .Returns(ANOTHER_USER_ID)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleJobDbSetForUser(CURRENT_USER_ID)[0])
                .MustBeCalled();

            var actionResult = _jobsController.Put(1,
                new Job
                {
                    Id = 1,
                    Type = "Test",
                    CreatedBy = ANOTHER_USER_ID,
                    Created = DateTimeOffset.Parse("05/01/2008"),
                    ModifiedBy = ANOTHER_USER_ID,
                    Modified = DateTimeOffset.Parse("05/01/2008"),
                    State = StateEnum.Canceled.ToString(),
                    Parameters = "testparameters",
                }).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(StatusCodeResult));
            var response = (StatusCodeResult)actionResult;
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_UPDATE_PERMISSION));
            Mock.Assert(() => CurrentUserDataProvider.GetCurrentUserId());
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void PutJobForAuthorizedUserSetsUpdatedDate()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_UPDATE_PERMISSION))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.GetCurrentUserId())
                .Returns(CURRENT_USER_ID)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleJobDbSetForUser(CURRENT_USER_ID)[0])
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Attach(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Entry(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .Returns(Mock.Create<DbEntityEntry<Job>>())
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.SaveChanges())
                .IgnoreInstance()
                .MustBeCalled();

            var actionResult = _jobsController.Put(1,
                new Job
                {
                    Id = 1,
                    Type = "Test",
                    State = StateEnum.Canceled.ToString(),
                    Parameters = "testparameters",
                }).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkNegotiatedContentResult<Job>));

            var response = actionResult as OkNegotiatedContentResult<Job>;
            var job = response.Content;
            Assert.AreEqual(1, job.Id);
            Assert.AreEqual(CURRENT_USER_ID, job.CreatedBy);
            Assert.AreEqual(DateTimeOffset.Now.Date, job.Modified.Date);
            Assert.AreEqual(CURRENT_USER_ID, job.ModifiedBy);
            Assert.AreEqual(StateEnum.Canceled.ToString(), job.State);
            Assert.AreEqual("Test", job.Type);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_UPDATE_PERMISSION));
            Mock.Assert(() => CurrentUserDataProvider.GetCurrentUserId());
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void PutJobForNonExistingJobIdReturnsNotFound()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_UPDATE_PERMISSION))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns((Job)null)
                .MustBeCalled();

            var actionResult = _jobsController.Put(1,
                new Job
                {
                    Id = 1,
                    Type = "Test",
                    State = StateEnum.Canceled.ToString(),
                    Parameters = "testparameters",
                }).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(StatusCodeResult));
            var response = (StatusCodeResult)actionResult;
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_UPDATE_PERMISSION));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void PutJobDifferentJobIdsInUrlAndBodyReturnsBadRequest()
        {
            var actionResult = _jobsController.Put(2,
                new Job
                {
                    Id = 1,
                    Type = "Test",
                    State = StateEnum.Canceled.ToString(),
                    Parameters = "testparameters",
                }).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(BadRequestResult));
        }

        [TestMethod]
        public void PostJobForUserWithoutCreatePermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_CREATE_PERMISSION))
                .Returns(false)
                .MustBeCalled();

            var actionResult = _jobsController.Post(
                new Job
                {
                    Id = 1,
                    Type = "Test",
                    State = StateEnum.Canceled.ToString(),
                    Parameters = "testparameters",
                }).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(StatusCodeResult));
            var response = (StatusCodeResult)actionResult;
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_CREATE_PERMISSION));
        }

        [TestMethod]
        public void PostJobForUserWithCreatePermissionCreatesJobAndReturnsCreated()
        {
            Job createdJob = null;

            Mock.Arrange(() => ODataControllerHelper.ResponseCreated(
                Arg.IsAny<ODataController>(), Arg.IsAny<Job>(),
                Arg.IsAny<String>())).Returns(new HttpResponseMessage(HttpStatusCode.Created));
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_CREATE_PERMISSION))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.GetCurrentUserId())
                .Returns(CURRENT_USER_ID)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Add(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .DoInstead((Job j) => createdJob = j)
                .Returns((Job j) => j)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.SaveChanges())
                .IgnoreInstance()
                .MustBeCalled();

            var actionResult = _jobsController.Post(
                new Job
                {
                    Id = 1,
                    CreatedBy = ANOTHER_USER_ID,
                    Modified = DateTimeOffset.Now,
                    ModifiedBy = CURRENT_USER_ID,
                    Type = "Test",
                    State = StateEnum.Running.ToString(),
                    Parameters = "testparameters",
                }).Result;

            Assert.AreEqual(CURRENT_USER_ID, createdJob.CreatedBy);
            Assert.AreEqual(DateTimeOffset.Now.Date, createdJob.Created.Date);
            Assert.IsNull(createdJob.ModifiedBy);
            Assert.AreEqual(StateEnum.Running.ToString(), createdJob.State);
            Assert.AreEqual("Test", createdJob.Type);
            Assert.AreEqual("testparameters", createdJob.Parameters);

            Assert.IsTrue(actionResult.GetType() == typeof(ResponseMessageResult));
            var response = actionResult as ResponseMessageResult;
            Assert.AreEqual(HttpStatusCode.Created , response.Response.StatusCode);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_CREATE_PERMISSION));
            Mock.Assert(() => CurrentUserDataProvider.GetCurrentUserId());
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void PostJobForUserWithCreatePermissionCreatesJobWithDefaultStateAndTypeAndReturnsCreated()
        {
            Job createdJob = null;

            Mock.Arrange(() => ODataControllerHelper.ResponseCreated(
                Arg.IsAny<ODataController>(), Arg.IsAny<Job>(),
                Arg.IsAny<String>())).Returns(new HttpResponseMessage(HttpStatusCode.Created));
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_CREATE_PERMISSION))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.GetCurrentUserId())
                .Returns(CURRENT_USER_ID)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Add(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .DoInstead((Job j) => createdJob = j)
                .Returns((Job j) => j)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.SaveChanges())
                .IgnoreInstance()
                .MustBeCalled();

            var actionResult = _jobsController.Post(
                new Job
                {
                    Id = 1,
                    CreatedBy = ANOTHER_USER_ID,
                    Modified = DateTimeOffset.Now,
                    ModifiedBy = CURRENT_USER_ID,
                    Parameters = "testparameters",
                }).Result;

            Assert.AreEqual(CURRENT_USER_ID, createdJob.CreatedBy);
            Assert.AreEqual(DateTimeOffset.Now.Date, createdJob.Created.Date);
            Assert.IsNull(createdJob.ModifiedBy);
            Assert.AreEqual(StateEnum.Configuring.ToString(), createdJob.State);
            Assert.AreEqual(DEFAULT_JOB_TYPE, createdJob.Type);
            Assert.AreEqual("testparameters", createdJob.Parameters);

            Assert.IsTrue(actionResult.GetType() == typeof(ResponseMessageResult));
            var response = actionResult as ResponseMessageResult;
            Assert.AreEqual(HttpStatusCode.Created, response.Response.StatusCode);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_CREATE_PERMISSION));
            Mock.Assert(() => CurrentUserDataProvider.GetCurrentUserId());
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void PatchJobForUserWithUpdatePermissionAndOwnershipUpdatesDeliveredFields()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_UPDATE_PERMISSION))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.GetCurrentUserId())
                .Returns(CURRENT_USER_ID)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleJobDbSetForUser(CURRENT_USER_ID)[0])
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Attach(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Entry(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .Returns(Mock.Create<DbEntityEntry<Job>>())
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.SaveChanges())
                .IgnoreInstance()
                .MustBeCalled();

            var delta = new Delta<Job>(typeof(Job));
            delta.TrySetPropertyValue("Id", "3");
            delta.TrySetPropertyValue("CreatedBy", ANOTHER_USER_ID);
            delta.TrySetPropertyValue("ModifiedBy", ANOTHER_USER_ID);
            delta.TrySetPropertyValue("Parameters", "testparameters");
            delta.TrySetPropertyValue("State", "Canceled");

            var actionResult = _jobsController.Patch(1, delta).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkNegotiatedContentResult<Job>));

            var response = actionResult as OkNegotiatedContentResult<Job>;
            var job = response.Content;
            Assert.AreEqual(1, job.Id);
            Assert.AreEqual(DateTimeOffset.Now.Date, job.Modified.Date);
            Assert.AreEqual(CURRENT_USER_ID, job.CreatedBy);
            Assert.AreEqual(CURRENT_USER_ID, job.ModifiedBy);
            Assert.AreEqual("testparameters", job.Parameters);
            Assert.AreEqual(StateEnum.Canceled.ToString(), job.State);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_UPDATE_PERMISSION));
            Mock.Assert(() => CurrentUserDataProvider.GetCurrentUserId());
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void PatchJobForUserWithUpdatePermissionAndOwnershipUpdatesDeliveredFields2()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_UPDATE_PERMISSION))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.GetCurrentUserId())
                .Returns(CURRENT_USER_ID)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleJobDbSetForUser(CURRENT_USER_ID)[0])
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Attach(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Entry(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .Returns(Mock.Create<DbEntityEntry<Job>>())
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.SaveChanges())
                .IgnoreInstance()
                .MustBeCalled();

            var delta = new Delta<Job>(typeof(Job));
            delta.TrySetPropertyValue("Id", "3");
            delta.TrySetPropertyValue("CreatedBy", ANOTHER_USER_ID);
            delta.TrySetPropertyValue("ModifiedBy", ANOTHER_USER_ID);
            delta.TrySetPropertyValue("Parameters", "testparameters");
            var actionResult = _jobsController.Patch(1, delta).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkNegotiatedContentResult<Job>));

            var response = actionResult as OkNegotiatedContentResult<Job>;
            var job = response.Content;
            Assert.AreEqual(1, job.Id);
            Assert.AreEqual(DateTimeOffset.Now.Date, job.Modified.Date);
            Assert.AreEqual(CURRENT_USER_ID, job.CreatedBy);
            Assert.AreEqual(CURRENT_USER_ID, job.ModifiedBy);
            Assert.AreEqual("testparameters", job.Parameters);
            Assert.AreEqual(StateEnum.Running.ToString(), job.State);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_UPDATE_PERMISSION));
            Mock.Assert(() => CurrentUserDataProvider.GetCurrentUserId());
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void PatchJobForUserWithoutUpdatePermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_UPDATE_PERMISSION))
                .Returns(false)
                .MustBeCalled();

            var actionResult = _jobsController.Patch(1, new Delta<Job>()).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(StatusCodeResult));
            var response = (StatusCodeResult)actionResult;
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_UPDATE_PERMISSION));
        }

        [TestMethod]
        public void PatchJobForUserWithoutOwnershipReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_UPDATE_PERMISSION))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.GetCurrentUserId())
                .Returns(ANOTHER_USER_ID)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleJobDbSetForUser(CURRENT_USER_ID)[0])
                .MustBeCalled();
            var actionResult = _jobsController.Patch(1, new Delta<Job>()).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(StatusCodeResult));
            var response = (StatusCodeResult)actionResult;
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_UPDATE_PERMISSION));
            Mock.Assert(() => CurrentUserDataProvider.GetCurrentUserId());
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void PatchForNonExistingJobIdReturnsNotFound()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_UPDATE_PERMISSION))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns((Job)null)
                .MustBeCalled();
            var actionResult = _jobsController.Patch(1, new Delta<Job>()).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(StatusCodeResult));
            var response = (StatusCodeResult)actionResult;
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_UPDATE_PERMISSION));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void DeleteJobForUserWithDeletePermissionAndOwnershipDeletesJob()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_DELETE_PERMISSION))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.GetCurrentUserId())
                .Returns(ANOTHER_USER_ID)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleJobDbSetForUser(CURRENT_USER_ID)[0])
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Remove(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .MustBeCalled();
            var actionResult = _jobsController.Delete(1).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(StatusCodeResult));
            var response = (StatusCodeResult)actionResult;
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_DELETE_PERMISSION));
            Mock.Assert(() => CurrentUserDataProvider.GetCurrentUserId());
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void DeleteJobForUserWithoutDeletePermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_DELETE_PERMISSION))
                .Returns(false)
                .MustBeCalled();

            var actionResult = _jobsController.Delete(1).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(StatusCodeResult));
            var response = (StatusCodeResult)actionResult;
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_DELETE_PERMISSION));
        }

        [TestMethod]
        public void DeleteJobForUserWithoutOwnershipReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_DELETE_PERMISSION))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.GetCurrentUserId())
                .Returns(ANOTHER_USER_ID)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleJobDbSetForUser(CURRENT_USER_ID)[0])
                .MustBeCalled();
            var actionResult = _jobsController.Delete(1).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(StatusCodeResult));
            var response = (StatusCodeResult)actionResult;
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_DELETE_PERMISSION));
            Mock.Assert(() => CurrentUserDataProvider.GetCurrentUserId());
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void DeleteForNonExistingJobIdReturnsNotFound()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_DELETE_PERMISSION))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns((Job)null)
                .MustBeCalled();
            var actionResult = _jobsController.Delete(1).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(StatusCodeResult));
            var response = (StatusCodeResult)actionResult;
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_DELETE_PERMISSION));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void RunForUserWithoutRunPermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_RUN_PERMISSION))
                .Returns(false)
                .MustBeCalled();

            var actionResult = _jobsController.Run(1, null).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(StatusCodeResult));
            var response = (StatusCodeResult)actionResult;
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_RUN_PERMISSION));
        }

        [TestMethod]
        public void RunForUserWithoutOwnerShipReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_RUN_PERMISSION))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.GetCurrentUserId())
                .Returns(ANOTHER_USER_ID)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleJobDbSetForUser(CURRENT_USER_ID)[0])
                .MustBeCalled();

            var actionResult = _jobsController.Run(1, null).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(StatusCodeResult));
            var response = (StatusCodeResult)actionResult;
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_RUN_PERMISSION));
            Mock.Assert(() => CurrentUserDataProvider.GetCurrentUserId());
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void RunForUserForNonExistingEntityReturnsNotFound()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_RUN_PERMISSION))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns((Job)null)
                .MustBeCalled();

            var actionResult = _jobsController.Run(1, null).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(StatusCodeResult));
            var response = (StatusCodeResult)actionResult;
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_RUN_PERMISSION));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void RunForUserWithPermissionAndOwnershipSetsStateToRunningAndReturnsOk()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_RUN_PERMISSION))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.GetCurrentUserId())
                .Returns(CURRENT_USER_ID)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(2))
                .IgnoreInstance()
                .Returns(CreateSampleJobDbSetForUser(CURRENT_USER_ID)[1])
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Attach(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Entry(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .Returns(Mock.Create<DbEntityEntry<Job>>())
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.SaveChanges())
                .IgnoreInstance()
                .MustBeCalled();

            var actionResult = _jobsController.Run(2, null).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkNegotiatedContentResult<String>));

            var response = actionResult as OkNegotiatedContentResult<String>;
            var jobState = response.Content;
            Assert.AreEqual(StateEnum.Running.ToString(), jobState);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_RUN_PERMISSION));
            Mock.Assert(() => CurrentUserDataProvider.GetCurrentUserId());
            Mock.Assert(_lifeCycleContext);
        }

        private ODataConventionModelBuilder GetBuilder()
        {
            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<Job>("Jobs");
            return builder;
        }

        private IList<Job> CreateSampleJobDbSetForUser(String ownerId)
        {
            var dbSet = new List<Job>();
            dbSet.Add(new Job { Id = 1, CreatedBy = ownerId, State = StateEnum.Running.ToString(), Type = DEFAULT_JOB_TYPE });
            dbSet.Add(new Job { Id = 2, CreatedBy = ownerId, State = StateEnum.Canceled.ToString(), Type = DEFAULT_JOB_TYPE });
            return dbSet;
        }
    }
}
