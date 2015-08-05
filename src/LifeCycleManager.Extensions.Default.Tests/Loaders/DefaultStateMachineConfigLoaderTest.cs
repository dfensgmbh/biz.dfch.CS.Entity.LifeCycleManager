﻿/**
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

using LifeCycleManager.Extensions.Default.Loaders;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LifeCycleManager.Extensions.Default.Tests.Loaders
{
    [TestClass]
    public class DefaultStateMachineConfigLoaderTest
    {
        [TestMethod]
        public void LoadConfigurationForPredefinedConfigReturnsConfigurationLoadedConfigurationFromAppConfig()
        {
            var stateMachineConfigLoader = new DefaultStateMachineConfigLoader();
            Assert.AreEqual("{\"Created-Continue\":\"Running\",\"Created-Cancel\":\"InternalErrorState\",\"Running-Continue\":\"Completed\",\"Running-Cancel\":\"Cancelled\",\"Completed-Continue\":\"Disposed\",\"Completed-Cancel\":\"InternalErrorState\",\"Cancelled-Continue\":\"Disposed\",\"Cancelled-Cancel\":\"InternalErrorState\",\"InternalErrorState-Continue\":\"Disposed\"}",
                stateMachineConfigLoader.LoadConfiguration("EntityType"));
        }

        [TestMethod]
        public void LoadConfigurationForNotDefinedConfigReturnsNull()
        {
            var stateMachineConfigLoader = new DefaultStateMachineConfigLoader();
            Assert.AreEqual(null,
                stateMachineConfigLoader.LoadConfiguration("AnotherEntity"));
        }
    }
}
