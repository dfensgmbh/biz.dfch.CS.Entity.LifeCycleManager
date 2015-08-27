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
using System.Collections.Specialized;
using System.Configuration;
using System.Data.Entity.Infrastructure.Pluralization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.OData;
using System.Web.Http.OData.Builder;
using System.Web.Http.OData.Query;
using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Entity;
using biz.dfch.CS.Entity.LifeCycleManager.Logging;
using biz.dfch.CS.Entity.LifeCycleManager.Model;
using biz.dfch.CS.Entity.LifeCycleManager.UserData;
using Microsoft.Data.OData;
using Newtonsoft.Json;

namespace biz.dfch.CS.Entity.LifeCycleManager.Controller
{
    public class LifeCyclesController : TenantAwareODataController
    {
        private const String _permissionInfix = "LifeCycle";
        private const String _permissionPrefix = "LightSwitchApplication";
        private const String CALLOUT_JOB_TYPE = "CalloutData";
        private const String CORE_ENDPOINT_URL_KEY = "LifeCycleManager.Endpoint.Core";

        private static ODataValidationSettings _validationSettings = new ODataValidationSettings();
        private static EnglishPluralizationService _pluralizationService = new EnglishPluralizationService();
        private CumulusCoreService.Core _coreService;

        public LifeCyclesController()
        {
            var declaringType = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType;
            var fn = String.Format("{0}:{1}",
                declaringType.Namespace,
                declaringType.Name);

            _coreService = new CumulusCoreService.Core(
            new Uri(ConfigurationManager.AppSettings[CORE_ENDPOINT_URL_KEY]));

            Debug.WriteLine(fn);
        }

        internal static void ModelBuilder(ODataConventionModelBuilder builder)
        {
            var EntitySetName = "LifeCycles";

            builder.EntitySet<LifeCycle>(EntitySetName);

            builder.Entity<LifeCycle>().Action("Next").Returns<String>();
            builder.Entity<LifeCycle>().Action("Cancel").Returns<String>();

            builder.Entity<LifeCycle>().Action("Allow").Returns<String>();
            builder.Entity<LifeCycle>().Action("Decline").Returns<String>();
        }

