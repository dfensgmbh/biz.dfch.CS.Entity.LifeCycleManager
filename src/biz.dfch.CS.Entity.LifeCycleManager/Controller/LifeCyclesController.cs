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
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.OData;
using System.Web.Http.OData.Builder;
using System.Web.Http.OData.Query;
using biz.dfch.CS.Entity.LifeCycleManager.Logging;
using biz.dfch.CS.Entity.LifeCycleManager.Model;
using biz.dfch.CS.Entity.LifeCycleManager.UserData;
using Microsoft.Data.OData;

namespace biz.dfch.CS.Entity.LifeCycleManager.Controller
{
    public class LifeCyclesController : ODataController
    {

        private const String _permissionInfix = "LifeCycle";
        private const String _permissionPrefix = "CumulusCore";

        private static ODataValidationSettings _validationSettings = new ODataValidationSettings();

        public LifeCyclesController()
        {
            String fn = String.Format("{0}:{1}",
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
            Debug.WriteLine(fn);
        }

        internal static void ModelBuilder(ODataConventionModelBuilder builder)
        {
            var EntitySetName = "LifeCycles";

            builder.EntitySet<LifeCycle>(EntitySetName);

            builder.Entity<LifeCycle>().Action("Next").Returns<String>();
            builder.Entity<LifeCycle>().Action("Cancel").Returns<String>();

            builder.Entity<LifeCycle>().Action("Allow").Returns<String>();
            builder.Entity<LifeCycle>().Action("Decline").Returns<String>();
        }

        // GET: api/Utilities.svc/LifeCycle
        public async Task<IHttpActionResult> GetLifeCycles(ODataQueryOptions<LifeCycle> queryOptions)
        {
            // validate the query.
            try
            {
                queryOptions.Validate(_validationSettings);
            }
            catch (ODataException ex)
            {
                return BadRequest(ex.Message);
            }

            // return Ok<IEnumerable<LifeCycle>>(lifeCycles);
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        // GET: api/Utilities.svc/LifeCycles(5)
        public async Task<IHttpActionResult> GetLifeCycle([FromODataUri] int key, ODataQueryOptions<LifeCycle> queryOptions)
        {
            // validate the query.
            try
            {
                queryOptions.Validate(_validationSettings);
            }
            catch (ODataException ex)
            {
                return BadRequest(ex.Message);
            }

            // return Ok<LifeCycle>(LifeCycle);
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        // PUT: api/Utilities.svc/LifeCycles(5)
        public async Task<IHttpActionResult> Put([FromODataUri] String key, LifeCycle lifeCycle)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!key.Equals(lifeCycle.Id))
            {
                return BadRequest();
            }

            // DFTODO change state with given condition
            // TODO: Add replace logic here.

            // return Updated(LifeCycle);
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        // POST: api/Utilities.svc/LifeCycles
        public async Task<IHttpActionResult> Post(LifeCycle lifeCycle)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // TODO: Add create logic here.

            // return Created(LifeCycle);
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        // PATCH: api/Utilities.svc/LifeCycles(5)
        [AcceptVerbs("PATCH", "MERGE")]
        public async Task<IHttpActionResult> Patch([FromODataUri] String key, Delta<LifeCycle> delta)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // TODO: Get the entity here.
            // DFTODO change state with given condition

            // delta.Patch(LifeCycle);

            // TODO: Save the patched entity.

            // return Updated(LifeCycle);
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        // DELETE: api/Utilities.svc/LifeCycles(5)
        public async Task<IHttpActionResult> Delete([FromODataUri] String key)
        {
            // TODO: Add delete logic here.

            // return StatusCode(HttpStatusCode.NoContent);
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Next([FromODataUri] int key, ODataActionParameters parameters)
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

                // DFTODO execute with continue condition

                return Ok();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(String.Format("{0}@{1}: {2}\r\n{3}", ex.GetType().Name, ex.Source, ex.Message, ex.StackTrace));
                throw;
            }
        }

        [HttpPost]
        public async Task<IHttpActionResult> Cancel([FromODataUri] int key, ODataActionParameters parameters)
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

                // DFTODO handle cancellation (revert logic)

                return Ok();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(String.Format("{0}@{1}: {2}\r\n{3}", ex.GetType().Name, ex.Source, ex.Message, ex.StackTrace));
                throw;
            }
        }

        [HttpPost]
        public async Task<IHttpActionResult> Allow([FromODataUri] int key, ODataActionParameters parameters)
        {
            var fn = String.Format("{0}:{1}", System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace, System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
            Debug.WriteLine(fn);

            try
            {
                var permissionId = CreatePermissionId("CanApprove");
                if (!CurrentUserDataProvider.HasCurrentUserPermission(permissionId))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }

                // DFTODO Load job
                // DFTODO Create new lifecycle manager instance
                // DFTODO Excecute pre or post LifeCycle on lifecycle manager depending on jobs parameters

                // DFTODO Check what to return
                return Ok();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(String.Format("{0}@{1}: {2}\r\n{3}", ex.GetType().Name, ex.Source, ex.Message, ex.StackTrace));
                throw;
            }
        }

        [HttpPost]
        public async Task<IHttpActionResult> Decline([FromODataUri] int key, ODataActionParameters parameters)
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

                // DFTODO Load job
                // DFTODO Create new lifecycle manager instance
                // DFTODO Excecute pre or post LifeCycle on lifecycle manager depending on jobs parameters

                // DFTODO Check what to return
                return Ok();
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
