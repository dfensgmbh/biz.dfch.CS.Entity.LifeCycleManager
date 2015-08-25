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
using System.Configuration;
using System.Data.Entity;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Web;
using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Entity;
using biz.dfch.CS.Entity.LifeCycleManager.UserData;
using Telerik.JustMock;
using System.Collections.Generic;
using System.Linq;

namespace biz.dfch.CS.Entity.LifeCycleManager.Tests.UserData
{
    [TestClass]
    public class CurrentUserDataProviderTest
    {
        private const String USERNAME = "User";
        private const String TENANT_ID = "aa506000-025b-474d-b747-53b67f50d46d";
        private const String CONNECTION_STRING_NAME = "LcmSecurityData";
        private const String CONNECTION_STRING = "Server=.\\SQLEXPRESS;User ID=sa;Password=password;Database=Test;";

        [TestInitialize]
        public void TestInitialize()
        {
            Mock.SetupStatic(typeof(HttpContext));
            Mock.SetupStatic(typeof(ConfigurationManager));
        }

        [TestMethod]
        public void GetCurrentUsernameGetsUsernameFromHttpContext()
        {
            Mock.Arrange(() => HttpContext.Current.User.Identity.Name)
                .Returns(USERNAME)
                .OccursOnce();

            CurrentUserDataProvider.GetCurrentUsername();

            Mock.Assert(() => HttpContext.Current.User.Identity.Name);
        }

        [TestMethod]
        public void IsEntityOfUserForMatchingTidAndCreatedByReturnsTrue()
        {
            Assert.IsTrue(CurrentUserDataProvider.IsEntityOfUser(USERNAME, TENANT_ID, new BaseEntity
            {
                CreatedBy = USERNAME,
                Tid = TENANT_ID
            }));
        }

        [TestMethod]
        public void IsEntityOfUserForNonMatchingTidAndCreatedByReturnsFalse()
        {
            Assert.IsFalse(CurrentUserDataProvider.IsEntityOfUser(USERNAME, TENANT_ID, 
                new BaseEntity
                {
                    CreatedBy = "Another",
                    Tid = TENANT_ID
                }));

            Assert.IsFalse(CurrentUserDataProvider.IsEntityOfUser(USERNAME, TENANT_ID,
                new BaseEntity
                {
                    CreatedBy = USERNAME,
                    Tid = Guid.NewGuid().ToString()
                }));
        }

        [TestMethod]
        public void GetEntitiesForUserReturnsEntitiesMatchingTidAndCreatedBy()
        {
            var dbSet = Mock.Create<DbSet<BaseEntity>>();

            IList<BaseEntity> result = CurrentUserDataProvider.GetEntitiesForUser(dbSet, USERNAME, TENANT_ID);

            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void GetEntitiesForUserReturnsEmptyList()
        {
            //IList<BaseEntity> dbSet = new List<BaseEntity> { new BaseEntity {Tid = TENANT_ID, CreatedBy = "Another" }};

            //IList<BaseEntity> result = CurrentUserDataProvider.GetEntitiesForUser(dbSet, USERNAME, TENANT_ID);

            Assert.AreEqual(0, result.Count);
        }

        // DFTODO test identity fetching

        [TestMethod]
        public void GetConnectionStringForExistingConnectionStringReturnsConnectionStringFromConfiguration()
        {
            var connectionStrings = new ConnectionStringSettingsCollection();
            connectionStrings.Add(new ConnectionStringSettings(CONNECTION_STRING_NAME, CONNECTION_STRING));

            Mock.Arrange(() => ConfigurationManager.ConnectionStrings)
                .Returns(connectionStrings)
                .MustBeCalled();
            
            MethodInfo method = typeof(CurrentUserDataProvider)
                .GetMethod("GetConnectionString", BindingFlags.Static | BindingFlags.NonPublic);
            String connectionString = (String)method.Invoke(null, new object[] { });

            Assert.AreEqual(CONNECTION_STRING, connectionString);

            Mock.Assert(() => ConfigurationManager.ConnectionStrings);
        }

        [TestMethod]
        public void GetConnectionStringForNonExistingConnectionStringThrowsArgumentException()
        {
            var connectionStrings = new ConnectionStringSettingsCollection();
            connectionStrings.Add(new ConnectionStringSettings("AnotherConnectionString", CONNECTION_STRING));

            Mock.Arrange(() => ConfigurationManager.ConnectionStrings)
                .Returns(connectionStrings)
                .MustBeCalled();

            MethodInfo method = typeof(CurrentUserDataProvider)
                .GetMethod("GetConnectionString", BindingFlags.Static | BindingFlags.NonPublic);

            try
            {
                method.Invoke(null, new object[] {});
            }
            catch (TargetInvocationException ex)
            {
                Assert.IsTrue(ex.GetBaseException().GetType() == typeof(ArgumentException));
            }

            Mock.Assert(() => ConfigurationManager.ConnectionStrings);
        }
    }
}