        // GET: api/Utilities.svc/LifeCycle
        public async Task<IHttpActionResult> GetLifeCycles(ODataQueryOptions<LifeCycle> queryOptions)
        {
            var declaringType = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType;
            var fn = String.Format("{0}:{1}",
                declaringType.Namespace,
                declaringType.Name);

            try
            {
                queryOptions.Validate(_validationSettings);
            }
            catch (ODataException ex)
            {
                Debug.WriteLine(String.Format("{0}: {1}\r\n{2}", ex.Source, ex.Message, ex.StackTrace));
                return BadRequest(ex.Message);
            }

            Debug.WriteLine(fn);

            // return Ok<IEnumerable<LifeCycle>>(lifeCycles);
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        // GET: api/Utilities.svc/LifeCycles(5)
        public async Task<IHttpActionResult> GetLifeCycle([FromODataUri] String key, ODataQueryOptions<LifeCycle> queryOptions)
        {
            var declaringType = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType;
            var fn = String.Format("{0}:{1}",
                declaringType.Namespace,
                declaringType.Name);

            try
            {
                queryOptions.Validate(_validationSettings);
            }
            catch (ODataException ex)
            {
                Debug.WriteLine(String.Format("{0}: {1}\r\n{2}", ex.Source, ex.Message, ex.StackTrace));
                return BadRequest(ex.Message);
            }
            
            Debug.WriteLine(fn);

            // return Ok<LifeCycle>(LifeCycle);
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        // PUT: api/Utilities.svc/LifeCycles(5)
        public async Task<IHttpActionResult> Put([FromODataUri] String key, LifeCycle lifeCycle)
        {
            var declaringType = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType;
            var fn = String.Format("{0}:{1}",
                declaringType.Namespace,
                declaringType.Name);

            if (!ModelState.IsValid)
            {
                Debug.WriteLine("Entity to be changed by LifeCycleManager with id '{0}' has invalid ModelState.", key);
                return BadRequest(ModelState);
            }

            if (key != lifeCycle.Id)
            {
                return BadRequest();
            }

            try
            {
                Debug.WriteLine(fn);

                var identity = CurrentUserDataProvider.GetIdentity(TenantId);

                var permissionId = CreatePermissionId("CanUpdate");
                if (!identity.Permissions.Contains(permissionId))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }

                var entityUri = new Uri(key);
                // DFTODO Pass IAuthenticationProvider implementation instead of null
                var entity = LoadEntity(null, entityUri);
                var lifeCycleManager = new LifeCycleManager(null, ExtractTypeFromUriString(key));
                lifeCycleManager.RequestStateChange(entityUri, entity, lifeCycle.Condition, TenantId);

                return Ok();
            }
            catch (UriFormatException e)
            {
                return BadRequest("Invalid Id - Id has to be a valid URI");
            }
            catch (HttpRequestException e)
            {
                return BadRequest("Loading entity from passed Uri failed (Either not found or not authorized)");
            }
            catch (InvalidOperationException e)
            {
                return BadRequest(String.Format("Changing state with provided condition: '{0}' not possible", lifeCycle.Condition));
            }
            catch (Exception e)
            {
                Debug.WriteLine(String.Format("{0}: {1}\r\n{2}", e.Source, e.Message, e.StackTrace));
                throw;
            }
        }

        // POST: api/Utilities.svc/LifeCycles
        public async Task<IHttpActionResult> Post(LifeCycle lifeCycle)
        {
            var declaringType = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType;
            var fn = String.Format("{0}:{1}",
                declaringType.Namespace,
                declaringType.Name);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Debug.WriteLine(fn);

            // TODO: Add create logic here.

            // return Created(LifeCycle);
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        // PATCH: api/Utilities.svc/LifeCycles(5)
        [AcceptVerbs("PATCH", "MERGE")]
        public async Task<IHttpActionResult> Patch([FromODataUri] String key, Delta<LifeCycle> delta)
        {
            var declaringType = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType;
            var fn = String.Format("{0}:{1}",
                declaringType.Namespace,
                declaringType.Name);

            if (!ModelState.IsValid)
            {
                Debug.WriteLine("Entity to be changed by LifeCycleManager with id '{0}' has invalid ModelState.", key);
                return BadRequest(ModelState);
            }

            try
            {
                Debug.WriteLine(fn);

                var identity = CurrentUserDataProvider.GetIdentity(TenantId);

                var permissionId = CreatePermissionId("CanUpdate");
                if (!identity.Permissions.Contains(permissionId))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }

                var entityUri = new Uri(key);
                // DFTODO Pass IAuthenticationProvider implementation instead of null
                var entity = LoadEntity(null, entityUri);
                var lifeCycleManager = new LifeCycleManager(null, ExtractTypeFromUriString(key));
                lifeCycleManager.RequestStateChange(entityUri, entity, delta.GetEntity().Condition, TenantId);

                return Ok();
            }
            catch (UriFormatException e)
            {
                return BadRequest("Invalid Id - Id has to be a valid URI");
            }
            catch (HttpRequestException e)
            {
                return BadRequest("Loading entity from passed Uri failed (Either not found or not authorized)");
            }
            catch (InvalidOperationException e)
            {
                return BadRequest(String.Format("Chaning state with provided condition: '{0}' not possible", delta.GetEntity().Condition));
            }
            catch (Exception e)
            {
                Debug.WriteLine(String.Format("{0}: {1}\r\n{2}", e.Source, e.Message, e.StackTrace));
                throw;
            }
        }

        // DELETE: api/Utilities.svc/LifeCycles(5)
        public async Task<IHttpActionResult> Delete([FromODataUri] String key)
        {
            var declaringType = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType;
            var fn = String.Format("{0}:{1}",
                declaringType.Namespace,
                declaringType.Name);
            
            Debug.WriteLine(fn);
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Next([FromODataUri] String key, ODataActionParameters parameters)
        {
            var declaringType = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType;
            var fn = String.Format("{0}:{1}",
                declaringType.Namespace,
                declaringType.Name);

            try
            {
                Debug.WriteLine(fn);

                var identity = CurrentUserDataProvider.GetIdentity(TenantId);

                var permissionId = CreatePermissionId("CanNext");
                if (!identity.Permissions.Contains(permissionId))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }

                var entityUri = new Uri(key);
                // DFTODO Pass IAuthenticationProvider implementation instead of null
                var entity = LoadEntity(null, entityUri);
                var lifeCycleManager = new LifeCycleManager(null, ExtractTypeFromUriString(key));
                lifeCycleManager.Next(entityUri, entity, TenantId);

                return Ok();
            }
            catch (UriFormatException e)
            {
                return BadRequest("Invalid id (Id should be a valid URI)");
            }
            catch (HttpRequestException e)
            {
                return BadRequest("Loading entity from passed Uri failed (Either not found or not authorized)");
            }
            catch (InvalidOperationException e)
            {
                return BadRequest("Changing state with 'Continue' condition not possible");
            }
            catch (Exception e)
            {
                Debug.WriteLine(String.Format("{0}: {1}\r\n{2}", e.Source, e.Message, e.StackTrace));
                throw;
            }
        }

        [HttpPost]
        public async Task<IHttpActionResult> Cancel([FromODataUri] String key, ODataActionParameters parameters)
        {
            var declaringType = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType;
            var fn = String.Format("{0}:{1}",
                declaringType.Namespace,
                declaringType.Name);

            try
            {
                Debug.WriteLine(fn);

                var identity = CurrentUserDataProvider.GetIdentity(TenantId);

                var permissionId = CreatePermissionId("CanCancel");
                if (!identity.Permissions.Contains(permissionId))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }

                var entityUri = new Uri(key);
                // DFTODO Pass IAuthenticationProvider implementation instead of null
                var entity = LoadEntity(null, entityUri);
                var lifeCycleManager = new LifeCycleManager(null, ExtractTypeFromUriString(key));
                lifeCycleManager.Cancel(entityUri, entity, TenantId);

                return Ok();
            }
            catch (UriFormatException e)
            {
                return BadRequest("Invalid id (Id should be a valid URI)");
            }
            catch (HttpRequestException e)
            {
                return BadRequest("Loading entity from passed Uri failed (Either not found or not authorized)");
            }
            catch (InvalidOperationException e)
            {
                return BadRequest("Changing state with 'Cancel' condition not possible");
            }
            catch (Exception e)
            {
                Debug.WriteLine(String.Format("{0}: {1}\r\n{2}", e.Source, e.Message, e.StackTrace));
                throw;
            }
        }

