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
using Telerik.JustMock.Helpers;

namespace LifeCycleManager.Extensions.Default.Tests.Executors
{
    [TestClass]
    public class HttpCalloutExecutorTest
    {
        private const String SAMPLE_REQUEST_URL = "http://test/api/callout";
        private const String VALID_DEFINITION = "{\"request-url\":\"" + SAMPLE_REQUEST_URL + "\"}";
        private const String INVALID_DEFINITION = "{\"request-url\":\"test/test\"}";
        private const String URI_FIELD = "_url";

        [TestMethod]
        public void HttpCalloutExecutorConstructorExtractsRequestUrlFromDefinition()
        {
            var calloutExecutor = new HttpCalloutExecutor(VALID_DEFINITION);
            var calloutExecutorWithPrivateAccess = new PrivateObject(calloutExecutor);
            Assert.AreEqual(new Uri(SAMPLE_REQUEST_URL), calloutExecutorWithPrivateAccess.GetField(URI_FIELD));
        }

        [TestMethod]
        public void HttpCalloutExecutorConstructorWithInvalidUrlInDefinitionThrowsException()
        {
            ThrowsAssert.Throws<UriFormatException>(() => new HttpCalloutExecutor(INVALID_DEFINITION));
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

            var calloutExecutor = new HttpCalloutExecutor(VALID_DEFINITION);
            calloutExecutor.ExecuteCallout(new CalloutData());

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

            var calloutExecutor = new HttpCalloutExecutor(VALID_DEFINITION);
            
            ThrowsAssert.Throws<ArgumentException>(() => calloutExecutor.ExecuteCallout(new CalloutData()));

            Mock.Assert(mockedHttpClient);
            Mock.Assert(mockedResponseMessage);
        }

        [TestMethod]
        public void ExecuteCalloutWithNullThrowsArgumentException()
        {
            var calloutExecutor = new HttpCalloutExecutor(VALID_DEFINITION);
            ThrowsAssert.Throws<ArgumentException>(() => calloutExecutor.ExecuteCallout(null), "Callout data should not be null");
        }
    }
}
