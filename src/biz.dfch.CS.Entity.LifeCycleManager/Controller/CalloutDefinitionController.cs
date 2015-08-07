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
using System.Linq;
using biz.dfch.CS.Entity.LifeCycleManager.Context;
using biz.dfch.CS.Entity.LifeCycleManager.Model;

namespace biz.dfch.CS.Entity.LifeCycleManager.Controller
{
    public class CalloutDefinitionController
    {
        public String LoadCalloutDefinition(String entityId, String entityType)
        {
            using (var db = new LifeCycleContext())
            {
                var definition = db
                    .Where(j => j.EntityId.Equals(entityId) && 
                        j.EntityType.Equals(entityType) && 
                        j.State.Equals(StateEnum.PENDING.ToString()))
                    .FirstOrDefault();

                if (null == definition)
                {
                    return false;
                }

                definition.Updated = DateTimeOffset.Now;
                definition.State = StateEnum.FINISHED.ToString();
                db.SaveChanges();

                return true;
            }
        }
    }
}
