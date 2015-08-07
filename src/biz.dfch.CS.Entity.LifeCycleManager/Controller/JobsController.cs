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
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.OData;
using System.Web.Http.OData.Query;
using biz.dfch.CS.Entity.LifeCycleManager.Context;
using biz.dfch.CS.Entity.LifeCycleManager.Logging;
using biz.dfch.CS.Entity.LifeCycleManager.Model;
using biz.dfch.CS.Entity.LifeCycleManager.UserData;
using Microsoft.Data.OData;

namespace biz.dfch.CS.Entity.LifeCycleManager.Controller
{
    public class JobsController : ODataController
    {
        private const String _permissionInfix = "Job";
        private const String _permissionPrefix = "Cumulus.Core";

        private static ODataValidationSettings _validationSettings = new ODataValidationSettings();

        public JobsController()
        {
            Debug.WriteLine("JobsController()");
        }

        internal static void ModelBuilder(System.Web.Http.OData.Builder.ODataConventionModelBuilder builder)
        {
            var EntitySetName = "Jobs";

            builder.EntitySet<Job>(EntitySetName);

            builder.Entity<Job>().Action("Run")
                .Returns<String>();
        }

        // GET: api/Core.svc/Jobs
        [EnableQuery(PageSize = 45)]
        public async Task<IHttpActionResult> GetJobs(ODataQueryOptions<Job> queryOptions)
        {
            String fn = String.Format("{0}:{1}", System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace, System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

            try
            {
                queryOptions.Validate(_validationSettings);
            }
            catch (ODataException ex)
            {
                return BadRequest(ex.Message);
            }

            try
            {
                Debug.WriteLine(fn);

                var permissionId = CreatePermissionId("CanRead");
                if (!CurrentUserDataProvider.HasCurrentUserPermission(permissionId))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }
                using (var db = new LifeCycleContext())
                {
                    var jobs = db.Jobs;
                    return Ok<IEnumerable<Job>>(jobs);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(String.Format("{0}: {1}\r\n{2}", e.Source, e.Message, e.StackTrace));
                throw;
            }
        }

        // GET: api/Core.svc/Jobs(5)
        public async Task<IHttpActionResult> GetJob([FromODataUri] int key, ODataQueryOptions<Job> queryOptions)
        {
            var fn = String.Format("{0}:{1}", System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace, System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

            try
            {
                queryOptions.Validate(_validationSettings);
            }
            catch (ODataException ex)
            {
                return BadRequest(ex.Message);
            }

            try
            {
                Debug.WriteLine(fn);

                var permissionId = CreatePermissionId("CanRead");
                if (!CurrentUserDataProvider.HasCurrentUserPermission(permissionId))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }
                Job job = null;
                using (var db = new LifeCycleContext())
                {
                    job = db.Jobs
                        .Where(j => j.Id == key).FirstOrDefault();
                }
                if (null == job)
                {
                    return StatusCode(HttpStatusCode.NotFound);
                }
                return Ok<Job>(job);
            }
            catch (Exception e)
            {
                Debug.WriteLine(String.Format("{0}: {1}\r\n{2}", e.Source, e.Message, e.StackTrace));
                throw;
            }
        }

        // PUT: api/Core.svc/Jobs(5)
        public async Task<IHttpActionResult> Put([FromODataUri] int key, Job job)
        {
            var fn = String.Format("{0}:{1}", System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace, System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

            if (!ModelState.IsValid)
            {
                Debug.WriteLine("Entity to be updated has invalid ModelState.");
                return BadRequest(ModelState);
            }

            if (key != job.Id)
            {
                return BadRequest();
            }

            Debug.WriteLine(fn);
            // TODO: Add logic here.

            // return Updated(job);
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        // POST: api/Core.svc/Jobs
        public async Task<IHttpActionResult> Post(Job job)
        {
            var fn = String.Format("{0}:{1}", System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace, System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
            if (!ModelState.IsValid)
            {
                Debug.WriteLine("Entity to be created has invalid ModelState.");
                return BadRequest(ModelState);
            }

            try
            {
                Debug.WriteLine(fn);

                var permissionId = CreatePermissionId("CanCreate");
                if (!CurrentUserDataProvider.HasCurrentUserPermission(permissionId))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }
                if (null == job)
                {
                    var errorMsg = "Entity to be created contains invalid data.";
                    Debug.WriteLine(errorMsg);
                    return BadRequest(errorMsg);
                }
                Debug.WriteLine(String.Format("Trying to save job : '{0}'", job.Id));

                var jobEntity;
                using (var db = new LifeCycleContext())
                {
                    jobEntity = new Job()
                    {
                        Created = DateTimeOffset.Now,
                        State = StateEnum.PENDING.ToString(),
                        Type = job.Type,
                        EntityId = job.EntityId,
                        EntityType = job.EntityType,
                        Parameters = job.Parameters,
                    };
                    Debug.WriteLine("Saving job for entity of type '{0}' with id '{1}'", job.EntityType, job.EntityId);
                    db.Jobs.Add(job);
                    db.SaveChanges();
                }

                return Created(jobEntity);
            }
            catch (Exception e)
            {
                Debug.WriteLine(String.Format("{0}: {1}\r\n{2}", e.Source, e.Message, e.StackTrace));
                throw;
            }
        }

        // PATCH: api/Core.svc/Jobs(5)
        [AcceptVerbs("PATCH", "MERGE")]
        public async Task<IHttpActionResult> Patch([FromODataUri] int key, Delta<Job> delta)
        {
            var fn = String.Format("{0}:{1}", System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace, System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Debug.WriteLine(fn);
            // TODO: Get the entity here.

            // delta.Patch(job);

            // TODO: Save the patched entity.

            // return Updated(job);
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        // DELETE: api/Core.svc/Jobs(5)
        public async Task<IHttpActionResult> Delete([FromODataUri] int key)
        {
            var fn = String.Format("{0}:{1}", System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace, System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

            try
            {
                Debug.WriteLine(fn);

                var permissionId = CreatePermissionId("CanDelete");
                if (!CurrentUserDataProvider.HasCurrentUserPermission(permissionId))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }
                var entity =
                    (
                    from entry in _inMemoryDemoApprovals
                    where entry.Id.Equals(key)
                    select entry
                    )
                    .FirstOrDefault();
                if (null == entity) { return StatusCode(HttpStatusCode.NotFound); }
                if (NotAssignedToCurrentUserOrToGroupOfCurrentUser(CurrentUserDataProvider.GetCurrentUserId(), CurrentUserDataProvider.GetRolesOfCurrentUser(), entity))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }
                _inMemoryDemoApprovals.TryTake(out entity);
                return StatusCode(HttpStatusCode.NoContent);
            }
            catch (Exception e)
            {
                Debug.WriteLine(String.Format("{0}: {1}\r\n{2}", e.Source, e.Message, e.StackTrace));
                throw;
            }
        }

        [HttpPost]
        public async Task<IHttpActionResult> Run([FromODataUri] int key, ODataActionParameters parameters)
        {
            var fn = String.Format("{0}:{1}", System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace, System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
            Debug.WriteLine(fn);

            try
            {
                var permissionId = CreatePermissionId("CanDecline");
                if (!CurrentUserDataProvider.HasCurrentUserPermission(permissionId))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }

                var entity =
                    (
                    from entry in _inMemoryDemoApprovals
                    where entry.Id.Equals(key)
                    select entry
                    ).FirstOrDefault();

                if (null == entity) { return StatusCode(HttpStatusCode.NotFound); }
                if (NotAssignedToCurrentUserOrToGroupOfCurrentUser(CurrentUserDataHelper.GetCurrentUserId(), CurrentUserDataHelper.GetRolesOfCurrentUser(), entity) || AlreadyApprovedOrDeclined(entity))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }
                var currentState = Models.Approval.StatusEnum.DECLINED.ToString();
                entity.Status = currentState;

                return Ok<String>(currentState);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(String.Format("{0}@{1}: {2}\r\n{3}", ex.GetType().Name, ex.Source, ex.Message, ex.StackTrace));
                throw;
            }
        }

        private String CreatePermissionId(String permissionSuffix)
        {
            return String.Format("{0}:{1}{2}", _permissionPrefix, _permissionInfix, permissionSuffix);
        }
    }
}