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
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Configuration;
using System.IO;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Endpoint;
using biz.dfch.CS.Entity.LifeCycleManager.TestServer.Logging;

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config", Watch = true)]
namespace biz.dfch.CS.Entity.LifeCycleManager.TestServer
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        private CompositionContainer _container;

        /**
         * On ComposeParts MEF initializes the list with all parts found that match the contract
         * (Contract: Parts have to be of type IODataEndpoint)
         **/
        [ImportMany(typeof(IODataEndpoint))]
        private IEnumerable<Lazy<IODataEndpoint, IODataEndpointData>> endpoints;

        protected void Application_Start()
        {
            LoadAndComposeParts();

            WebApiConfig.InitEndpoints(endpoints);

            GlobalConfiguration.Configuration.Services.Replace(typeof(IAssembliesResolver), new DefaultAssembliesResolver());

            GlobalConfiguration.Configure(WebApiConfig.Register);

            Debug.WriteLine("Global.Application_Start() END.");
        }

        private void LoadAndComposeParts()
        {
            var assemblyCatalog = new AggregateCatalog();

            // Adds all the parts found in the given directory
            var folder = ConfigurationManager.AppSettings["LifeCycleManager.TestServer.ExtensionsFolder"];
            try
            {
                if (!Path.IsPathRooted(folder))
                {
                    folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, folder);
                }
                Debug.WriteLine("Plugin folder = " + folder);
                assemblyCatalog.Catalogs.Add(new DirectoryCatalog(folder));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(String.Format("WARNING: Loading extensions from '{0}' FAILED.\n{1}",
                    folder, ex.Message));
            }
            finally
            {
                // Adds all the parts found in the same assembly as the actual class
                assemblyCatalog.Catalogs.Add(new AssemblyCatalog(typeof(WebApiApplication).Assembly));
            }

            _container = new CompositionContainer(assemblyCatalog);

            try
            {
                _container.ComposeParts(this);
            }
            catch (CompositionException compositionException)
            {
                Console.WriteLine(compositionException.ToString());
            }
        }
    }
}
