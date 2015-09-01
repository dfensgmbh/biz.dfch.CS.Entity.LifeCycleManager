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
﻿using System.Web;
﻿using biz.dfch.CS.Entity.LifeCycleManager.Controller;
﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
﻿using Telerik.JustMock;

namespace biz.dfch.CS.Entity.LifeCycleManager.Tests.Controller
{
    [TestClass]
    public class TenantAwareODataControllerTest
    {
        private const String TENANT_ID_HEADER_KEY = "Biz-Dfch-Tenant-Id";
        private const String TENANT_ID = "aa506000-025b-474d-b747-53b67f50d46d";

        [TestInitialize]
        public void TestInitialize()
        {
            Mock.SetupStatic(typeof(HttpContext));
        }

        [TestMethod]
        public void ConstructorSetsTenantIdPropertyAccordingHeaderValue()
        {
            Mock.Arrange(() => HttpContext.Current.Request.Headers.Get(TENANT_ID_HEADER_KEY))
                .Returns(TENANT_ID)
                .OccursOnce();

            var controller = new TenantAwareODataController();
            Assert.AreEqual(TENANT_ID, controller.TenantId);

            Mock.Assert(() => HttpContext.Current.Request.Headers.Get(TENANT_ID_HEADER_KEY));
        }

        [TestMethod]
        public void ConstructorSetsTenantIdPropertyToNullIfNoTenantIdPresentInHeader()
        {
            Mock.Arrange(() => HttpContext.Current.Request.Headers.Get(TENANT_ID_HEADER_KEY))
                .Returns((String)null)
                .OccursOnce();

            var controller = new TenantAwareODataController();
            Assert.AreEqual(null, controller.TenantId);

            Mock.Assert(() => HttpContext.Current.Request.Headers.Get(TENANT_ID_HEADER_KEY));
        }
    }
}
