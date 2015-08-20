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
﻿using System.Net.Http;
﻿using System.Net.Http.Headers;
﻿using biz.dfch.CS.Entity.LifeCycleManager.Logging;
﻿using biz.dfch.CS.Entity.LifeCycleManager.UserData;

namespace biz.dfch.CS.Entity.LifeCycleManager.Controller
{
    public class EntityController
    {
        private HttpClient _httpClient;
        private const String APPLICATION_JSON = "application/json";

        public EntityController(ICredentialProvider credentialProvider)
        {
            Debug.Write("Initializing Http Client");
            var clientHandler = new HttpClientHandler();
            if (null != credentialProvider)
            {
                clientHandler.Credentials = credentialProvider.GetCredentials();
            }
            _httpClient = new HttpClient(clientHandler);
        }

        public String LoadEntity(Uri entityUri)
        {
            Debug.WriteLine("Loading entity with Uri: '{0}'", entityUri);
            CheckEntityUri(entityUri);

            SetHeaders();
            _httpClient.BaseAddress = entityUri;
            int _TimeoutSec = 90;
            _httpClient.Timeout = new TimeSpan(0, 0, _TimeoutSec);

            HttpResponseMessage response = _httpClient.GetAsync(entityUri).Result;
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e)
            {
                Debug.WriteLine("Error occurred while fetching entity from '{0}'", e.Message);
                throw;
            }
            
            return response.Content.ReadAsStringAsync().Result;
        }

        public void UpdateEntity(Uri entityUri, String entity)
        {
            Debug.WriteLine("Updating entity with Uri: '{0}'", entityUri);
            CheckEntityUri(entityUri);

            SetHeaders();
            _httpClient.BaseAddress = entityUri;
            int _TimeoutSec = 90;
            _httpClient.Timeout = new TimeSpan(0, 0, _TimeoutSec);
            new StringContent(entity);
            HttpContent body = new StringContent(entity);
            body.Headers.ContentType = new MediaTypeHeaderValue(APPLICATION_JSON);
            HttpResponseMessage response = _httpClient.PutAsync(entityUri, body).Result;
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e)
            {
                Debug.WriteLine("Error occurred while updating entity '{0}'", e.Message);
                throw;
            }
        }

        private void CheckEntityUri(Uri entityUri)
        {
            if (null == entityUri)
            {
                Debug.WriteLine("Entity Uri passed to LoadEntity method is null");
                throw new ArgumentNullException("entityUri");
            }
        }

        private void SetHeaders()
        {
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(APPLICATION_JSON));
        }
    }
}
