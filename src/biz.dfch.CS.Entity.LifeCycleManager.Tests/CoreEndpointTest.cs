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
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace biz.dfch.CS.Entity.LifeCycleManager.Tests
{
    [TestClass]
    public class CoreEndpointTest
    {
        private const String CONTAINER_NAME = "Core";

        [TestMethod]
        public void GetContainerNameReturnsNameOfTheContainer()
        {
            var utilitiesEndpoint = new CoreEndpoint();
            Assert.AreEqual(CONTAINER_NAME, utilitiesEndpoint.GetContainerName());
        }

        [TestMethod]
        public void GetModelReturnsModelContainingEdmEntityTypeForJobs()
        {
            var utilitiesEndpoint = new CoreEndpoint();
            var model = utilitiesEndpoint.GetModel();

            Assert.AreEqual(1, model.SchemaElements.Where(v => v.Name.Equals("Job")).Count());
        }

        [TestMethod]
        public void GetModelReturnsModelContainingEdmEntityTypeForCallbacks()
        {
            var utilitiesEndpoint = new CoreEndpoint();
            var model = utilitiesEndpoint.GetModel();

            Assert.AreEqual(1, model.SchemaElements.Where(v => v.Name.Equals("LifeCycle")).Count());
        }

        [TestMethod]
        public void GetModelReturnsModelContainingEdmEntityTypeForStateChangeLocks()
        {
            var utilitiesEndpoint = new CoreEndpoint();
            var model = utilitiesEndpoint.GetModel();

            Assert.AreEqual(1, model.SchemaElements.Where(v => v.Name.Equals("StateChangeLock")).Count());
        }

        [TestMethod]
        public void GetModelReturnsModelContainingEdmEntityTypeForCalloutDefinitions()
        {
            var utilitiesEndpoint = new CoreEndpoint();
            var model = utilitiesEndpoint.GetModel();

            Assert.AreEqual(1, model.SchemaElements.Where(v => v.Name.Equals("CalloutDefinition")).Count());
        }
    }
}
