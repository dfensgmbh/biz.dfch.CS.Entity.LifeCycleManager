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

﻿using System;
﻿using System.Collections.Generic;
﻿using System.Configuration;
﻿using System.Data.Entity;
﻿using System.Data.Entity.Core;
﻿using System.Data.SqlClient;
﻿using System.Diagnostics;
﻿using System.Linq;
﻿using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Entity;
using System.Web;

namespace biz.dfch.CS.Entity.LifeCycleManager.UserData
{
    public static class CurrentUserDataProvider
    {
        private const String APPLICATION_NAME_KEY = "Application.Name";
        private const String CONNECTION_STRING_NAME = "LcmSecurityData";

        private static String _connectionString = GetConnectionString();

        public static String GetCurrentUsername()
        {
            return HttpContext.Current.User.Identity.Name;
        }

        public static Boolean IsEntityOfUser(String currentUsername, String tenantId, BaseEntity entity)
        {
            // DFTODO Check ACLs here
            return entity.Tid == tenantId && entity.CreatedBy == currentUsername;
        }

        public static IList<T> GetEntitiesForUser<T>(DbSet<T> dbSet, String currentUsername, String tenantId)
            where T : BaseEntity
        {
            // DFTODO Check ACLs here
            return dbSet.Where(x => x.Tid == tenantId && x.CreatedBy == currentUsername).ToList();
        }

        public static Identity GetIdentity(String tenantId)
        {
            // DFTODO Check, if user really belongs to tenant with id tenantId
            // DFTODO Check, if tenantId is null query home/primary tenant
            var username = GetCurrentUsername();
            var identity = new Identity();
            String userId = GetUserId(_connectionString, username);

            identity.Username = username;
            identity.Tid = tenantId;
            identity.Roles = GetRoles(_connectionString, userId);
            identity.Permissions = GetPermissions(_connectionString, identity.Roles);

            return identity;
        }

        private static String GetUserId(String connectionString, String username)
        {
            var applicationName = ConfigurationManager.AppSettings[APPLICATION_NAME_KEY];
            if (null == applicationName)
            {
                throw new ArgumentNullException(String.Format("'{0}' not defined in configuration file", APPLICATION_NAME_KEY));
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("SELECT Cumulus.dbo.aspnet_Users.UserId FROM Cumulus.dbo.aspnet_Users INNER JOIN Cumulus.dbo.aspnet_Applications ON Cumulus.dbo.aspnet_Applications.ApplicationId = Cumulus.dbo.aspnet_Users.ApplicationId WHERE Cumulus.dbo.aspnet_Users.UserName = @username AND Cumulus.dbo.aspnet_Applications.ApplicationName = @applicationName", connection);
                command.Parameters.Add(new SqlParameter("username", username));
                command.Parameters.Add(new SqlParameter("applicationName", applicationName));

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader.GetGuid(0).ToString();
                    }
                    Debug.WriteLine("UserId for user with username '{0}' not available in database", username);
                    throw new ObjectNotFoundException();
                }
            }
        }

        private static IEnumerable<String> GetRoles(String connectionString, String userId)
        {
            var roles = new List<String>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command =
                    new SqlCommand("SELECT Cumulus.dbo.aspnet_Roles.RoleName FROM Cumulus.dbo.aspnet_UsersInRoles INNER JOIN Cumulus.dbo.aspnet_Roles ON Cumulus.dbo.aspnet_Roles.RoleId = Cumulus.dbo.aspnet_UsersInRoles.RoleId WHERE Cumulus.dbo.aspnet_UsersInRoles.UserId = @userId", connection);
                command.Parameters.Add(new SqlParameter("userId", userId));

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        roles.Add(reader.GetString(0));
                    }
                    return roles;
                }
            }
        }

        private static IEnumerable<String> GetPermissions(String connectionString, IEnumerable<String> roles)
        {
            var permissions = new List<String>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                foreach (var role in roles)
                {
                    SqlCommand command = new SqlCommand("SELECT PermissionId FROM Cumulus.dbo.RolePermissions WHERE RoleName = @roleName", connection);
                    command.Parameters.Add(new SqlParameter("roleName", role));

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            permissions.Add(reader.GetString(0));
                        }
                    }
                }
                return permissions;
            }
        }

        private static String GetConnectionString()
        {
            ConnectionStringSettingsCollection connectionStrings = ConfigurationManager.ConnectionStrings;

            foreach (ConnectionStringSettings connectionString in connectionStrings)
            {
                if (connectionString.Name == CONNECTION_STRING_NAME)
                {
                    return connectionString.ConnectionString;
                }
            }
            throw new ArgumentException(String.Format("No connection string with name '{0}' found", CONNECTION_STRING_NAME));
        }
    }
}
