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
using System.Net;
using System.Net.Http;
using biz.dfch.CS.Entity.LifeCycleManager.Controller;
using biz.dfch.CS.Entity.LifeCycleManager.Credentials;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTestExtensions;
using Telerik.JustMock;

namespace biz.dfch.CS.Entity.LifeCycleManager.Tests.Controller
{
    [TestClass]
    public class EntityControllerTest
    {
        private Uri SAMPLE_ENTITY_URI = new Uri("http://test/api/EntityType(1)");
        
        private ICredentialProvider _credentialProvider;

        [TestInitialize]
        public void TestInitialize()
        {
            _credentialProvider = Mock.Create<ICredentialProvider>();
        }

        [TestMethod]
        public void EntityControllerConstructorReadsCredentialsFromCredentialsProvider()
        {
            Mock.Arrange(() => _credentialProvider.GetCredentials())
                .Returns(CredentialCache.DefaultNetworkCredentials)
                .MustBeCalled();
            new EntityController(_credentialProvider);
            Mock.Assert(_credentialProvider);
        }

        [TestMethod]
        [WorkItem(28)]
        public void LoadEntityWithNullEntityUriThrowsArgumentNullException()
        {
            var entityController = new EntityController(_credentialProvider);
            ThrowsAssert.Throws<ArgumentNullException>(() => entityController.LoadEntity(null));
        }

        [TestMethod]
        [WorkItem(28)]
        public void LoadEntityExecutesGetWithHttpClientOnEntityUri()
        {
            var mockedHttpClient = Mock.Create<HttpClient>();
            var mockedResponseMessage = Mock.Create<HttpResponseMessage>();
            Mock.Arrange(() => mockedResponseMessage.Content.ReadAsStringAsync().Result).Returns("test").MustBeCalled();
            Mock.Arrange(() => mockedHttpClient.GetAsync(SAMPLE_ENTITY_URI).Result)
                .IgnoreInstance()
                .Returns(mockedResponseMessage)
                .MustBeCalled();
            
            var entityController = new EntityController(_credentialProvider);
            entityController.LoadEntity(SAMPLE_ENTITY_URI);

            Mock.Assert(mockedHttpClient);
            Mock.Assert(mockedResponseMessage);
        }

        [TestMethod]
        [WorkItem(28)]
        public void LoadEntityReturnsEntityAsJsonIfFound()
        {
            var mockedHttpClient = Mock.Create<HttpClient>();
            var mockedResponseMessage = Mock.Create<HttpResponseMessage>();
            Mock.Arrange(() => mockedResponseMessage.Content.ReadAsStringAsync().Result).Returns("test").MustBeCalled();
            Mock.Arrange(() => mockedResponseMessage.EnsureSuccessStatusCode()).MustBeCalled();
            Mock.Arrange(() => mockedHttpClient.GetAsync(SAMPLE_ENTITY_URI).Result)
                .IgnoreInstance()
                .Returns(mockedResponseMessage)
                .MustBeCalled();

            var entityController = new EntityController(_credentialProvider);

            Assert.AreEqual("test", entityController.LoadEntity(SAMPLE_ENTITY_URI));

            Mock.Assert(mockedHttpClient);
            Mock.Assert(mockedResponseMessage);
        }

        [TestMethod]
        [WorkItem(28)]
        public void LoadEntityWithUriPointingToNonExistingEntityThrowsArgumentException()
        {
            var mockedHttpClient = Mock.Create<HttpClient>();
            var mockedResponseMessage = Mock.Create<HttpResponseMessage>();
            Mock.Arrange(() => mockedResponseMessage.EnsureSuccessStatusCode()).Throws<HttpRequestException>().MustBeCalled();
            Mock.Arrange(() => mockedHttpClient.GetAsync(SAMPLE_ENTITY_URI).Result)
                .IgnoreInstance()
                .Returns(mockedResponseMessage)
                .MustBeCalled();

            var entityController = new EntityController(_credentialProvider);

            ThrowsAssert.Throws<ArgumentException>(() => entityController.LoadEntity(SAMPLE_ENTITY_URI));

            Mock.Assert(mockedHttpClient);
            Mock.Assert(mockedResponseMessage);
        }

        [TestMethod]
        public void UpdateEntityWithNullEntityUriThrowsArgumentNullException()
        {
            var entityController = new EntityController(_credentialProvider);
            ThrowsAssert.Throws<ArgumentNullException>(() => entityController.UpdateEntity(null, ""));
        }

        [TestMethod]
        public void UpdateEntityExecutesPuthWithHttpClientOnEntityUri()
        {
            var mockedHttpClient = Mock.Create<HttpClient>();
            var mockedResponseMessage = Mock.Create<HttpResponseMessage>();
            Mock.Arrange(() => mockedHttpClient.PutAsync(SAMPLE_ENTITY_URI, Arg.IsAny<HttpContent>()).Result)
                .IgnoreInstance()
                .Returns(mockedResponseMessage)
                .MustBeCalled();

            var entityController = new EntityController(_credentialProvider);
            entityController.UpdateEntity(SAMPLE_ENTITY_URI, "");

            Mock.Assert(mockedHttpClient);
            Mock.Assert(mockedResponseMessage);
        }

        [TestMethod]
        public void UpdateEntityWithUriPointingToNonExistingEntityThrowsArgumentException() 
        {
            var mockedHttpClient = Mock.Create<HttpClient>();
            var mockedResponseMessage = Mock.Create<HttpResponseMessage>();
            Mock.Arrange(() => mockedResponseMessage.EnsureSuccessStatusCode()).Throws<HttpRequestException>().MustBeCalled();
            Mock.Arrange(() => mockedHttpClient.PutAsync(SAMPLE_ENTITY_URI, Arg.IsAny<HttpContent>()).Result)
                .IgnoreInstance()
                .Returns(mockedResponseMessage)
                .MustBeCalled();

            var entityController = new EntityController(_credentialProvider);

            ThrowsAssert.Throws<ArgumentException>(() => entityController.UpdateEntity(SAMPLE_ENTITY_URI, ""));

            Mock.Assert(mockedHttpClient);
            Mock.Assert(mockedResponseMessage);
        }
    }
}
