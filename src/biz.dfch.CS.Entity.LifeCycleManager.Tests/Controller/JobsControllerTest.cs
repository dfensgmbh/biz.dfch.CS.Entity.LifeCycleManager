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
using System.Web;
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
    public class JobsControllerTest : BaseControllerTest<Job>
    {
        private JobsController _jobsController;
        private LifeCycleContext _lifeCycleContext;
        private const String JOB_READ_PERMISSION = "LightSwitchApplication:JobCanRead";
        private const String JOB_UPDATE_PERMISSION = "LightSwitchApplication:JobCanUpdate";
        private const String JOB_CREATE_PERMISSION = "LightSwitchApplication:JobCanCreate";
        private const String JOB_DELETE_PERMISSION = "LightSwitchApplication:JobCanDelete";
        private const String JOB_RUN_PERMISSION = "LightSwitchApplication:JobCanRun";
        private const String DEFAULT_JOB_TYPE = "Default";
        private const String SAMPLE_PARAMETERS = "{}";

        [TestInitialize]
        public void TestInitialize()
        {
            Mock.SetupStatic(typeof(ODataControllerHelper));
            Mock.SetupStatic(typeof(CurrentUserDataProvider));
            Mock.SetupStatic(typeof(HttpContext));

            Mock.Arrange(() => HttpContext.Current.Request.Headers.Get(TENANT_ID_HEADER_KEY))
                .Returns(TENANT_ID)
                .OccursOnce();

            _jobsController = new JobsController();
            _lifeCycleContext = Mock.Create<LifeCycleContext>();
        }

        [TestMethod]
        [WorkItem(16)]
        public void GetJobsForUserWithReadPermissionReturnsJobsTheUserIsAuthorizedFor()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { JOB_READ_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.GetEntitiesForUser(Arg.IsAny<DbSet<Job>>(), CURRENT_USER_ID, EMPTY_TENANT_ID))
                .ReturnsCollection(CreateSampleJobDbSet().ToList())
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs)
                .IgnoreInstance()
                .ReturnsCollection(CreateSampleJobDbSet())
                .MustBeCalled();

            var actionResult = _jobsController.GetJobs(
                CreateODataQueryOptions("http://localhost/api/Core.svc/Jobs"))
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkNegotiatedContentResult<IEnumerable<Job>>));

            var response = actionResult as OkNegotiatedContentResult<IEnumerable<Job>>;
            Assert.IsNotNull(response);
            Assert.AreEqual(2, response.Content.Count());

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(() => CurrentUserDataProvider.GetEntitiesForUser(Arg.IsAny<DbSet<Job>>(), CURRENT_USER_ID, EMPTY_TENANT_ID));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        [WorkItem(16)]
        public void GetJobsForUserWithoutReadPermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String>(),
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();

            var actionResult = _jobsController.GetJobs(
                CreateODataQueryOptions("http://localhost/api/Core.svc/Jobs"))
                .Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
        }

        [TestMethod]
        [WorkItem(16)]
        public void GetJobsForUserWithoutAnyJobReturnsEmptyList()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { JOB_READ_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.GetEntitiesForUser(Arg.IsAny<DbSet<Job>>(), CURRENT_USER_ID, EMPTY_TENANT_ID))
                .ReturnsCollection(new List<Job>())
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs)
                .IgnoreInstance()
                .ReturnsCollection(CreateSampleJobDbSet())
                .MustBeCalled();

            var actionResult = _jobsController.GetJobs(
                CreateODataQueryOptions("http://localhost/api/Core.svc/Jobs"))
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkNegotiatedContentResult<IEnumerable<Job>>));

            var response = actionResult as OkNegotiatedContentResult<IEnumerable<Job>>;
            Assert.IsNotNull(response);
            Assert.AreEqual(0, response.Content.Count());

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(() => CurrentUserDataProvider.GetEntitiesForUser(Arg.IsAny<DbSet<Job>>(), CURRENT_USER_ID, EMPTY_TENANT_ID));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        [WorkItem(16)]
        public void GetJobByIdForAuthorizedUserWithReadPermissionReturnsRequestedJob()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { JOB_READ_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, "", Arg.IsAny<Job>()))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleJobDbSet()[0])
                .MustBeCalled();

            var actionResult = _jobsController.GetJob(1, 
                CreateODataQueryOptions("http://localhost/api/Core.svc/Jobs(1)"))
                .Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkNegotiatedContentResult<Job>));

            var response = actionResult as OkNegotiatedContentResult<Job>;
            var job = response.Content;
            Assert.AreEqual(1, job.Id);
            Assert.AreEqual(CURRENT_USER_ID, job.CreatedBy);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<Job>()));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        [WorkItem(16)]
        public void GetJobByIdForUnauthorizedUserReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { JOB_READ_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<Job>()))
                .Returns(false)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleJobDbSet()[0])
                .MustBeCalled();

            var actionResult = _jobsController.GetJob(1,
                CreateODataQueryOptions("http://localhost/api/Core.svc/Jobs(1)"))
                .Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<Job>()));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        [WorkItem(16)]
        public void GetJobByIdForNonExistingJobIdReturnsNotFound()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { JOB_READ_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns((Job)null)
                .MustBeCalled();

            var actionResult = _jobsController.GetJob(1,
                CreateODataQueryOptions("http://localhost/api/Core.svc/Jobs(1)"))
                .Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.NotFound);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        [WorkItem(16)]
        public void GetJobByIdForUserWithoutReadPermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String>(),
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();

            var actionResult = _jobsController.GetJob(1,
                CreateODataQueryOptions("http://localhost/api/Core.svc/Jobs(1)"))
                .Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
        }

        [TestMethod]
        [WorkItem(16)]
        public void PutJobForAuthorizedUserWithUpdatePermissionUpdatesJob()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { JOB_UPDATE_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<Job>()))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleJobDbSet()[0])
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
                    Tid = ANOTHER_TENANT_ID,
                    State = JobStateEnum.Canceled.ToString(),
                    Parameters = SAMPLE_PARAMETERS,
                    Token = SAMPLE_TOKEN,
                    TenantId = ANOTHER_TENANT_ID
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
            Assert.AreEqual(TENANT_ID, job.Tid);
            Assert.AreEqual(JobStateEnum.Canceled.ToString(), job.State);
            Assert.AreEqual(SAMPLE_PARAMETERS, job.Parameters);
            Assert.AreEqual(SAMPLE_TOKEN, job.Token);
            Assert.AreEqual(ANOTHER_TENANT_ID, job.TenantId);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<Job>()));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        [WorkItem(16)]
        public void PutJobForUserWithoutUpdatePermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String>(),
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();

            var actionResult = _jobsController.Put(1,
                new Job { Id = 1 })
                .Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
        }

        [TestMethod]
        [WorkItem(16)]
        public void PutJobForUnauthorizedUserReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { JOB_UPDATE_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<Job>()))
                .Returns(false)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleJobDbSet()[0])
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
                    State = JobStateEnum.Canceled.ToString(),
                    Parameters = SAMPLE_PARAMETERS,
                }).Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<Job>()));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        [WorkItem(16)]
        public void PutJobForAuthorizedUserSetsUpdatedDate()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { JOB_UPDATE_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<Job>()))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleJobDbSet()[0])
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
                    State = JobStateEnum.Canceled.ToString(),
                    Parameters = SAMPLE_PARAMETERS,
                }).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkNegotiatedContentResult<Job>));

            var response = actionResult as OkNegotiatedContentResult<Job>;
            var job = response.Content;
            Assert.AreEqual(1, job.Id);
            Assert.AreEqual(CURRENT_USER_ID, job.CreatedBy);
            Assert.AreEqual(DateTimeOffset.Now.Date, job.Modified.Date);
            Assert.AreEqual(CURRENT_USER_ID, job.ModifiedBy);
            Assert.AreEqual(JobStateEnum.Canceled.ToString(), job.State);
            Assert.AreEqual("Test", job.Type);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<Job>()));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        [WorkItem(16)]
        public void PutJobForNonExistingJobIdReturnsNotFound()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { JOB_UPDATE_PERMISSION },
                    Username = CURRENT_USER_ID
                })
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
                    State = JobStateEnum.Canceled.ToString(),
                    Parameters = SAMPLE_PARAMETERS,
                }).Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.NotFound);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        [WorkItem(16)]
        public void PutJobWithDifferentJobIdsInUrlAndBodyReturnsBadRequest()
        {
            var actionResult = _jobsController.Put(2,
                new Job
                {
                    Id = 1,
                    Type = "Test",
                    State = JobStateEnum.Canceled.ToString(),
                    Parameters = SAMPLE_PARAMETERS,
                }).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(BadRequestResult));
        }

        [TestMethod]
        [WorkItem(16)]
        public void PostJobForUserWithoutCreatePermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String>(),
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();

            var actionResult = _jobsController.Post(
                new Job
                {
                    Id = 1,
                    Type = "Test",
                    State = JobStateEnum.Canceled.ToString(),
                    Parameters = SAMPLE_PARAMETERS,
                    Token = SAMPLE_TOKEN
                }).Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
        }

        [TestMethod]
        [WorkItem(16)]
        public void PostJobForUserWithCreatePermissionCreatesJobAndReturnsCreated()
        {
            Job createdJob = null;

            Mock.Arrange(() => ODataControllerHelper.ResponseCreated(
                Arg.IsAny<ODataController>(), Arg.IsAny<Job>(),
                Arg.IsAny<String>())).Returns(new HttpResponseMessage(HttpStatusCode.Created));
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { JOB_CREATE_PERMISSION },
                    Username = CURRENT_USER_ID
                })
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
                    Tid = TENANT_ID,
                    Type = "Test",
                    State = JobStateEnum.Running.ToString(),
                    Parameters = SAMPLE_PARAMETERS,
                    Token = SAMPLE_TOKEN,
                    TenantId = ANOTHER_TENANT_ID
                }).Result;

            Assert.AreEqual(CURRENT_USER_ID, createdJob.CreatedBy);
            Assert.AreEqual(DateTimeOffset.Now.Date, createdJob.Created.Date);
            Assert.IsNull(createdJob.ModifiedBy);
            Assert.AreEqual(TENANT_ID, createdJob.Tid);
            Assert.AreEqual(JobStateEnum.Running.ToString(), createdJob.State);
            Assert.AreEqual("Test", createdJob.Type);
            Assert.AreEqual(SAMPLE_PARAMETERS, createdJob.Parameters);
            Assert.AreEqual(ANOTHER_TENANT_ID, createdJob.TenantId);

            Assert.IsTrue(actionResult.GetType() == typeof(ResponseMessageResult));
            var response = actionResult as ResponseMessageResult;
            Assert.AreEqual(HttpStatusCode.Created , response.Response.StatusCode);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        [WorkItem(16)]
        public void PostJobForUserWithCreatePermissionCreatesJobWithDefaultStateAndTypeAndReturnsCreated()
        {
            Job createdJob = null;

            Mock.Arrange(() => ODataControllerHelper.ResponseCreated(
                Arg.IsAny<ODataController>(), Arg.IsAny<Job>(),
                Arg.IsAny<String>())).Returns(new HttpResponseMessage(HttpStatusCode.Created));
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { JOB_CREATE_PERMISSION },
                    Username = CURRENT_USER_ID
                })
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
                    Parameters = SAMPLE_PARAMETERS,
                    Token = SAMPLE_TOKEN
                }).Result;

            Assert.AreEqual(CURRENT_USER_ID, createdJob.CreatedBy);
            Assert.AreEqual(DateTimeOffset.Now.Date, createdJob.Created.Date);
            Assert.IsNull(createdJob.ModifiedBy);
            Assert.AreEqual(JobStateEnum.Configuring.ToString(), createdJob.State);
            Assert.AreEqual(DEFAULT_JOB_TYPE, createdJob.Type);
            Assert.AreEqual(SAMPLE_PARAMETERS, createdJob.Parameters);

            Assert.IsTrue(actionResult.GetType() == typeof(ResponseMessageResult));
            var response = actionResult as ResponseMessageResult;
            Assert.AreEqual(HttpStatusCode.Created, response.Response.StatusCode);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        [WorkItem(16)]
        public void PatchJobForAuthorizedUserWithUpdatePermissionUpdatesDeliveredFields()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { JOB_UPDATE_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<Job>()))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleJobDbSet()[0])
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
            delta.TrySetPropertyValue("Tid", ANOTHER_TENANT_ID);
            delta.TrySetPropertyValue("Parameters", SAMPLE_PARAMETERS);
            delta.TrySetPropertyValue("State", "Canceled");
            delta.TrySetPropertyValue("Token", SAMPLE_TOKEN);
            delta.TrySetPropertyValue("TenantId", ANOTHER_TENANT_ID);

            var actionResult = _jobsController.Patch(1, delta).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkNegotiatedContentResult<Job>));

            var response = actionResult as OkNegotiatedContentResult<Job>;
            var job = response.Content;
            Assert.AreEqual(1, job.Id);
            Assert.AreEqual(DateTimeOffset.Now.Date, job.Modified.Date);
            Assert.AreEqual(CURRENT_USER_ID, job.CreatedBy);
            Assert.AreEqual(CURRENT_USER_ID, job.ModifiedBy);
            Assert.AreEqual(TENANT_ID, job.Tid);
            Assert.AreEqual(SAMPLE_PARAMETERS, job.Parameters);
            Assert.AreEqual(JobStateEnum.Canceled.ToString(), job.State);
            Assert.AreEqual(SAMPLE_TOKEN, job.Token);
            Assert.AreEqual(ANOTHER_TENANT_ID, job.TenantId);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<Job>()));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        [WorkItem(16)]
        public void PatchJobForAuthorizedUserWithUpdatePermissionUpdatesDeliveredFields2()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { JOB_UPDATE_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<Job>()))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleJobDbSet()[0])
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
            delta.TrySetPropertyValue("Parameters", SAMPLE_PARAMETERS);
            var actionResult = _jobsController.Patch(1, delta).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkNegotiatedContentResult<Job>));

            var response = actionResult as OkNegotiatedContentResult<Job>;
            var job = response.Content;
            Assert.AreEqual(1, job.Id);
            Assert.AreEqual(DateTimeOffset.Now.Date, job.Modified.Date);
            Assert.AreEqual(CURRENT_USER_ID, job.CreatedBy);
            Assert.AreEqual(CURRENT_USER_ID, job.ModifiedBy);
            Assert.AreEqual(SAMPLE_PARAMETERS, job.Parameters);
            Assert.AreEqual(JobStateEnum.Running.ToString(), job.State);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<Job>()));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        [WorkItem(16)]
        public void PatchJobForUserWithoutUpdatePermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String>(),
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();

            var actionResult = _jobsController.Patch(1, new Delta<Job>()).Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
        }

        [TestMethod]
        [WorkItem(16)]
        public void PatchJobForUnauthorizedUserReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { JOB_UPDATE_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<Job>()))
                .Returns(false)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleJobDbSet()[0])
                .MustBeCalled();
            var actionResult = _jobsController.Patch(1, new Delta<Job>()).Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<Job>()));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        [WorkItem(16)]
        public void PatchForNonExistingJobIdReturnsNotFound()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { JOB_UPDATE_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns((Job)null)
                .MustBeCalled();
            var actionResult = _jobsController.Patch(1, new Delta<Job>()).Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.NotFound);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        [WorkItem(16)]
        public void DeleteJobForAuthorizedUserWithDeletePermissionDeletesJob()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { JOB_DELETE_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<Job>()))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleJobDbSet()[0])
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Remove(Arg.IsAny<Job>()))
                .IgnoreInstance()
                .MustBeCalled();
            var actionResult = _jobsController.Delete(1).Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.NoContent);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<Job>()));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        [WorkItem(16)]
        public void DeleteJobForUserWithoutDeletePermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String>(),
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();

            var actionResult = _jobsController.Delete(1).Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
        }

        [TestMethod]
        [WorkItem(16)]
        public void DeleteJobForUnauthorizedUserpReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { JOB_DELETE_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<Job>()))
                .Returns(false)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleJobDbSet()[0])
                .MustBeCalled();
            var actionResult = _jobsController.Delete(1).Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<Job>()));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        [WorkItem(16)]
        public void DeleteForNonExistingJobIdReturnsNotFound()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { JOB_DELETE_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns((Job)null)
                .MustBeCalled();
            var actionResult = _jobsController.Delete(1).Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.NotFound);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        [WorkItem(16)]
        public void RunForUserWithoutRunPermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String>(),
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();

            var actionResult = _jobsController.Run(1, null).Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
        }

        [TestMethod]
        [WorkItem(16)]
        public void RunForUnauthorizedUserReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { JOB_RUN_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<Job>()))
                .Returns(false)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleJobDbSet()[0])
                .MustBeCalled();

            var actionResult = _jobsController.Run(1, null).Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<Job>()));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        [WorkItem(16)]
        public void RunForUserForNonExistingEntityReturnsNotFound()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { JOB_RUN_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns((Job)null)
                .MustBeCalled();

            var actionResult = _jobsController.Run(1, null).Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.NotFound);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        [WorkItem(16)]
        public void RunForAuthorizedUserWithPermissionSetsStateToRunningAndReturnsOk()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { JOB_RUN_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<Job>()))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(2))
                .IgnoreInstance()
                .Returns(CreateSampleJobDbSet()[1])
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
            Assert.AreEqual(JobStateEnum.Running.ToString(), jobState);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<Job>()));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        [WorkItem(16)]
        public void FinishForUserWithoutRunPermissionReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String>(),
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();

            var actionResult = _jobsController.Finish(1, null).Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
        }

        [TestMethod]
        [WorkItem(16)]
        public void FinishForUnauthorizedUserReturnsForbidden()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { JOB_RUN_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<Job>()))
                .Returns(false)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns(CreateSampleJobDbSet()[0])
                .MustBeCalled();

            var actionResult = _jobsController.Finish(1, null).Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.Forbidden);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<Job>()));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        [WorkItem(16)]
        public void FinishForUserForNonExistingEntityReturnsNotFound()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { JOB_RUN_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(1))
                .IgnoreInstance()
                .Returns((Job)null)
                .MustBeCalled();

            var actionResult = _jobsController.Finish(1, null).Result;

            AssertStatusCodeResult(actionResult, HttpStatusCode.NotFound);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(_lifeCycleContext);
        }

        [TestMethod]
        [WorkItem(16)]
        public void FinishForAuthorizedUserWithPermissionSetsStateToFinishedAndReturnsOk()
        {
            Mock.Arrange(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID))
                .Returns(new Identity
                {
                    Permissions = new List<String> { JOB_RUN_PERMISSION },
                    Username = CURRENT_USER_ID
                })
                .MustBeCalled();
            Mock.Arrange(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<Job>()))
                .Returns(true)
                .MustBeCalled();
            Mock.Arrange(() => _lifeCycleContext.Jobs.Find(2))
                .IgnoreInstance()
                .Returns(CreateSampleJobDbSet()[1])
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

            var actionResult = _jobsController.Finish(2, null).Result;

            Assert.IsTrue(actionResult.GetType() == typeof(OkNegotiatedContentResult<String>));

            var response = actionResult as OkNegotiatedContentResult<String>;
            var jobState = response.Content;
            Assert.AreEqual(JobStateEnum.Finished.ToString(), jobState);

            Mock.Assert(() => CurrentUserDataProvider.GetIdentity(EMPTY_TENANT_ID));
            Mock.Assert(() => CurrentUserDataProvider.IsEntityOfUser(CURRENT_USER_ID, EMPTY_TENANT_ID, Arg.IsAny<Job>()));
            Mock.Assert(_lifeCycleContext);
        }

        private IList<Job> CreateSampleJobDbSet()
        {
            var dbSet = new List<Job>();
            dbSet.Add(new Job { Id = 1, CreatedBy = CURRENT_USER_ID, Tid = TENANT_ID, State = JobStateEnum.Running.ToString(), Type = DEFAULT_JOB_TYPE });
            dbSet.Add(new Job { Id = 2, CreatedBy = CURRENT_USER_ID, Tid = TENANT_ID, State = JobStateEnum.Canceled.ToString(), Type = DEFAULT_JOB_TYPE });
            return dbSet;
        }
    }
}
