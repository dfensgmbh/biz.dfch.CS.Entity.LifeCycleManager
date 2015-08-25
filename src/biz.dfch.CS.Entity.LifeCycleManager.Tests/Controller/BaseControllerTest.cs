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
using System.Web.Http.OData;
using System.Web.Http.OData.Query;
using System.Web.Http.OData.Builder;
﻿using System.Web.Http.Results;
﻿using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace biz.dfch.CS.Entity.LifeCycleManager.Tests.Controller
{
    public class BaseControllerTest<T>
    {

        protected const String CURRENT_USER_ID = "currentUser";
        protected const String ANOTHER_USER_ID = "anotherUser";
        protected const String TENANT_ID = "aa506000-025b-474d-b747-53b67f50d46d";
        protected const String EMPTY_TENANT_ID = "";
        protected const String ANOTHER_TENANT_ID = "2e44d024-21c9-46ba-aceb-953a15f9f70a";
        protected const String SAMPLE_TOKEN = "5H7l7uZ61JTRS716D498WZ6RYa53p9QA";

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
