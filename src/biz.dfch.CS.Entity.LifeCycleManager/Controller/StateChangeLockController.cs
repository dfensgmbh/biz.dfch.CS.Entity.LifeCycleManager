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
﻿using System.Linq;
﻿using biz.dfch.CS.Entity.LifeCycleManager.Context;
﻿using biz.dfch.CS.Entity.LifeCycleManager.Logging;
﻿using biz.dfch.CS.Entity.LifeCycleManager.Model;

namespace biz.dfch.CS.Entity.LifeCycleManager.Controller
{
    public class StateChangeLockController
    {
        public Boolean CreateStateChangeLock(String entityId, String entityType)
        {
            using (var db = new LifeCycleContext())
            {
                var stateChangeLock = new StateChangeLock
                {
                    Created = DateTimeOffset.Now,
                    EntityId = entityId,
                    EntityType = entityType
                };
                db.StateChangeLocks.Add(stateChangeLock);
                Debug.WriteLine("Saving state change lock for entity of type '{0}' with id '{1}'", entityType, entityId);
                return 1 != db.SaveChanges() ? false : true;
            }
        }

        public Boolean IsLocked(String entityId, String entityType)
        {
            using (var db = new LifeCycleContext())
            {
                var stateChangeLock = db.StateChangeLocks.Where(l => l.EntityId.Equals(entityId) && l.EntityType.Equals(entityType)).FirstOrDefault();
                return null == stateChangeLock ? false : true;
            }
        }

        public Boolean DeleteStateChangeLock(String entityId, String entityType)
        {
            var count = 0;
            using (var db = new LifeCycleContext())
            {
                var stateChangeLocks =
                    db.StateChangeLocks.Where(l => l.EntityId.Equals(entityId) && l.EntityType.Equals(entityType));
                count = stateChangeLocks.Count();
                Debug.WriteLine("Deleting state change lock for entity of type '{0}' with id '{1}'...", entityType, entityId);
                foreach (StateChangeLock stateChangeLock in stateChangeLocks)
                {
                    Debug.WriteLine("Entry with Id '{0}' deleted", stateChangeLock.Id);
                    db.StateChangeLocks.Remove(stateChangeLock);
                }
                db.SaveChanges();
            }
            return 1 != count ? false : true;
        }
    }
}
