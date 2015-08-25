﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Web.Http;
using System.Web.Http.OData.Batch;
using System.Web.Http.OData.Extensions;
using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Endpoint;

namespace biz.dfch.CS.Entity.LifeCycleManager.TestServer
{
    public static class WebApiConfig
    {
        private static String _apiBase = "api";
        private static List<String> _serverRoles = ConfigurationManager.AppSettings["LifeCycleManager.Server.ServerRoles"].Split(',').ToList<String>();

        private static IEnumerable<Lazy<IODataEndpoint, IODataEndpointData>> endpoints;

        public static void InitEndpoints(IEnumerable<Lazy<IODataEndpoint, IODataEndpointData>> endpoints)
        {
            WebApiConfig.endpoints = (null == endpoints) ? new List<Lazy<IODataEndpoint, IODataEndpointData>>() : endpoints;
        }

        public static void Register(HttpConfiguration config)
        {
            foreach (var endpoint in endpoints)
            {

                if (NotMatchingRoleOfServer(endpoint))
                {
                    continue;
                }

                if (IsContainerNameNotUnique(endpoint))
                {
                    Debug.WriteLine(String.Format("ContainerName '{0}' not unique", endpoint.Value.GetContainerName()));
                    continue;
                }

                RegisterMefEndpoint(config, endpoint);
            }
            config.MapHttpAttributeRoutes();
            Debug.Write("WebApiConfig::Register() END");
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
                routeName: String.Format("{0}/{1}.svc", _apiBase, containerName)
                ,
                routePrefix: String.Format("{0}/{1}.svc", _apiBase, containerName)
                ,
                model: edmModel
                ,
                batchHandler: new DefaultODataBatchHandler(GlobalConfiguration.DefaultServer)
                );
        }
    }
}
