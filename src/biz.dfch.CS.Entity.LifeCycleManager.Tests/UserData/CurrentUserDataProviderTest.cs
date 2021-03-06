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
using System.Collections.Specialized;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Web;
using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Entity;
using biz.dfch.CS.Entity.LifeCycleManager.UserData;
using MSTestExtensions;
using Telerik.JustMock;

namespace biz.dfch.CS.Entity.LifeCycleManager.Tests.UserData
{
    [TestClass]
    public class CurrentUserDataProviderTest
    {
        private const String USERNAME = "User";
        private const String TENANT_ID = "aa506000-025b-474d-b747-53b67f50d46d";
        private const String CONNECTION_STRING_NAME = "LcmSecurityData";
        private const String APPLICATION_NAME_KEY = "Application.Name";
        private const String DEFAULT_TENANT_ID_KEY = "TenantId.Default";
        private const String CONNECTION_STRING = "Server=.\\SQLEXPRESS;User ID=sa;Password=password;Database=Test;";
        private const String DEFAULT_TENANT_ID = "f61907fc-75a0-4303-8ad2-b8079fa2bb89";

        private List<String> _permissions = new List<String>{ "XCanRead", "XCanCreate" };
        private List<String> _roles = new List<String>{ "RoleX", "RoleY" };
        
