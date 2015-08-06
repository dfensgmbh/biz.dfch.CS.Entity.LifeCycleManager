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
    public class JobController
    {
        public Boolean CreateJob(String entityId, String entityType, String condition, String sourceState, String targetState)
        {
            using (var db = new LifeCycleContext())
            {
                var job = new Job()
                {
                    Created = DateTimeOffset.Now,
                    State = StateEnum.PENDING.ToString(),
                    EntityId = entityId,
                    EntityType = entityType,
                    Condition = condition,
                    SourceState = sourceState,
                    TargetState = targetState
                };
                Debug.WriteLine("Saving job for entity of type '{0}' with id '{1}'", entityType, entityId);
                db.Jobs.Add(job);
                return 1 != db.SaveChanges() ? false : true;
            }
        }

        public Boolean FinishJob(String entityId, String entityType)
        {
            using (var db = new LifeCycleContext())
            {
                var job = db.Jobs.Where(j => j.EntityId.Equals(entityId) && j.EntityType.Equals(entityType) && j.State.Equals(StateEnum.PENDING.ToString())).FirstOrDefault();

                if (null == job)
                {
                    return false;
                }
                
                job.Updated = DateTimeOffset.Now;
                job.State = StateEnum.FINISHED.ToString();
                db.SaveChanges();
                
                return true;
            }
        }
    }
}
