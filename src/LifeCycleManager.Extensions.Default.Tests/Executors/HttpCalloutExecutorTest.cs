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
using System.Net.Http;
using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Entity;
using LifecycleManager.Extensions.Default.Executors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTestExtensions;
using Telerik.JustMock;

namespace LifeCycleManager.Extensions.Default.Tests.Executors
{
    [TestClass]
    public class HttpCalloutExecutorTest
    {
        private const String SAMPLE_REQUEST_URL = "http://test/api/callout";
        private const String VALID_DEFINITION = "{\"request-url\":\"" + SAMPLE_REQUEST_URL + "\"}";
        private const String INVALID_DEFINITION = "{\"request-url\":\"test/test\"}";
        private const String URI_FIELD = "_requestUrl";

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
        public void ExecuteCalloutDoesPostOnUriWithCalloutDataAsBody()
        {
            var mockedHttpClient = Mock.Create<HttpClient>();
            var mockedResponseMessage = Mock.Create<HttpResponseMessage>();
            Mock.Arrange(() => mockedHttpClient.PostAsync(new Uri(SAMPLE_REQUEST_URL), Arg.IsAny<HttpContent>()).Result)
                .IgnoreInstance()
                .Returns(mockedResponseMessage)
                .MustBeCalled();

            _httpCalloutExecutor.ExecuteCallout(VALID_DEFINITION, new CalloutData());

            Mock.Assert(mockedHttpClient);
            Mock.Assert(mockedResponseMessage);
        }

        [TestMethod]
        public void ExecuteCalloutWithInvalidUrlThrowsArgumentException()
        {
            var mockedHttpClient = Mock.Create<HttpClient>();
            var mockedResponseMessage = Mock.Create<HttpResponseMessage>();
            Mock.Arrange(() => mockedResponseMessage.EnsureSuccessStatusCode()).Throws<HttpRequestException>().MustBeCalled();
            Mock.Arrange(() => mockedHttpClient.PostAsync(new Uri(SAMPLE_REQUEST_URL), Arg.IsAny<HttpContent>()).Result)
                .IgnoreInstance()
                .Returns(mockedResponseMessage)
                .MustBeCalled();

            ThrowsAssert.Throws<ArgumentException>(() => _httpCalloutExecutor.ExecuteCallout(VALID_DEFINITION, new CalloutData()));

            Mock.Assert(mockedHttpClient);
            Mock.Assert(mockedResponseMessage);
        }

        [TestMethod]
        public void ExecuteCalloutWithNullThrowsArgumentException()
        {
            ThrowsAssert.Throws<ArgumentException>(() => _httpCalloutExecutor.ExecuteCallout(VALID_DEFINITION, null), "Callout data should not be null");
        }
    }
}
