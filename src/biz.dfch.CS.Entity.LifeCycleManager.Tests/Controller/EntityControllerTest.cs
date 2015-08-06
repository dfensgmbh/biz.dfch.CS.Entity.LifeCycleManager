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
using System.Threading.Tasks;
using biz.dfch.CS.Entity.LifeCycleManager.Controller;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTestExtensions;
using Telerik.JustMock;

namespace biz.dfch.CS.Entity.LifeCycleManager.Tests.Controller
{
    [TestClass]
    public class EntityControllerTest
    {
        private const String HTTP_CLIENT_FIELD = "_httpClient";
        private Uri SAMPLE_ENTITY_URI = new Uri("http://test/api/EntityType(1)");

        [TestMethod]
        [WorkItem(28)]
        public void LoadEntityWithNullEntityUriThrowsArgumentNullException()
        {
            var entityController = new EntityController();
            ThrowsAssert.Throws<ArgumentNullException>(() => entityController.LoadEntity(null));
        }

        [TestMethod]
        [WorkItem(28)]
        public void LoadEntityCallsHttpClientWithEntityUri()
        {
            var mockedHttpClient = Mock.Create<HttpClient>();
            var mockedResponseMessage = Mock.Create<HttpResponseMessage>();
            Mock.Arrange(() => mockedResponseMessage.Content.ReadAsStringAsync().Result).Returns("test").MustBeCalled();
            Mock.Arrange(() => mockedHttpClient.GetAsync(SAMPLE_ENTITY_URI).Result).Returns(mockedResponseMessage).MustBeCalled();
            
            var entityController = new EntityController();
            var entityControllerWithPrivatAccess = new PrivateObject(entityController);
            entityControllerWithPrivatAccess.SetField(HTTP_CLIENT_FIELD, mockedHttpClient);
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
            Mock.Arrange(() => mockedHttpClient.GetAsync(SAMPLE_ENTITY_URI).Result).Returns(mockedResponseMessage).MustBeCalled();

            var entityController = new EntityController();
            var entityControllerWithPrivatAccess = new PrivateObject(entityController);
            entityControllerWithPrivatAccess.SetField(HTTP_CLIENT_FIELD, mockedHttpClient);

            Assert.AreEqual("test", entityController.LoadEntity(SAMPLE_ENTITY_URI));

            Mock.Assert(mockedHttpClient);
            Mock.Assert(mockedResponseMessage);
        }

        [TestMethod]
        [WorkItem(28)]
        public void LoadEntityWithUriPointingToNonExistingEntityThrowsException()
        {

        }
    }
}
