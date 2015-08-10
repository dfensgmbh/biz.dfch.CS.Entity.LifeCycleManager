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
using System.Collections.Generic;
using System.Linq;
﻿using System.Net;
﻿using System.Text;
using System.Threading.Tasks;
﻿using System.Web.Http;
﻿using System.Web.Http.OData;
﻿using System.Web.Http.OData.Builder;
﻿using System.Web.Http.OData.Query;
﻿using biz.dfch.CS.Entity.LifeCycleManager.Context;
﻿using biz.dfch.CS.Entity.LifeCycleManager.Logging;
﻿using biz.dfch.CS.Entity.LifeCycleManager.Model;
﻿using Microsoft.Data.OData;

namespace biz.dfch.CS.Entity.LifeCycleManager.Controller
{
    public class StateChangeLocksController : ODataController
    {
        private const String _permissionInfix = "StateChangeLock";
        private const String _permissionPrefix = "CumulusCore";

        private static ODataValidationSettings _validationSettings = new ODataValidationSettings();

        public StateChangeLocksController()
        {
            Debug.WriteLine("StateChangeLocksController()");
        }

        internal static void ModelBuilder(ODataConventionModelBuilder builder)
        {
            var EntitySetName = "StateChangeLocks";

            builder.EntitySet<StateChangeLock>(EntitySetName);
        }

        // GET: api/Utilities.svc/StateChangeLocks
        public async Task<IHttpActionResult> GetStateChangeLocks(ODataQueryOptions<StateChangeLock> queryOptions)
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

            //using (var db = new LifeCycleContext())
            //{
            //    var stateChangeLock = db.StateChangeLocks
            //        .Where(l => l.EntityId.Equals(entityId) &&
            //            l.EntityType.Equals(entityType))
            //            .FirstOrDefault();
            //    return null == stateChangeLock ? false : true;
            //}

            // return Ok<IEnumerable<StateChangeLock>>(stateChangeLocks);
            return StatusCode(HttpStatusCode.NotImplemented);
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
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // TODO: Add create logic here.
            //using (var db = new LifeCycleContext())
            //{
            //    var stateChangeLock = new StateChangeLock
            //    {
            //        Created = DateTimeOffset.Now,
            //        EntityId = entityId,
            //        EntityType = entityType
            //    };
            //    db.StateChangeLocks.Add(stateChangeLock);
            //    Debug.WriteLine("Saving state change lock for entity of type '{0}' with id '{1}'", entityType, entityId);
            //    return 1 != db.SaveChanges() ? false : true;
            //}

            // return Created(stateChangeLock);
            return StatusCode(HttpStatusCode.NotImplemented);
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
            // TODO: Add delete logic here.
            //var count = 0;
            //using (var db = new LifeCycleContext())
            //{
            //    var stateChangeLocks =
            //        db.StateChangeLocks.Where(l => l.EntityId.Equals(entityId) && l.EntityType.Equals(entityType));
            //    count = stateChangeLocks.Count();
            //    Debug.WriteLine("Deleting state change lock for entity of type '{0}' with id '{1}'...", entityType, entityId);
            //    foreach (StateChangeLock stateChangeLock in stateChangeLocks)
            //    {
            //        Debug.WriteLine("Entry with Id '{0}' deleted", stateChangeLock.Id);
            //        db.StateChangeLocks.Remove(stateChangeLock);
            //    }
            //    db.SaveChanges();
            //}
            //return 1 != count ? false : true;


            // return StatusCode(HttpStatusCode.NoContent);
            return StatusCode(HttpStatusCode.NotImplemented);
        }
    }
}
