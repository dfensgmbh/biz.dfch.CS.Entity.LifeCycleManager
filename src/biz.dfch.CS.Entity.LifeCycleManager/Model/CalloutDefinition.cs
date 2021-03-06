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
using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Entity;
using biz.dfch.CS.Utilities;

namespace biz.dfch.CS.Entity.LifeCycleManager.Model
{
    public class CalloutDefinition : BaseEntity
    {
        private CalloutDefinitionType _CalloutType { get; set; }

        [Required]
        public String CalloutType
        {
            get { return _CalloutType.ToString(); }
            set { _CalloutType = EnumUtil.Parse<CalloutDefinitionType>(value, true); }
        }
        [Required]
        public String TenantId { get; set; }
        public String EntityId { get; set; }
        public String EntityType { get; set; }
        [Required]
        public String Condition { get; set; }
        public String Parameters { get; set; }

        public CalloutDefinition()
        {
        }

        public enum CalloutDefinitionType
        {
            PreAndPost,
            Pre,
            Post
        }
    }
}
