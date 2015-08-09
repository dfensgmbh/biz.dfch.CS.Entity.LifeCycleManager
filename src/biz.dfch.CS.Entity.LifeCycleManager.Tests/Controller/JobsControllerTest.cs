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
        private const String JOB_CREATE_PERMISSION = "CumulusCore:JobCanUpdate";
        private const String OWNER_ID = "owner";
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
                .Returns(OWNER_ID)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs)
                .IgnoreInstance()
                .ReturnsCollection(CreateSampleJobDbSetForUser(OWNER_ID))
                .MustBeCalled();

            var context = new ODataQueryContext(GetBuilder().GetEdmModel(), typeof(Job));
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/Utilities.svc/Jobs");

            var actionResult = _jobsController.GetJobs(new ODataQueryOptions<Job>(context, request)).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkNegotiatedContentResult<IEnumerable<Job>>));

            var response = actionResult as OkNegotiatedContentResult<IEnumerable<Job>>;
            Assert.IsNotNull(response);
            Assert.AreEqual(1, response.Content.Count());

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
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/Utilities.svc/Jobs");

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
                .Returns(OWNER_ID)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs)
                .IgnoreInstance()
                .ReturnsCollection(CreateSampleJobDbSetForUser(ANOTHER_USER_ID))
                .MustBeCalled();

            var context = new ODataQueryContext(GetBuilder().GetEdmModel(), typeof(Job));
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/Utilities.svc/Jobs");

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
        public void GetJobByIdForUserWithOwnershipAndReadPermissionReturnsDesiredJob()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_READ_PERMISSION))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.GetCurrentUserId())
                .Returns(OWNER_ID)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleJobDbSetForUser(OWNER_ID)[0])
                .MustBeCalled();

            var context = new ODataQueryContext(GetBuilder().GetEdmModel(), typeof(Job));
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/Utilities.svc/Jobs(1)");

            var actionResult = _jobsController.GetJob(1, new ODataQueryOptions<Job>(context, request)).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkNegotiatedContentResult<Job>));

            var response = actionResult as OkNegotiatedContentResult<Job>;
            var job = response.Content;
            Assert.AreEqual(1, job.Id);
            Assert.AreEqual(OWNER_ID, job.CreatedBy);

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
                .Returns(CreateSampleJobDbSetForUser(OWNER_ID)[0])
                .MustBeCalled();

            var context = new ODataQueryContext(GetBuilder().GetEdmModel(), typeof(Job));
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/Utilities.svc/Jobs(1)");

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
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/Utilities.svc/Jobs(1)");

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
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/Utilities.svc/Jobs(1)");

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
                .Returns(OWNER_ID)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleJobDbSetForUser(OWNER_ID)[0])
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

            var actionResult = _jobsController.Put(1, CreateTestJob(1, ANOTHER_USER_ID, StateEnum.Canceled.ToString())).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkNegotiatedContentResult<Job>));

            var response = actionResult as OkNegotiatedContentResult<Job>;
            var job = response.Content;
            Assert.AreEqual(1, job.Id);
            Assert.AreEqual(OWNER_ID, job.CreatedBy);
            Assert.AreEqual(OWNER_ID, job.ModifiedBy);
            Assert.AreEqual(StateEnum.Canceled.ToString(), job.State);

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

            var actionResult = _jobsController.Put(1, CreateTestJob(1, OWNER_ID, StateEnum.Canceled.ToString())).Result;

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
                .Returns(CreateSampleJobDbSetForUser(OWNER_ID)[0])
                .MustBeCalled();

            var actionResult = _jobsController.Put(1, CreateTestJob(1, ANOTHER_USER_ID, StateEnum.Canceled.ToString())).Result;

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
                .Returns(OWNER_ID)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleJobDbSetForUser(OWNER_ID)[0])
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

            var actionResult = _jobsController.Put(1, CreateTestJob(1, ANOTHER_USER_ID, StateEnum.Canceled.ToString())).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkNegotiatedContentResult<Job>));

            var response = actionResult as OkNegotiatedContentResult<Job>;
            var job = response.Content;
            Assert.AreEqual(1, job.Id);
            Assert.AreEqual(DateTimeOffset.Now.Date, job.Modified.Date);
            Assert.AreEqual(OWNER_ID, job.CreatedBy);
            Assert.AreEqual(OWNER_ID, job.ModifiedBy);
            Assert.AreEqual(StateEnum.Canceled.ToString(), job.State);

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

            var actionResult = _jobsController.Put(1, CreateTestJob(1, ANOTHER_USER_ID, StateEnum.Canceled.ToString())).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(StatusCodeResult));
            var response = (StatusCodeResult)actionResult;
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_UPDATE_PERMISSION));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void PutJobDifferentJobIdsInUrlAndBodyReturnsBadRequest()
        {
            var actionResult = _jobsController.Put(2, CreateTestJob(1, ANOTHER_USER_ID, StateEnum.Canceled.ToString())).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(BadRequestResult));
        }

        [TestMethod]
        public void PostJobForUserWithoutWritePermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_CREATE_PERMISSION))
                .Returns(false)
                .MustBeCalled();
            Mock.Arrange(() => ODataControllerHelper.ResponseCreated(Arg.IsAny<ODataController>(), Arg.IsAny<Job>(), Arg.IsAny<String>()))
                .Returns(new HttpResponseMessage(HttpStatusCode.Created));

            var actionResult = _jobsController.Post(CreateTestJob(1, ANOTHER_USER_ID, StateEnum.Canceled.ToString())).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(StatusCodeResult));
            var response = (StatusCodeResult)actionResult;
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_UPDATE_PERMISSION));
        }

        [TestMethod]
        public void PatchJobForUserWithUpdatePermissionAndOwnershipUpdatesDeliveredFields()
        {
            Mock.Arrange(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_UPDATE_PERMISSION))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.GetCurrentUserId())
                .Returns(OWNER_ID)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleJobDbSetForUser(OWNER_ID)[0])
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

            var actionResult = _jobsController.Patch(1, new Delta<Job>()).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkNegotiatedContentResult<Job>));

            var response = actionResult as OkNegotiatedContentResult<Job>;
            var job = response.Content;
            Assert.AreEqual(1, job.Id);
            Assert.AreEqual(DateTimeOffset.Now.Date, job.Modified.Date);
            Assert.AreEqual(OWNER_ID, job.CreatedBy);
            Assert.AreEqual(OWNER_ID, job.ModifiedBy);
            Assert.AreEqual(StateEnum.Canceled.ToString(), job.State);

            Mock.Assert(() => CurrentUserDataProvider.HasCurrentUserPermission(JOB_UPDATE_PERMISSION));
            Mock.Assert(() => CurrentUserDataProvider.GetCurrentUserId());
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        public void PatchJobForUserWithoutUpdatePermissionReturnsForbidden()
        {

        }

        [TestMethod]
        public void PatchJobForUserWithoutOwnershipReturnsForbidden()
        {

        }

        [TestMethod]
        public void PatchJobForAuthorizedUserSetsUpdatedDate()
        {

        }

        [TestMethod]
        public void PatchForNonExistingJobIdReturnsNotFound()
        {

        }

        [TestMethod]
        public void DeleteJobForUserWithDeletePermissionAndOwnershipDeletesJob()
        {

        }

        [TestMethod]
        public void DeleteJobForUserWithoutUpdatePermissionReturnsForbidden()
        {

        }

        [TestMethod]
        public void DeleteJobForUserWithoutOwnershipReturnsForbidden()
        {

        }

        [TestMethod]
        public void DeleteForNonExistingJobIdReturnsNotFound()
        {
            
        }

        // DFTODO impl tests for run

        private ODataConventionModelBuilder GetBuilder()
        {
            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<Job>("Jobs");
            return builder;
        }

        private IList<Job> CreateSampleJobDbSetForUser(String ownerId)
        {
            var dbSet = new List<Job>();
            dbSet.Add(new Job {Id = 1, CreatedBy = ownerId, State = StateEnum.Running.ToString()});
            dbSet.Add(new Job {Id = 2, CreatedBy = ownerId, State = StateEnum.Canceled.ToString()});
            return dbSet;
        }

        private Job CreateTestJob(int id, String ownerId, String state)
        {
            return new Job {Id = id,
                Type = "Test",
                CreatedBy = ownerId,
                State = state};
        }
    }
}
