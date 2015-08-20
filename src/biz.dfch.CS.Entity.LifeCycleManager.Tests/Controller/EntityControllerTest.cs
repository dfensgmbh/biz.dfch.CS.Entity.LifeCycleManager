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
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using biz.dfch.CS.Entity.LifeCycleManager.Controller;
using biz.dfch.CS.Entity.LifeCycleManager.UserData;
using biz.dfch.CS.Utilities.Rest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTestExtensions;
using Telerik.JustMock;
using HttpMethod = biz.dfch.CS.Utilities.Rest.HttpMethod;

namespace biz.dfch.CS.Entity.LifeCycleManager.Tests.Controller
{
    [TestClass]
    public class EntityControllerTest
    {
        private const String BEARER_AUTH_TYPE = "Bearer";
        private const String SAMPLE_BEARER_TOKEN = "AbCdEf123456";
        private const String HEADERS_FIELD = "_headers";
        private const String AUTH_TYPE_FIELD = "_authType";
        private const String SAMPLE_ENTITY = "{}";
        private Uri SAMPLE_ENTITY_URI = new Uri("http://test/api/EntityType(1)");
        
        private IAuthenticationProvider _authenticationProvider;

        [TestInitialize]
        public void TestInitialize()
        {
            _authenticationProvider = Mock.Create<IAuthenticationProvider>();
        }

        [TestMethod]
        public void EntityControllerConstructorReadsOutAuthenticationInformationFromAuthenticationProviderIfNotNull()
        {
            Mock.Arrange(() => _authenticationProvider.GetAuthHeaderValue())
                .Returns(SAMPLE_BEARER_TOKEN)
                .OccursOnce();
            
            Mock.Arrange(() => _authenticationProvider.GetAuthTypeValue())
                .Returns(BEARER_AUTH_TYPE)
                .OccursOnce();
            
            var entityController = new EntityController(_authenticationProvider);
            var entityControllerWithPrivateAccess = new PrivateObject(entityController);
            var headersField = (IDictionary<String, String>)entityControllerWithPrivateAccess.GetField(HEADERS_FIELD);
            
            Assert.AreEqual(SAMPLE_BEARER_TOKEN, headersField["Authorization"]);
            Assert.AreEqual(BEARER_AUTH_TYPE, (String)entityControllerWithPrivateAccess.GetField(AUTH_TYPE_FIELD));
            
            Mock.Assert(_authenticationProvider);
        }

        [TestMethod]
        [WorkItem(28)]
        public void LoadEntityWithNullEntityUriThrowsArgumentNullException()
        {
            var entityController = new EntityController(_authenticationProvider);
            ThrowsAssert.Throws<ArgumentNullException>(() => entityController.LoadEntity(null));
        }

        [TestMethod]
        [WorkItem(28)]
        public void LoadEntityCallsRestCallExecutorsInvokeMethodWithEntityUriAndAuthHeaders()
        {
            Mock.Arrange(() => _authenticationProvider.GetAuthHeaderValue())
                .Returns(SAMPLE_BEARER_TOKEN)
                .OccursOnce();

            Mock.Arrange(() => _authenticationProvider.GetAuthTypeValue())
                .Returns(BEARER_AUTH_TYPE)
                .OccursOnce();

            var headers = new Dictionary<String, String>();
            headers.Add("Authorization", SAMPLE_BEARER_TOKEN);
            var mockedRestCallExecutor = Mock.Create<RestCallExecutor>();
            Mock.Arrange(() => mockedRestCallExecutor.Invoke(SAMPLE_ENTITY_URI.ToString(), Arg.Is(headers)))
                .IgnoreInstance()
                .OccursOnce();

            var entityController = new EntityController(_authenticationProvider);
            entityController.LoadEntity(SAMPLE_ENTITY_URI);

            Mock.Assert(_authenticationProvider);
            Mock.Assert(mockedRestCallExecutor);
        }

        [TestMethod]
        [WorkItem(28)]
        public void LoadEntityReturnsEntityAsJsonIfFound()
        {
            Mock.Arrange(() => _authenticationProvider.GetAuthHeaderValue())
                .Returns(SAMPLE_BEARER_TOKEN)
                .OccursOnce();

            Mock.Arrange(() => _authenticationProvider.GetAuthTypeValue())
                .Returns(BEARER_AUTH_TYPE)
                .OccursOnce();

            var headers = new Dictionary<String, String>();
            headers.Add("Authorization", SAMPLE_BEARER_TOKEN);
            var mockedRestCallExecutor = Mock.Create<RestCallExecutor>();
            Mock.Arrange(() => mockedRestCallExecutor.Invoke(SAMPLE_ENTITY_URI.ToString(), Arg.Is(headers)))
                .IgnoreInstance()
                .Returns("test")
                .OccursOnce();

            var entityController = new EntityController(_authenticationProvider);
            Assert.AreEqual("test", entityController.LoadEntity(SAMPLE_ENTITY_URI));

            Mock.Assert(_authenticationProvider);
            Mock.Assert(mockedRestCallExecutor);
        }

