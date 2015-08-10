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
    public class CallbacksController : ODataController
    {

        private const String _permissionInfix = "Callback";
        private const String _permissionPrefix = "CumulusCore";

        private static ODataValidationSettings _validationSettings = new ODataValidationSettings();

        public CallbacksController()
        {
            Debug.WriteLine("CallbacksController()");
        }

        internal static void ModelBuilder(ODataConventionModelBuilder builder)
        {
            var EntitySetName = "Callbacks";

            builder.EntitySet<Callback>(EntitySetName);

            builder.Entity<Callback>().Action("Allow").Returns<String>();
            builder.Entity<Callback>().Action("Deny").Returns<String>();
        }

        // GET: api/Utilities.svc/Callbacks
        public async Task<IHttpActionResult> GetCallbacks(ODataQueryOptions<Callback> queryOptions)
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

            // return Ok<IEnumerable<Callback>>(callbacks);
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        // GET: api/Utilities.svc/Callbacks(5)
        public async Task<IHttpActionResult> GetCallback([FromODataUri] int key, ODataQueryOptions<Callback> queryOptions)
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

            // return Ok<Callback>(callback);
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        // PUT: api/Utilities.svc/Callbacks(5)
        public async Task<IHttpActionResult> Put([FromODataUri] int key, Callback callback)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (key != callback.Id)
            {
                return BadRequest();
            }

            // TODO: Add replace logic here.

            // return Updated(callback);
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        // POST: api/Utilities.svc/Callbacks
        public async Task<IHttpActionResult> Post(Callback callback)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // TODO: Add create logic here.

            // return Created(callback);
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        // PATCH: api/Utilities.svc/Callbacks(5)
        [AcceptVerbs("PATCH", "MERGE")]
        public async Task<IHttpActionResult> Patch([FromODataUri] int key, Delta<Callback> delta)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // TODO: Get the entity here.

            // delta.Patch(callback);

            // TODO: Save the patched entity.

            // return Updated(callback);
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        // DELETE: api/Utilities.svc/Callbacks(5)
        public async Task<IHttpActionResult> Delete([FromODataUri] int key)
        {
            // TODO: Add delete logic here.

            // return StatusCode(HttpStatusCode.NoContent);
            return StatusCode(HttpStatusCode.NotImplemented);
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
                // DFTODO Excecute pre or post callback on lifecycle manager depending on jobs parameters

                // DFTODO Check what to return
                return Ok<String>("");
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
                // DFTODO Excecute pre or post callback on lifecycle manager depending on jobs parameters

                // DFTODO Check what to return
                return Ok<String>("");
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