        [HttpPost]
        public async Task<IHttpActionResult> Allow([FromODataUri] String token, ODataActionParameters parameters)
        {
            var declaringType = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType;
            var fn = String.Format("{0}:{1}",
                declaringType.Namespace,
                declaringType.Name);

            try
            {
                Debug.WriteLine(fn);
                var identity = CurrentUserDataProvider.GetIdentity(TenantId);

                var permissionId = CreatePermissionId("CanAllow");
                if (!identity.Permissions.Contains(permissionId))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }

                var job = _coreService.Jobs.Where(j => token == j.Token &&
                    CALLOUT_JOB_TYPE == j.Type &&
                    j.State == JobStateEnum.Running.ToString())
                    .SingleOrDefault();

                if (null == job)
                {
                    return StatusCode(HttpStatusCode.NotFound);
                }

                var calloutDefinition = JsonConvert.DeserializeObject<CalloutData>(job.Parameters);
                // DFTODO Pass IAuthenticationProvider implementation instead of null
                var lifeCycleManager = new LifeCycleManager(null, calloutDefinition.EntityType);
                lifeCycleManager.OnAllowCallback(job);

                return Ok();
            }
            catch (InvalidOperationException e)
            {
                return BadRequest(String.Format("Allow job with token: '{0}' not possible", token));
            }
            catch (Exception e)
            {
                Debug.WriteLine(String.Format("{0}: {1}\r\n{2}", e.Source, e.Message, e.StackTrace));
                throw;
            }
        }

        [HttpPost]
        public async Task<IHttpActionResult> Decline([FromODataUri] String token, ODataActionParameters parameters)
        {
            var declaringType = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType;
            var fn = String.Format("{0}:{1}",
                declaringType.Namespace,
                declaringType.Name);

            try
            {
                Debug.WriteLine(fn);
                var identity = CurrentUserDataProvider.GetIdentity(TenantId);

                var permissionId = CreatePermissionId("CanDecline");
                if (!identity.Permissions.Contains(permissionId))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }

                var job = _coreService.Jobs.Where(j => token == j.Token && 
                    CALLOUT_JOB_TYPE == j.Type &&
                    j.State == JobStateEnum.Running.ToString())
                    .SingleOrDefault();

                if (null == job)
                {
                    return StatusCode(HttpStatusCode.NotFound);
                }

                var calloutDefinition = JsonConvert.DeserializeObject<CalloutData>(job.Parameters);
                // DFTODO Pass IAuthenticationProvider implementation instead of null
                var lifeCycleManager = new LifeCycleManager(null, calloutDefinition.EntityType);
                lifeCycleManager.OnDeclineCallback(job);

                return Ok();
            }
            catch (InvalidOperationException e)
            {
                return BadRequest(String.Format("Decline job with token: '{0} not possible", token));
            }
            catch (Exception e)
            {
                Debug.WriteLine(String.Format("{0}: {1}\r\n{2}", e.Source, e.Message, e.StackTrace));
                throw;
            }
        }

        private String CreatePermissionId(String permissionSuffix)
        {
            return String.Format("{0}:{1}{2}", _permissionPrefix, _permissionInfix, permissionSuffix);
        }

        private String LoadEntity(IAuthenticationProvider authenticationProvider, Uri uri)
        {
            var entityController = new EntityController(authenticationProvider);
            return entityController.LoadEntity(uri);
        }

        private String ExtractTypeFromUriString(String key)
        {
            var begin = key.LastIndexOf("/");
            var end = key.IndexOf("(");
            return _pluralizationService.Singularize(key.Substring(begin + 1, end - begin - 1));
        }
    }
}
