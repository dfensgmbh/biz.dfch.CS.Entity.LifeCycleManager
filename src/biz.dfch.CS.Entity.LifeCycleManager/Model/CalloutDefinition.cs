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
using System.ComponentModel.DataAnnotations;

namespace biz.dfch.CS.Entity.LifeCycleManager.Model
{
    public class CalloutDefinition
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public String TenantId { get; set; }
        [Required]
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset Updated { get; set; }

        public String EntityId { get; set; }
        public String EntityType { get; set; }
        public String Parameters { get; set; }
    }
}