        [TestInitialize]
        public void TestInitialize()
        {
            Mock.SetupStatic(typeof(HttpContext));
            Mock.SetupStatic(typeof(ConfigurationManager));

            var connectionStrings = new ConnectionStringSettingsCollection();
            connectionStrings.Add(new ConnectionStringSettings(CONNECTION_STRING_NAME, CONNECTION_STRING));

            Mock.Arrange(() => ConfigurationManager.ConnectionStrings)
                .Returns(connectionStrings)
                .MustBeCalled();
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
        public void GetIdentityReturnsIdentityOfCurrentUser()
        {
            var sqlConnection = Mock.Create<SqlConnection>();
            var sqlCommand = Mock.Create<SqlCommand>();
            var sqlDataReader = Mock.Create<SqlDataReader>();
            var userId = Guid.NewGuid();

            Mock.Arrange(() => HttpContext.Current.User.Identity.Name)
                .Returns(USERNAME)
                .OccursOnce();

            Mock.Arrange(() => ConfigurationManager.AppSettings)
                .Returns(new NameValueCollection{{ APPLICATION_NAME_KEY, "Test" }})
                .OccursOnce();

            Mock.Arrange(() => new SqlConnection(CONNECTION_STRING))
                .DoNothing()
                .Occurs(3);

            Mock.Arrange(() => sqlConnection.Open())
                .IgnoreInstance()
                .Occurs(3);

            Mock.Arrange(() => sqlCommand.ExecuteReader())
                .IgnoreInstance()
                .Returns(sqlDataReader);

            Mock.Arrange(() => sqlDataReader.GetGuid(0))
                .Returns(userId)
                .InSequence()
                .OccursOnce();

            Mock.Arrange(() => sqlConnection.Close())
                .IgnoreInstance()
                .Occurs(3);

            Mock.Arrange(() => sqlDataReader.Read())
                .Returns(true)
                .InSequence();

            Mock.Arrange(() => sqlDataReader.Read())
                .Returns(true)
                .InSequence();

            Mock.Arrange(() => sqlDataReader.Read())
                .Returns(true)
                .InSequence();

            Mock.Arrange(() => sqlDataReader.Read())
                .Returns(false)
                .InSequence();

            Mock.Arrange(() => sqlDataReader.GetString(0))
                .Returns(_roles[0])
                .InSequence()
                .OccursOnce();

            Mock.Arrange(() => sqlDataReader.GetString(0))
                .Returns(_roles[1])
                .InSequence()
                .OccursOnce();

            Mock.Arrange(() => sqlDataReader.Read())
                .Returns(true)
                .InSequence();

            Mock.Arrange(() => sqlDataReader.Read())
                .Returns(true)
                .InSequence();

            Mock.Arrange(() => sqlDataReader.Read())
                .Returns(false)
                .InSequence();

            Mock.Arrange(() => sqlDataReader.GetString(0))
                .Returns(_permissions[0])
                .InSequence()
                .OccursOnce();

            Mock.Arrange(() => sqlDataReader.GetString(0))
                .Returns(_permissions[1])
                .InSequence()
                .OccursOnce();

            var identity = CurrentUserDataProvider.GetIdentity(TENANT_ID);

            Assert.AreEqual(TENANT_ID, identity.Tid);
            Assert.AreEqual(USERNAME, identity.Username);
            CollectionAssert.AreEqual(_permissions, identity.Permissions.ToList());
            CollectionAssert.AreEqual(_roles, identity.Roles.ToList());

            Mock.Assert(() => HttpContext.Current.User.Identity.Name);
            Mock.Assert(() => ConfigurationManager.AppSettings);
            Mock.Assert(sqlConnection);
            Mock.Assert(sqlCommand);
            Mock.Assert(sqlDataReader);
        }

        [TestMethod]
        public void GetIdentityWithNullTenantIdReturnsIdentityOfCurrentUserWithDefaultTenantId()
        {
            var sqlConnection = Mock.Create<SqlConnection>();
            var sqlCommand = Mock.Create<SqlCommand>();
            var sqlDataReader = Mock.Create<SqlDataReader>();
            var userId = Guid.NewGuid();

            Mock.Arrange(() => HttpContext.Current.User.Identity.Name)
                .Returns(USERNAME)
                .OccursOnce();

            Mock.Arrange(() => ConfigurationManager.AppSettings)
                .Returns(new NameValueCollection { 
                    { APPLICATION_NAME_KEY, "Test" }
                })
                .OccursOnce();

            Mock.Arrange(() => new SqlConnection(CONNECTION_STRING))
                .DoNothing()
                .Occurs(3);

            Mock.Arrange(() => sqlConnection.Open())
                .IgnoreInstance()
                .Occurs(3);

            Mock.Arrange(() => sqlCommand.ExecuteReader())
                .IgnoreInstance()
                .Returns(sqlDataReader);

            Mock.Arrange(() => sqlDataReader.GetGuid(0))
                .Returns(userId)
                .InSequence()
                .OccursOnce();

            Mock.Arrange(() => sqlConnection.Close())
                .IgnoreInstance()
                .Occurs(3);

            Mock.Arrange(() => sqlDataReader.Read())
                .Returns(true)
                .InSequence();

            Mock.Arrange(() => sqlDataReader.Read())
                .Returns(true)
                .InSequence();

            Mock.Arrange(() => sqlDataReader.Read())
                .Returns(true)
                .InSequence();

            Mock.Arrange(() => sqlDataReader.Read())
                .Returns(false)
                .InSequence();

            Mock.Arrange(() => sqlDataReader.GetString(0))
                .Returns(_roles[0])
                .InSequence()
                .OccursOnce();

            Mock.Arrange(() => sqlDataReader.GetString(0))
                .Returns(_roles[1])
                .InSequence()
                .OccursOnce();

            Mock.Arrange(() => sqlDataReader.Read())
                .Returns(true)
                .InSequence();

            Mock.Arrange(() => sqlDataReader.Read())
                .Returns(true)
                .InSequence();

            Mock.Arrange(() => sqlDataReader.Read())
                .Returns(false)
                .InSequence();

            Mock.Arrange(() => sqlDataReader.GetString(0))
                .Returns(_permissions[0])
                .InSequence()
                .OccursOnce();

            Mock.Arrange(() => sqlDataReader.GetString(0))
                .Returns(_permissions[1])
                .InSequence()
                .OccursOnce();

            var identity = CurrentUserDataProvider.GetIdentity(null);

            Assert.AreEqual(DEFAULT_TENANT_ID, identity.Tid);
            Assert.AreEqual(USERNAME, identity.Username);
            CollectionAssert.AreEqual(_permissions, identity.Permissions.ToList());
            CollectionAssert.AreEqual(_roles, identity.Roles.ToList());

            Mock.Assert(() => HttpContext.Current.User.Identity.Name);
            Mock.Assert(() => ConfigurationManager.AppSettings);
            Mock.Assert(sqlConnection);
            Mock.Assert(sqlCommand);
            Mock.Assert(sqlDataReader);
        }

        [TestMethod]
        public void GetIdentityThrowsArgumentNullExceptionIfApplicationNameNotDefined()
        {
            Mock.Arrange(() => HttpContext.Current.User.Identity.Name)
                .Returns(USERNAME)
                .OccursOnce();

            Mock.Arrange(() => ConfigurationManager.AppSettings)
                .Returns(new NameValueCollection())
                .OccursOnce();

            ThrowsAssert.Throws<ArgumentNullException>(() => CurrentUserDataProvider.GetIdentity(TENANT_ID));

            Mock.Assert(() => HttpContext.Current.User.Identity.Name);
            Mock.Assert(() => ConfigurationManager.AppSettings);
        }

        [TestMethod]
        public void GetConnectionStringForExistingConnectionStringReturnsConnectionStringFromConfiguration()
        {           
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
