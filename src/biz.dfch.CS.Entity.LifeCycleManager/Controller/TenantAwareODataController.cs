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
﻿using System.Configuration;
﻿using System.Web;
﻿using System.Web.Http.OData;

namespace biz.dfch.CS.Entity.LifeCycleManager.Controller
{
    public class TenantAwareODataController : ODataController
    {
        protected static String TenantIdHeaderKey = ConfigurationManager.AppSettings["TenantId.Header.Key"];

        public String TenantId { get; private set; }

        public TenantAwareODataController()
        {
            TenantId = HttpContext.Current.Request.Headers.Get(TenantIdHeaderKey);
            if (null == TenantId)
            {
                var cookie = HttpContext.Current.Request.Cookies.Get(TenantIdHeaderKey);
                if (null != cookie)
                {
                    TenantId = cookie.Value;
                }
            }
        }
    }
}
