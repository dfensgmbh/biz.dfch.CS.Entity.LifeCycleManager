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
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.OData;
using System.Web.Http.OData.Builder;
using System.Web.Http.OData.Query;
using biz.dfch.CS.Entity.LifeCycleManager.Context;
using biz.dfch.CS.Entity.LifeCycleManager.Logging;
using biz.dfch.CS.Entity.LifeCycleManager.Model;
using biz.dfch.CS.Entity.LifeCycleManager.UserData;
using biz.dfch.CS.Entity.LifeCycleManager.Util;
using Microsoft.Data.OData;

namespace biz.dfch.CS.Entity.LifeCycleManager.Controller
{
    public class JobsController : ODataController
    {
        private const String _permissionInfix = "Job";
        private const String _permissionPrefix = "CumulusCore";
        private LifeCycleContext db = new LifeCycleContext();

        private static ODataValidationSettings _validationSettings = new ODataValidationSettings();

        public JobsController()
        {
            Debug.WriteLine("JobsController()");
        }

        internal static void ModelBuilder(ODataConventionModelBuilder builder)
        {
            var EntitySetName = "Jobs";

            builder.EntitySet<Job>(EntitySetName);

            builder.Entity<Job>().Action("Run").Returns<String>();
        }

        // GET: api/Core.svc/Jobs
        [EnableQuery(PageSize = 45)]
        public async Task<IHttpActionResult> GetJobs(ODataQueryOptions<Job> queryOptions)
        {
            String fn = String.Format("{0}:{1}", 
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace, 
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

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
                var currentUserId = CurrentUserDataProvider.GetCurrentUserId();
                var jobs = from job in db.Jobs
                    where job.CreatedBy == currentUserId
                    select job;
                    
                return Ok<IEnumerable<Job>>(jobs);
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
            var fn = String.Format("{0}:{1}", 
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

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
                var job = db.Jobs.Find(key);
                if (null == job)
                {
                    return StatusCode(HttpStatusCode.NotFound);
                }
                if (!CurrentUserDataProvider.GetCurrentUserId().Equals(job.CreatedBy))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
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
            var fn = String.Format("{0}:{1}",
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

            if (!ModelState.IsValid)
            {
                Debug.WriteLine("Entity to be updated has invalid ModelState.");
                return BadRequest(ModelState);
            }

            if (key != job.Id)
            {
                return BadRequest();
            }

            try
            {
                Debug.WriteLine(fn);
                
                var permissionId = CreatePermissionId("CanUpdate");
                if (!CurrentUserDataProvider.HasCurrentUserPermission(permissionId))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }
                var original = db.Jobs.Find(job.Id);
                if (null == original)
                {
                    return StatusCode(HttpStatusCode.NotFound);
                }
                if (!CurrentUserDataProvider.GetCurrentUserId().Equals(original.CreatedBy))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }
                job.Created = original.Created;
                job.CreatedBy = original.CreatedBy;
                job.Modified = DateTimeOffset.Now;
                job.ModifiedBy = CurrentUserDataProvider.GetCurrentUserId();
                db.Jobs.Attach(job);
                db.Entry(job).State = EntityState.Modified;
                db.SaveChanges();
                return Ok<Job>(job);
            }
            catch (Exception e)
            {
                Debug.WriteLine(String.Format("{0}: {1}\r\n{2}", e.Source, e.Message, e.StackTrace));
                throw;
            }
        }

        // POST: api/Core.svc/Jobs
        public async Task<IHttpActionResult> Post(Job job)
        {
            var fn = String.Format("{0}:{1}",
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

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
                Debug.WriteLine("Saving new job");

                var jobEntity = new Job()
                {
                    Created = DateTimeOffset.Now,
                    CreatedBy = CurrentUserDataProvider.GetCurrentUserId(),
                    Type = null == job.Type ? "DefaultJob" : job.Type,
                    State = job.State,
                    Parameters = job.Parameters,
                };
                jobEntity = db.Jobs.Add(jobEntity);
                Debug.WriteLine("Created job with id '{0}'", jobEntity.Id);
                db.SaveChanges();
                return ResponseMessage(ODataControllerHelper.ResponseCreated(this, jobEntity));
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
            var fn = String.Format("{0}:{1}",
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                Debug.WriteLine(fn);

                var permissionId = CreatePermissionId("CanUpdate");
                if (!CurrentUserDataProvider.HasCurrentUserPermission(permissionId))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }
                var job = db.Jobs.Find(key);
                if (null == job)
                {
                    return StatusCode(HttpStatusCode.NotFound);
                }
                if (!CurrentUserDataProvider.GetCurrentUserId().Equals(job.CreatedBy))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }
                var id = job.Id;
                var created = job.Created;
                var createdBy = job.CreatedBy;
                delta.Patch(job);
                job.Id = id;
                job.Created = created;
                job.CreatedBy = createdBy;
                job.Modified = DateTimeOffset.Now;
                job.ModifiedBy = CurrentUserDataProvider.GetCurrentUserId();
                db.Jobs.Attach(job);
                db.Entry(job).State = EntityState.Modified;
                db.SaveChanges();
                return Ok<Job>(job);
            }
            catch (Exception e)
            {
                Debug.WriteLine(String.Format("{0}: {1}\r\n{2}", e.Source, e.Message, e.StackTrace));
                throw;
            }
        }

        // DELETE: api/Core.svc/Jobs(5)
        public async Task<IHttpActionResult> Delete([FromODataUri] int key)
        {
            var fn = String.Format("{0}:{1}", 
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace, 
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

            try
            {
                Debug.WriteLine(fn);

                var permissionId = CreatePermissionId("CanDelete");
                if (!CurrentUserDataProvider.HasCurrentUserPermission(permissionId))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }
                var job = db.Jobs.Find(key);
                if (null == job)
                {
                    return StatusCode(HttpStatusCode.NotFound);
                }
                if (!CurrentUserDataProvider.GetCurrentUserId().Equals(job.CreatedBy))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }
                db.Jobs.Remove(job);
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
            var fn = String.Format("{0}:{1}", 
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
            Debug.WriteLine(fn);

            try
            {
                var permissionId = CreatePermissionId("CanRun");
                if (!CurrentUserDataProvider.HasCurrentUserPermission(permissionId))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }

                var job = db.Jobs.Find(key);
                if (null == job)
                {
                    return StatusCode(HttpStatusCode.NotFound);
                }
                if (!CurrentUserDataProvider.GetCurrentUserId().Equals(job.CreatedBy))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }
                job.Modified = DateTimeOffset.Now;
                job.ModifiedBy = CurrentUserDataProvider.GetCurrentUserId();
                job.State = StateEnum.Running.ToString();
                db.Jobs.Attach(job);
                db.Entry(job).State = EntityState.Modified;
                db.SaveChanges();
                return Ok<String>(job.State);
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
