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
﻿using System.ComponentModel.DataAnnotations;

namespace biz.dfch.CS.Entity.LifeCycleManager.Contracts.Entity
{
    public class BaseEntity
    {
        [Key]
        public int Id { get; set; }
        private Guid _Tid { get; set; }
        public String Tid
        {
            get { return _Tid.ToString(); }
            set { _Tid = Guid.Parse(value); }
        }
        public String CreatedBy { get; set; }
        public String ModifiedBy { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset Modified { get; set; }
    }
}
