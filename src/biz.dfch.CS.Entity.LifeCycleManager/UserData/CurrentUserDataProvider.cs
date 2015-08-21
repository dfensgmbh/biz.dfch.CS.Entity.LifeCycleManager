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
﻿using System.Data.SqlClient;
﻿using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Entity;

namespace biz.dfch.CS.Entity.LifeCycleManager.UserData
{
    // DFTODO Rename to UserDataProvider
    public static class CurrentUserDataProvider
    {
        public static String GetCurrentUserId()
        {
            return "Administrator";
        }

        public static Boolean HasCurrentUserPermission(String permissionId)
        {
            return true;
        }

        public static Boolean IsUserAuthorized(String currentUserId, String tenantId, BaseEntity entity)
        {
            // DFTODO Check if tenantId matches?
            // DFTODO check ACL table!?
            return true;
        }

        public static Identity GetIdentity(String username)
        {
            var identity = new Identity();
            var connectionString = GetConnectionString();
            String userId = GetUserId(connectionString, username);

            identity.Username = username;
            identity.Roles = GetRoles(connectionString, userId);
            identity.Permissions = GetPermissions(connectionString, identity.Roles);

            return identity;
        }

        private static String GetUserId(String connectionString, String username)
        {
            // DFTODO read applicationName from config
            var applicationName = "Cumulus";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("SELECT Cumulus.dbo.aspnet_Users.UserId FROM Cumulus.dbo.aspnet_Users INNER JOIN Cumulus.dbo.aspnet_Applications ON Cumulus.dbo.aspnet_Applications.ApplicationId = Cumulus.dbo.aspnet_Users.ApplicationId WHERE Cumulus.dbo.aspnet_Users.UserName = @username AND Cumulus.dbo.aspnet_Applications.ApplicationName = @applicationName", connection);
                command.Parameters.Add(new SqlParameter("username", username));
                command.Parameters.Add(new SqlParameter("applicationName", applicationName));

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    return reader.GetString(0);
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
                if (connectionString.Name.Equals("MySqlConnection"))
                {
                    return connectionString.ConnectionString;
                }
            }
            return null;
        }
    }
}
