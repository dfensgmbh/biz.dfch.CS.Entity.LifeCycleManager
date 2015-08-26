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
using System.ComponentModel.Composition;
using System.Configuration;
using System.Web.Http.OData.Builder;
using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Endpoint;
using biz.dfch.CS.Entity.LifeCycleManager.Logging;
using Microsoft.Data.Edm;

namespace biz.dfch.CS.Entity.LifeCycleManager
{
    [Export(typeof(IODataEndpoint))]
    [ExportMetadata("ServerRole", ServerRole.HOST)]
    public class CoreEndpoint : IODataEndpoint
    {
        private const String DEFAULT_CONTAINER_NAME = "Core";

        public IEdmModel GetModel()
        {
            Debug.WriteLine("Start building core endpoint...");
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            builder.ContainerName = GetContainerName();

            Controller.JobsController.ModelBuilder(builder);
            Controller.LifeCyclesController.ModelBuilder(builder);
            Controller.StateChangeLocksController.ModelBuilder(builder);
            Controller.CalloutDefinitionsController.ModelBuilder(builder);

            Debug.WriteLine("Core endpoint built!");

            return builder.GetEdmModel();
        }

        public String GetContainerName()
        {
            var containerName = ConfigurationManager.AppSettings["LifeCycleManager.Endpoint.Core.Container.Name"];
            if (String.IsNullOrWhiteSpace(containerName))
            {
                containerName = DEFAULT_CONTAINER_NAME;
            }
            return containerName;
        }
    }
}
