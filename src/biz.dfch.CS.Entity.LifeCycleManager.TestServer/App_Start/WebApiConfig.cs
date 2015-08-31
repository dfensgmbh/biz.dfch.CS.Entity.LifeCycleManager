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
using System.Configuration;
using System.Linq;
using System.Web.Http;
using System.Web.Http.OData.Batch;
using System.Web.Http.OData.Extensions;
using biz.dfch.CS.Entity.LifeCycleManager.TestServer.Logging;
using biz.dfch.CS.Utilities.Contracts.Endpoint;

namespace biz.dfch.CS.Entity.LifeCycleManager.TestServer
{
    public static class WebApiConfig
    {
        private const String SERVER_ROLES_KEY = "LifeCycleManager.Server.ServerRoles";
        private static String _apiBase = "api";
        private static List<String> _serverRoles;

        private static IEnumerable<Lazy<IODataEndpoint, IODataEndpointData>> endpoints;

        public static void InitEndpoints(IEnumerable<Lazy<IODataEndpoint, IODataEndpointData>> endpoints)
        {
            WebApiConfig.endpoints = (null == endpoints) ? new List<Lazy<IODataEndpoint, IODataEndpointData>>() : endpoints;
        }

        public static void Register(HttpConfiguration config)
        {
            Debug.Write("WebApiConfig::Register() START");

            ResolveServerRoles();

            if (null != endpoints)
            {
                foreach (var endpoint in endpoints)
                {

                    if (NotMatchingRoleOfServer(endpoint))
                    {
                        continue;
                    }

                    if (IsContainerNameNotUnique(endpoint))
                    {
                        Debug.WriteLine(String.Format("ContainerName '{0}' not unique",
                            endpoint.Value.GetContainerName()));
                        continue;
                    }

                    RegisterMefEndpoint(config, endpoint);
                }
            }
            config.MapHttpAttributeRoutes();
            Debug.Write("WebApiConfig::Register() END");
        }

        private static void ResolveServerRoles()
        {
            var serverRoles = ConfigurationManager.AppSettings[SERVER_ROLES_KEY];
            if (null == serverRoles)
            {
                Debug.WriteLine("WARNING: '{0}' not set in .config file", SERVER_ROLES_KEY);
                _serverRoles = new List<String>();
            }
            else
            {
                _serverRoles = serverRoles.Split(',').ToList<String>();
            }
        }

        private static Boolean NotMatchingRoleOfServer(Lazy<IODataEndpoint, IODataEndpointData> endpoint)
        {
            return !_serverRoles.Contains(endpoint.Metadata.ServerRole.ToString());
        }

        private static Boolean IsContainerNameNotUnique(Lazy<IODataEndpoint, IODataEndpointData> endpoint)
        {
            return "Administration" == endpoint.Value.GetContainerName() || 1 < endpoints
                .Where(v => v.Value.GetContainerName().Equals(endpoint.Value.GetContainerName(), StringComparison.InvariantCultureIgnoreCase))
                .Count();
        }

        private static void RegisterMefEndpoint(HttpConfiguration config, Lazy<IODataEndpoint, IODataEndpointData> endpoint)
        {
            RegisterEndpoint(config, endpoint.Value.GetContainerName(), endpoint.Value.GetModel());
        }

        private static void RegisterEndpoint(HttpConfiguration config, string containerName, Microsoft.Data.Edm.IEdmModel edmModel)
        {
            config.Routes.MapODataServiceRoute(
                routeName: String.Format("{0}/{1}", _apiBase, containerName)
                ,
                routePrefix: String.Format("{0}/{1}", _apiBase, containerName)
                ,
                model: edmModel
                ,
                batchHandler: new DefaultODataBatchHandler(GlobalConfiguration.DefaultServer)
                );
        }
    }
}
