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
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Entity;
using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Executors;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LifecycleManager.Extensions.Default.Executors
{
    [Export(typeof(ICalloutExecutor))]
    public class HttpCalloutExecutor : ICalloutExecutor
    {
        // DFTODO Add logging lib

        private HttpClient _httpClient;
        private const String APPLICATION_JSON = "application/json";
        private Uri _url;

        public HttpCalloutExecutor(String definitionParameters)
        {
            Debug.Write("Initializing Http Client");
            // DFTODO Authentication/Credentials?
            _httpClient = new HttpClient();
            _url = ExtractUrlFromDefinition(definitionParameters);
            _httpClient.BaseAddress = _url;
        }

        private Uri ExtractUrlFromDefinition(String definitionParameters)
        {
            var obj = JObject.Parse(definitionParameters);
            return new Uri((String)obj["request-url"]);
        }

        public void ExecuteCallout(CalloutData data)
        {
            if (null == data)
            {
                Debug.WriteLine("CalloutData parameter is null");
                throw new ArgumentException("Callout data should not be null");
            }
            Debug.WriteLine("Executing callout to '{0}'", _url);
            
            SetHeaders();
            int _TimeoutSec = 90;
            _httpClient.Timeout = new TimeSpan(0, 0, _TimeoutSec);
            HttpContent body = new StringContent(JsonConvert.SerializeObject(data));
            body.Headers.ContentType = new MediaTypeHeaderValue(APPLICATION_JSON);
            HttpResponseMessage response = _httpClient.PostAsync(_url, body).Result;
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e)
            {
                Debug.WriteLine("Error occurred while executing callout: {0}", e.Message);
                throw new ArgumentException(String.Format("The callout request URL '{0}' is not valid", _url), e);
            }
        }

        private void SetHeaders()
        {
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(APPLICATION_JSON));
        }
    }
}
