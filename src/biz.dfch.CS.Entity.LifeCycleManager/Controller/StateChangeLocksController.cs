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

﻿using System;
﻿using System.Collections.Generic;
﻿using System.Linq;
﻿using System.Net;
using System.Threading.Tasks;
﻿using System.Web.Http;
﻿using System.Web.Http.OData;
﻿using System.Web.Http.OData.Builder;
﻿using System.Web.Http.OData.Query;
﻿using biz.dfch.CS.Entity.LifeCycleManager.Context;
﻿using biz.dfch.CS.Entity.LifeCycleManager.Logging;
﻿using biz.dfch.CS.Entity.LifeCycleManager.Model;
﻿using biz.dfch.CS.Entity.LifeCycleManager.UserData;
﻿using biz.dfch.CS.Entity.LifeCycleManager.Util;
﻿using Microsoft.Data.OData;

namespace biz.dfch.CS.Entity.LifeCycleManager.Controller
{
    public class StateChangeLocksController : ODataController
    {
        private const String _permissionInfix = "StateChangeLock";
        private const String _permissionPrefix = "CumulusCore";
        private LifeCycleContext db = new LifeCycleContext();

        private static ODataValidationSettings _validationSettings = new ODataValidationSettings();

        public StateChangeLocksController()
        {
            String fn = String.Format("{0}:{1}",
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
            Debug.WriteLine(fn);
        }

        internal static void ModelBuilder(ODataConventionModelBuilder builder)
        {
            var EntitySetName = "StateChangeLocks";

            builder.EntitySet<StateChangeLock>(EntitySetName);
        }

        // GET: api/Utilities.svc/StateChangeLocks
        public async Task<IHttpActionResult> GetStateChangeLocks(ODataQueryOptions<StateChangeLock> queryOptions)
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
                var stateChangeLocks = from stateChangeLock in db.StateChangeLocks
                           where stateChangeLock.CreatedBy == currentUserId
                           select stateChangeLock;
                return Ok<IEnumerable<StateChangeLock>>(stateChangeLocks);
            }
            catch (Exception e)
            {
                Debug.WriteLine(String.Format("{0}: {1}\r\n{2}", e.Source, e.Message, e.StackTrace));
                throw;
            }
        }

        // GET: api/Utilities.svc/StateChangeLocks(5)
        public async Task<IHttpActionResult> GetStateChangeLock([FromODataUri] int key, ODataQueryOptions<StateChangeLock> queryOptions)
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

            // return Ok<StateChangeLock>(stateChangeLock);
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        // PUT: api/Utilities.svc/StateChangeLocks(5)
        public async Task<IHttpActionResult> Put([FromODataUri] int key, StateChangeLock stateChangeLock)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (key != stateChangeLock.Id)
            {
                return BadRequest();
            }

            // TODO: Add replace logic here.

            // return Updated(stateChangeLock);
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        // POST: api/Utilities.svc/StateChangeLocks
        public async Task<IHttpActionResult> Post(StateChangeLock stateChangeLock)
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
                if (null == stateChangeLock)
                {
                    var errorMsg = "Entity to be created contains invalid data.";
                    Debug.WriteLine(errorMsg);
                    return BadRequest(errorMsg);
                }
                Debug.WriteLine("Saving new state change lock for entity of type '{0}' and id '{1}'", 
                    stateChangeLock.EntityType,
                    stateChangeLock.EntityId);

                var stateChangeLockEntity = new StateChangeLock()
                {
                    CreatedBy = CurrentUserDataProvider.GetCurrentUserId(),
                    Created = DateTimeOffset.Now,
                    EntityId = stateChangeLock.EntityId,
                    EntityType = stateChangeLock.EntityType,
                };
                stateChangeLockEntity = db.StateChangeLocks.Add(stateChangeLockEntity);
                db.SaveChanges();
                Debug.WriteLine("Created job with id '{0}'", stateChangeLockEntity.Id);
                return ResponseMessage(ODataControllerHelper.ResponseCreated(this, stateChangeLockEntity));
            }
            catch (Exception e)
            {
                Debug.WriteLine(String.Format("{0}: {1}\r\n{2}", e.Source, e.Message, e.StackTrace));
                throw;
            }
        }

        // PATCH: api/Utilities.svc/StateChangeLocks(5)
        [AcceptVerbs("PATCH", "MERGE")]
        public async Task<IHttpActionResult> Patch([FromODataUri] int key, Delta<StateChangeLock> delta)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // TODO: Get the entity here.

            // delta.Patch(stateChangeLock);

            // TODO: Save the patched entity.

            // return Updated(stateChangeLock);
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        // DELETE: api/Utilities.svc/StateChangeLocks(5)
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
                var stateChangeLock = db.StateChangeLocks.Find(key);
                if (null == stateChangeLock)
                {
                    return StatusCode(HttpStatusCode.NotFound);
                }
                if (!CurrentUserDataProvider.GetCurrentUserId().Equals(stateChangeLock.CreatedBy))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }
                Debug.WriteLine("Deleting state change lock for entity of type '{0}' with id '{1}'...",
                    stateChangeLock.EntityType, stateChangeLock.EntityId);
                db.StateChangeLocks.Remove(stateChangeLock);
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
