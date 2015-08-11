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
﻿
using System;
﻿using System.Net;
﻿using System.Net.Http;
using System.Reflection;
using System.Web.Http;
﻿using System.Web.Http.OData;
﻿using System.Web.Http.OData.Query;
using System.Web.Http.OData.Builder;
﻿using System.Web.Http.Results;
﻿using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace biz.dfch.CS.Entity.LifeCycleManager.Tests.Controller
{
    public class BaseControllerTest<T>
    {
        protected ODataQueryOptions<T> CreateODataQueryOptions(String uri)
        {
            var context = new ODataQueryContext(GetBuilder(typeof(T)).GetEdmModel(), typeof(T));
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            return new ODataQueryOptions<T>(context, request);
        }

        protected ODataConventionModelBuilder GetBuilder(Type type)
        {
            var builder = new ODataConventionModelBuilder();
            MethodInfo method = typeof(ODataConventionModelBuilder).GetMethod("EntitySet");
            MethodInfo genericMethod = method.MakeGenericMethod(type);
            genericMethod.Invoke(builder, new object[]{type.Name + "s"});
            return builder;
        }

        protected void AssertStatusCodeResult(IHttpActionResult actionResult, HttpStatusCode expectedStatusCode)
        {
            Assert.IsTrue(actionResult.GetType() == typeof(StatusCodeResult));
            var response = (StatusCodeResult)actionResult;
            Assert.AreEqual(expectedStatusCode, response.StatusCode);
        }
    }
}
