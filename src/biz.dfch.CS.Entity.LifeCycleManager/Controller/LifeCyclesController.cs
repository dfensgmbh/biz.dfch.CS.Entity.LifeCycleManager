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
            String fn = String.Format("{0}:{1}",
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

            try
            {
                queryOptions.Validate(_validationSettings);
            }
            catch (ODataException ex)
            {
                Debug.WriteLine(String.Format("{0}: {1}\r\n{2}", ex.Source, ex.Message, ex.StackTrace));
                return BadRequest(ex.Message);
            }

            Debug.WriteLine(fn);

            // return Ok<IEnumerable<LifeCycle>>(lifeCycles);
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        // GET: api/Utilities.svc/LifeCycles(5)
        public async Task<IHttpActionResult> GetLifeCycle([FromODataUri] String key, ODataQueryOptions<LifeCycle> queryOptions)
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
                Debug.WriteLine(String.Format("{0}: {1}\r\n{2}", ex.Source, ex.Message, ex.StackTrace));
                return BadRequest(ex.Message);
            }
            
            Debug.WriteLine(fn);

            // return Ok<LifeCycle>(LifeCycle);
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        // PUT: api/Utilities.svc/LifeCycles(5)
        public async Task<IHttpActionResult> Put([FromODataUri] String key, LifeCycle lifeCycle)
        {
            String fn = String.Format("{0}:{1}",
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!key.Equals(lifeCycle.Id))
            {
                return BadRequest();
            }

            Debug.WriteLine(fn);

            // DFTODO Load entity and check if user is permitted
            // DFTODO change state with given condition

            // return Updated(LifeCycle);
            // DFTODO Check what to return
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        // POST: api/Utilities.svc/LifeCycles
        public async Task<IHttpActionResult> Post(LifeCycle lifeCycle)
        {
            String fn = String.Format("{0}:{1}",
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Debug.WriteLine(fn);

            // TODO: Add create logic here.

            // return Created(LifeCycle);
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        // PATCH: api/Utilities.svc/LifeCycles(5)
        [AcceptVerbs("PATCH", "MERGE")]
        public async Task<IHttpActionResult> Patch([FromODataUri] String key, Delta<LifeCycle> delta)
        {
            String fn = String.Format("{0}:{1}",
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Debug.WriteLine(fn);

            // DFTODO Load entity and check if user is permitted
            // DFTODO change state with given condition

            // delta.Patch(LifeCycle);

            // return Updated(LifeCycle);
            // DFTODO Check what to return
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        // DELETE: api/Utilities.svc/LifeCycles(5)
        public async Task<IHttpActionResult> Delete([FromODataUri] String key)
        {
            String fn = String.Format("{0}:{1}",
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
            
            // TODO: Add delete logic here.

            Debug.WriteLine(fn);
            // return StatusCode(HttpStatusCode.NoContent);
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Next([FromODataUri] String key, ODataActionParameters parameters)
        {
            String fn = String.Format("{0}:{1}",
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
            Debug.WriteLine(fn);

            try
            {
                var permissionId = CreatePermissionId("CanNext");
                if (!CurrentUserDataProvider.HasCurrentUserPermission(permissionId))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }

                // DFTODO Load entity and check if user is permitted
                // DFTODO execute with continue condition

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
        public async Task<IHttpActionResult> Cancel([FromODataUri] String key, ODataActionParameters parameters)
        {
            String fn = String.Format("{0}:{1}",
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
            Debug.WriteLine(fn);

            try
            {
                var permissionId = CreatePermissionId("CanCancel");
                if (!CurrentUserDataProvider.HasCurrentUserPermission(permissionId))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }

                // DFTODO Load entity and check if user is permitted
                // DFTODO handle cancellation (revert logic)

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
        public async Task<IHttpActionResult> Allow([FromODataUri] String key, ODataActionParameters parameters)
        {
            String fn = String.Format("{0}:{1}",
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
            Debug.WriteLine(fn);

            try
            {
                var permissionId = CreatePermissionId("CanAllow");
                if (!CurrentUserDataProvider.HasCurrentUserPermission(permissionId))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }

                // DFTODO Load entity and check if user is permitted
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
        public async Task<IHttpActionResult> Decline([FromODataUri] String key, ODataActionParameters parameters)
        {
            String fn = String.Format("{0}:{1}",
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
            Debug.WriteLine(fn);

            try
            {
                var permissionId = CreatePermissionId("CanDecline");
                if (!CurrentUserDataProvider.HasCurrentUserPermission(permissionId))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }

                // DFTODO Load entity and check if user is permitted
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
