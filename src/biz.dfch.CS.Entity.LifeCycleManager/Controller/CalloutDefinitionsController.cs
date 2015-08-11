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
    public class CalloutDefinitionsController : ODataController
    {
        private const String _permissionInfix = "CalloutDefinition";
        private const String _permissionPrefix = "CumulusCore";
        private LifeCycleContext db = new LifeCycleContext();

        private static ODataValidationSettings _validationSettings = new ODataValidationSettings();

        public CalloutDefinitionsController()
        {
            String fn = String.Format("{0}:{1}",
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
            Debug.WriteLine(fn);
        }

        internal static void ModelBuilder(ODataConventionModelBuilder builder)
        {
            var EntitySetName = "CalloutDefinitions";

            builder.EntitySet<CalloutDefinition>(EntitySetName);
        }

        // GET: api/Core.svc/CalloutDefinitions
        [EnableQuery(PageSize = 45)]
        public async Task<IHttpActionResult> GetCalloutDefinitions(ODataQueryOptions<CalloutDefinition> queryOptions)
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
                var calloutDefinitions = from calloutDefinition in db.CalloutDefinitions
                           where calloutDefinition.CreatedBy == currentUserId
                           select calloutDefinition;

                return Ok<IEnumerable<CalloutDefinition>>(calloutDefinitions);
            }
            catch (Exception e)
            {
                Debug.WriteLine(String.Format("{0}: {1}\r\n{2}", e.Source, e.Message, e.StackTrace));
                throw;
            }
        }

        // GET: api/Core.svc/CalloutDefinitions(5)
        public async Task<IHttpActionResult> GetCalloutDefinition([FromODataUri] int key, ODataQueryOptions<CalloutDefinition> queryOptions)
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
                var calloutDefinition = db.CalloutDefinitions.Find(key);
                if (null == calloutDefinition)
                {
                    return StatusCode(HttpStatusCode.NotFound);
                }
                if (!CurrentUserDataProvider.GetCurrentUserId().Equals(calloutDefinition.CreatedBy))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }
                return Ok<CalloutDefinition>(calloutDefinition);
            }
            catch (Exception e)
            {
                Debug.WriteLine(String.Format("{0}: {1}\r\n{2}", e.Source, e.Message, e.StackTrace));
                throw;
            }
        }

        // PUT: api/Core.svc/CalloutDefinitions(5)
        public async Task<IHttpActionResult> Put([FromODataUri] int key, CalloutDefinition calloutDefinition)
        {
            var fn = String.Format("{0}:{1}",
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

            if (!ModelState.IsValid)
            {
                Debug.WriteLine("Entity to be updated has invalid ModelState.");
                return BadRequest(ModelState);
            }

            if (key != calloutDefinition.Id)
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
                var original = db.CalloutDefinitions.Find(calloutDefinition.Id);
                if (null == original)
                {
                    return StatusCode(HttpStatusCode.NotFound);
                }
                if (!CurrentUserDataProvider.GetCurrentUserId().Equals(original.CreatedBy))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }
                calloutDefinition.Created = original.Created;
                calloutDefinition.CreatedBy = original.CreatedBy;
                calloutDefinition.Modified = DateTimeOffset.Now;
                calloutDefinition.ModifiedBy = CurrentUserDataProvider.GetCurrentUserId();
                db.CalloutDefinitions.Attach(calloutDefinition);
                db.Entry(calloutDefinition).State = EntityState.Modified;
                db.SaveChanges();
                return Ok<CalloutDefinition>(calloutDefinition);
            }
            catch (Exception e)
            {
                Debug.WriteLine(String.Format("{0}: {1}\r\n{2}", e.Source, e.Message, e.StackTrace));
                throw;
            }
        }

        // POST: api/Core.svc/CalloutDefinitions
        public async Task<IHttpActionResult> Post(CalloutDefinition calloutDefinition)
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
                if (null == calloutDefinition)
                {
                    var errorMsg = "Entity to be created contains invalid data.";
                    Debug.WriteLine(errorMsg);
                    return BadRequest(errorMsg);
                }
                Debug.WriteLine("Saving new calloutDefinition");

                var calloutDefinitionEntity = new CalloutDefinition()
                {
                    Created = DateTimeOffset.Now,
                    CreatedBy = CurrentUserDataProvider.GetCurrentUserId(),
                    TenantId = calloutDefinition.TenantId,
                    EntityType = calloutDefinition.EntityType,
                    EntityId = calloutDefinition.EntityId,
                    Parameters = calloutDefinition.Parameters,
                };
                calloutDefinitionEntity = db.CalloutDefinitions.Add(calloutDefinitionEntity);
                Debug.WriteLine("Created calloutDefinition with id '{0}'", calloutDefinitionEntity.Id);
                db.SaveChanges();
                return ResponseMessage(ODataControllerHelper.ResponseCreated(this, calloutDefinitionEntity));
            }
            catch (Exception e)
            {
                Debug.WriteLine(String.Format("{0}: {1}\r\n{2}", e.Source, e.Message, e.StackTrace));
                throw;
            }
        }

        // PATCH: api/Core.svc/CalloutDefinitions(5)
        [AcceptVerbs("PATCH", "MERGE")]
        public async Task<IHttpActionResult> Patch([FromODataUri] int key, Delta<CalloutDefinition> delta)
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
                var calloutDefinition = db.CalloutDefinitions.Find(key);
                if (null == calloutDefinition)
                {
                    return StatusCode(HttpStatusCode.NotFound);
                }
                if (!CurrentUserDataProvider.GetCurrentUserId().Equals(calloutDefinition.CreatedBy))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }
                var id = calloutDefinition.Id;
                var created = calloutDefinition.Created;
                var createdBy = calloutDefinition.CreatedBy;
                delta.Patch(calloutDefinition);
                calloutDefinition.Id = id;
                calloutDefinition.Created = created;
                calloutDefinition.CreatedBy = createdBy;
                calloutDefinition.Modified = DateTimeOffset.Now;
                calloutDefinition.ModifiedBy = CurrentUserDataProvider.GetCurrentUserId();
                db.CalloutDefinitions.Attach(calloutDefinition);
                db.Entry(calloutDefinition).State = EntityState.Modified;
                db.SaveChanges();
                return Ok<CalloutDefinition>(calloutDefinition);
            }
            catch (Exception e)
            {
                Debug.WriteLine(String.Format("{0}: {1}\r\n{2}", e.Source, e.Message, e.StackTrace));
                throw;
            }
        }

        // DELETE: api/Core.svc/CalloutDefinitions(5)
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
                var calloutDefinition = db.CalloutDefinitions.Find(key);
                if (null == calloutDefinition)
                {
                    return StatusCode(HttpStatusCode.NotFound);
                }
                if (!CurrentUserDataProvider.GetCurrentUserId().Equals(calloutDefinition.CreatedBy))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }
                db.CalloutDefinitions.Remove(calloutDefinition);
                return StatusCode(HttpStatusCode.NoContent);
            }
            catch (Exception e)
            {
                Debug.WriteLine(String.Format("{0}: {1}\r\n{2}", e.Source, e.Message, e.StackTrace));
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