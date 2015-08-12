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
using System.Data.Entity.Infrastructure.Pluralization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
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
using Job = biz.dfch.CS.Entity.LifeCycleManager.CumulusCoreService.Job;

namespace biz.dfch.CS.Entity.LifeCycleManager.Controller
{
    public class LifeCyclesController : ODataController
    {
        private const String _permissionInfix = "LifeCycle";
        private const String _permissionPrefix = "CumulusCore";

        private static ODataValidationSettings _validationSettings = new ODataValidationSettings();
        private static EnglishPluralizationService _pluralizationService = new EnglishPluralizationService();
        private static CumulusCoreService.Core _coreService = new CumulusCoreService.Core(
            new Uri(ConfigurationManager.AppSettings["Core.Endpoint"]));

        public LifeCyclesController()
        {
            String fn = String.Format("{0}:{1}",
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
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
            String fn = String.Format("{0}:{1}",
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

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
            String fn = String.Format("{0}:{1}",
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

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
            String fn = String.Format("{0}:{1}",
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

            if (!ModelState.IsValid)
            {
                Debug.WriteLine("Entity to be changed by LifeCycleManager with id '{0}' has invalid ModelState.", key);
                return BadRequest(ModelState);
            }

            if (!key.Equals(lifeCycle.Id))
            {
                return BadRequest();
            }

            try
            {
                Debug.WriteLine(fn);

                var permissionId = CreatePermissionId("CanUpdate");
                if (!CurrentUserDataProvider.HasCurrentUserPermission(permissionId))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }

                var entityUri = new Uri(key);
                // DFTODO Check what ICredentialProvider implementation to pass instead of null
                var entity = LoadEntity(null, entityUri);
                var lifeCycleManager = new LifeCycleManager(null, ExtractTypeFromUriString(key));
                lifeCycleManager.ChangeState(entityUri, entity, lifeCycle.Condition);

                return Ok();
            }
            catch (UriFormatException e)
            {
                return BadRequest("Invalid Id - Id has to be a valid URI");
            }
            catch (HttpRequestException e)
            {
                return BadRequest("Unable to load entity from passed Uri (Either not found or not authorized)");
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
            String fn = String.Format("{0}:{1}",
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

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
            String fn = String.Format("{0}:{1}",
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

            if (!ModelState.IsValid)
            {
                Debug.WriteLine("Entity to be changed by LifeCycleManager with id '{0}' has invalid ModelState.", key);
                return BadRequest(ModelState);
            }

            try
            {
                Debug.WriteLine(fn);

                var permissionId = CreatePermissionId("CanUpdate");
                if (!CurrentUserDataProvider.HasCurrentUserPermission(permissionId))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }

                var entityUri = new Uri(key);
                // DFTODO Check what ICredentialProvider implementation to pass instead of null
                var entity = LoadEntity(null, entityUri);
                var lifeCycleManager = new LifeCycleManager(null, ExtractTypeFromUriString(key));
                lifeCycleManager.ChangeState(entityUri, entity, delta.GetEntity().Condition);

                return Ok();
            }
            catch (UriFormatException e)
            {
                return BadRequest("Invalid Id - Id has to be a valid URI");
            }
            catch (HttpRequestException e)
            {
                return BadRequest("Unable to load entity from passed Uri (Either not found or not authorized)");
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
            String fn = String.Format("{0}:{1}",
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
            
            // TODO: Add delete logic here.

            Debug.WriteLine(fn);
            // return StatusCode(HttpStatusCode.NoContent);
            return StatusCode(HttpStatusCode.NotImplemented);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Next([FromODataUri] String key, ODataActionParameters parameters)
        {
            String fn = String.Format("{0}:{1}",
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

            try
            {
                Debug.WriteLine(fn);

                var permissionId = CreatePermissionId("CanNext");
                if (!CurrentUserDataProvider.HasCurrentUserPermission(permissionId))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }

                var entityUri = new Uri(key);
                // DFTODO Check what ICredentialProvider implementation to pass instead of null
                var entity = LoadEntity(null, entityUri);
                var lifeCycleManager = new LifeCycleManager(null, ExtractTypeFromUriString(key));
                lifeCycleManager.Next(entityUri, entity);

                return Ok();
            }
            catch (UriFormatException e)
            {
                return BadRequest("Invalid id (Id should be a valid URI)");
            }
            catch (HttpRequestException e)
            {
                return BadRequest("Unable to load entity from passed Uri (Either not found or not authorized)");
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
            String fn = String.Format("{0}:{1}",
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

            try
            {
                Debug.WriteLine(fn);

                var permissionId = CreatePermissionId("CanCancel");
                if (!CurrentUserDataProvider.HasCurrentUserPermission(permissionId))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }

                var entityUri = new Uri(key);
                // DFTODO Check what ICredentialProvider implementation to pass instead of null
                var entity = LoadEntity(null, entityUri);
                var lifeCycleManager = new LifeCycleManager(null, ExtractTypeFromUriString(key));
                lifeCycleManager.Cancel(entityUri, entity);

                return Ok();
            }
            catch (UriFormatException e)
            {
                return BadRequest("Invalid id (Id should be a valid URI)");
            }
            catch (HttpRequestException e)
            {
                return BadRequest("Unable to load entity from passed Uri (Either not found or not authorized)");
            }
            catch (Exception e)
            {
                Debug.WriteLine(String.Format("{0}: {1}\r\n{2}", e.Source, e.Message, e.StackTrace));
                throw;
            }
        }

        [HttpPost]
        public async Task<IHttpActionResult> Allow([FromODataUri] int key, ODataActionParameters parameters)
        {
            // DFTODO Check how to avoid that remote apps can allow jobs of another user

            String fn = String.Format("{0}:{1}",
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

            try
            {
                Debug.WriteLine(fn);

                var permissionId = CreatePermissionId("CanAllow");
                if (!CurrentUserDataProvider.HasCurrentUserPermission(permissionId))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }

                var job = _coreService.Jobs.Where(j => j.Id == key && j.State.Equals(StateEnum.Running.ToString()))
                    .SingleOrDefault();

                if (null == job)
                {
                    return StatusCode(HttpStatusCode.NotFound);
                }

                // DFTODO Check what ICredentialProvider implementation to pass instead of null
                DelegateJobHandlingToLifeCycleManager(job);

                return Ok();
            }
            catch (Exception e)
            {
                Debug.WriteLine(String.Format("{0}: {1}\r\n{2}", e.Source, e.Message, e.StackTrace));
                throw;
            }
        }

        [HttpPost]
        public async Task<IHttpActionResult> Decline([FromODataUri] int key, ODataActionParameters parameters)
        {
            // DFTODO Check how to avoid that remote apps can decline jobs of another user

            String fn = String.Format("{0}:{1}",
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

            try
            {
                Debug.WriteLine(fn);

                var permissionId = CreatePermissionId("CanDecline");
                if (!CurrentUserDataProvider.HasCurrentUserPermission(permissionId))
                {
                    return StatusCode(HttpStatusCode.Forbidden);
                }

                var job = _coreService.Jobs.Where(j => j.Id == key && j.State.Equals(StateEnum.Running.ToString()))
                    .SingleOrDefault();

                if (null == job)
                {
                    return StatusCode(HttpStatusCode.NotFound);
                }

                // DFTODO Check what ICredentialProvider implementation to pass instead of null
                DelegateJobHandlingToLifeCycleManager(job);

                return Ok();
            }
            catch (Exception e)
            {
                Debug.WriteLine(String.Format("{0}: {1}\r\n{2}", e.Source, e.Message, e.StackTrace));
                throw;
            }
        }

        private void DelegateJobHandlingToLifeCycleManager(Job job)
        {
            var calloutDefinition = JsonConvert.DeserializeObject<CalloutData>(job.Parameters);
            var lifeCycleManager = new LifeCycleManager(null, calloutDefinition.EntityType);
            lifeCycleManager.OnCallback(job);
        }

        private String CreatePermissionId(String permissionSuffix)
        {
            return String.Format("{0}:{1}{2}", _permissionPrefix, _permissionInfix, permissionSuffix);
        }

        private String LoadEntity(ICredentialProvider credentialProvider, Uri uri)
        {
            // DFTODO Check how to pass credentials used to authenticate when loading the entity
            // DFTODO Check if user is authorized to change the state of the entity with the given ID
            var entityLoader = new EntityController(credentialProvider);
            return entityLoader.LoadEntity(uri);
        }

        private String ExtractTypeFromUriString(String key)
        {
            var begin = key.LastIndexOf("/");
            var end = key.IndexOf("(");
            return _pluralizationService.Singularize(key.Substring(begin + 1, end - begin - 1));
        }
    }
}
