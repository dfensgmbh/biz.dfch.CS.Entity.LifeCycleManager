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
                System.Diagnostics.Trace.WriteLine(String.Format("WARNING: Loading extensions from '{0}' FAILED.\n{1}", folder, ex.Message));
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
