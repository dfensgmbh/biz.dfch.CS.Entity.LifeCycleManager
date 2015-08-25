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
using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Entity;
using biz.dfch.CS.Utilities.Rest;
using LifecycleManager.Extensions.Default.Executors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTestExtensions;
using Telerik.JustMock;
using HttpMethod = biz.dfch.CS.Utilities.Rest.HttpMethod;

namespace LifeCycleManager.Extensions.Default.Tests.Executors
{
    [TestClass]
    public class HttpCalloutExecutorTest
    {
        private const String SAMPLE_REQUEST_URL = "http://test/api/callout";
        private const String VALID_DEFINITION = "{\"callout-url\":\"" + SAMPLE_REQUEST_URL + "\"}";
        private const String INVALID_DEFINITION = "{\"callout-url\":\"test/test\"}";

        private HttpCalloutExecutor _httpCalloutExecutor = new HttpCalloutExecutor();

        [TestInitialize]
        public void TestInitialize()
        {
            _httpCalloutExecutor = new HttpCalloutExecutor();
        }

        [TestMethod]
        public void ExecuteCalloutWithInvalidUrlInDefinitionThrowsUriFormatException()
        {
            ThrowsAssert.Throws<UriFormatException>(() => _httpCalloutExecutor.ExecuteCallout(INVALID_DEFINITION, new CalloutData()));
        }

        [TestMethod]
        public void ExecuteCalloutCallsRestCallExecutorsInvokeWithMethodPostUriAndCalloutDataAsBody()
        {
            var mockedRestCallExecutor = Mock.Create<RestCallExecutor>();
            Mock.Arrange(() => mockedRestCallExecutor.Invoke(HttpMethod.Post, SAMPLE_REQUEST_URL, null, Arg.AnyString))
                .IgnoreInstance()
                .OccursOnce();

            _httpCalloutExecutor.ExecuteCallout(VALID_DEFINITION, new CalloutData());

            Mock.Assert(mockedRestCallExecutor);
        }

        [TestMethod]
        public void ExecuteCalloutWithInvalidUrlThrowsUriFormatException()
        {
            ThrowsAssert.Throws<UriFormatException>(() => _httpCalloutExecutor.ExecuteCallout(INVALID_DEFINITION, new CalloutData()));
        }

        [TestMethod]
        public void ExecuteCalloutWithNullThrowsArgumentNullException()
        {
            ThrowsAssert.Throws<ArgumentNullException>(() => _httpCalloutExecutor.ExecuteCallout(VALID_DEFINITION, null));
        }
    }
}