        [TestMethod]
        [WorkItem(28)]
        public void LoadEntityWithUriPointingToNonExistingEntityThrowsHttpRequestException()
        {
            Mock.Arrange(() => _authenticationProvider.GetAuthHeaderValue())
                .Returns(SAMPLE_BEARER_TOKEN)
                .OccursOnce();

            Mock.Arrange(() => _authenticationProvider.GetAuthTypeValue())
                .Returns(BEARER_AUTH_TYPE)
                .OccursOnce();

            var headers = new Dictionary<String, String>();
            headers.Add("Authorization", SAMPLE_BEARER_TOKEN);
            var mockedRestCallExecutor = Mock.Create<RestCallExecutor>();
            Mock.Arrange(() => mockedRestCallExecutor.Invoke(SAMPLE_ENTITY_URI.ToString(), Arg.Is(headers)))
                .IgnoreInstance()
                .Throws<HttpRequestException>()
                .OccursOnce();

            var entityController = new EntityController(_authenticationProvider);
            ThrowsAssert.Throws<HttpRequestException>(() => entityController.LoadEntity(SAMPLE_ENTITY_URI));

            Mock.Assert(_authenticationProvider);
            Mock.Assert(mockedRestCallExecutor);
        }

        [TestMethod]
        public void UpdateEntityWithNullEntityUriThrowsArgumentNullException()
        {
            var entityController = new EntityController(_authenticationProvider);
            ThrowsAssert.Throws<ArgumentNullException>(() => entityController.UpdateEntity(null, ""));
        }

        [TestMethod]
        public void UpdateEntityCallsRestCallExecutorsInvokeMethodOnEntityUriWithEntityInBody()
        {
            Mock.Arrange(() => _authenticationProvider.GetAuthHeaderValue())
                .Returns(SAMPLE_BEARER_TOKEN)
                .OccursOnce();

            Mock.Arrange(() => _authenticationProvider.GetAuthTypeValue())
                .Returns(BEARER_AUTH_TYPE)
                .OccursOnce();

            var headers = new Dictionary<String, String>();
            headers.Add("Authorization", SAMPLE_BEARER_TOKEN);
            var mockedRestCallExecutor = Mock.Create<RestCallExecutor>();

            Mock.Arrange(() => mockedRestCallExecutor.Invoke(HttpMethod.Put, SAMPLE_ENTITY_URI.ToString(), Arg.Is(headers), SAMPLE_ENTITY))
                .IgnoreInstance()
                .OccursOnce();

            var entityController = new EntityController(_authenticationProvider);
            entityController.UpdateEntity(SAMPLE_ENTITY_URI, SAMPLE_ENTITY);

            Mock.Assert(_authenticationProvider);
            Mock.Assert(mockedRestCallExecutor);
        }

        [TestMethod]
        public void UpdateEntityWithUriPointingToNonExistingEntityThrowsHttpRequestException() 
        {
            Mock.Arrange(() => _authenticationProvider.GetAuthHeaderValue())
                .Returns(SAMPLE_BEARER_TOKEN)
                .OccursOnce();

            Mock.Arrange(() => _authenticationProvider.GetAuthTypeValue())
                .Returns(BEARER_AUTH_TYPE)
                .OccursOnce();

            var headers = new Dictionary<String, String>();
            headers.Add("Authorization", SAMPLE_BEARER_TOKEN);
            var mockedRestCallExecutor = Mock.Create<RestCallExecutor>();
            Mock.Arrange(() => mockedRestCallExecutor.Invoke(HttpMethod.Put, SAMPLE_ENTITY_URI.ToString(), Arg.Is(headers), SAMPLE_ENTITY))
                .IgnoreInstance()
                .Throws<HttpRequestException>()
                .OccursOnce();

            var entityController = new EntityController(_authenticationProvider);
            ThrowsAssert.Throws<HttpRequestException>(() => entityController.UpdateEntity(SAMPLE_ENTITY_URI, SAMPLE_ENTITY));

            Mock.Assert(_authenticationProvider);
            Mock.Assert(mockedRestCallExecutor);
        }
    }
}
