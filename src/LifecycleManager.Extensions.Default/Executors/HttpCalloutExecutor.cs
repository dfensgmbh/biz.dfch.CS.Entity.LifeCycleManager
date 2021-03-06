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
using System.Net.Http;
using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Entity;
using biz.dfch.CS.Entity.LifeCycleManager.Contracts.Executors;
using biz.dfch.CS.Utilities.Rest;
using LifeCycleManager.Extensions.Default.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using HttpMethod = biz.dfch.CS.Utilities.Rest.HttpMethod;

namespace LifecycleManager.Extensions.Default.Executors
{
    [Export(typeof(ICalloutExecutor))]
    public class HttpCalloutExecutor : ICalloutExecutor
    {
        private const String CALLOUT_URL_JSON_PROPERTY = "callout-url";
        private const String AUTHENTICATION_Scheme_JSON_PROPERTY = "authentication-scheme";
        private const String AUTHENTICATION_VALUE_JSON_PROPERTY = "authentication-value";
        private const String AUTHORIZATION_HEADER_KEY = "Authorization";

        private RestCallExecutor _restCallExecutor;

        public HttpCalloutExecutor()
        {
           _restCallExecutor = new RestCallExecutor(true);
        }

        public void ExecuteCallout(String definitionParameters, CalloutData data)
        {
            var requestUrl = ExtractUrlFromDefinition(definitionParameters);
            var headers = ExtractAuthorizationHeadersFromDefinition(definitionParameters);

            if (null == data)
            {
                Debug.WriteLine("CalloutData is null");
                throw new ArgumentNullException("data", "CalloutData must not be null");
            }

            Debug.WriteLine("Executing callout to '{0}'", requestUrl.ToString());

            try
            {
                _restCallExecutor.Invoke(HttpMethod.Post, requestUrl.ToString(), headers, JsonConvert.SerializeObject(data));
            }
            catch (HttpRequestException e)
            {
                Debug.WriteLine("Error occurred while executing callout: {0}", e.Message);
                throw;
            }
        }

        private IDictionary<String, String> ExtractAuthorizationHeadersFromDefinition(String definitionParameters)
        {
            var obj = JObject.Parse(definitionParameters);

            var authScheme = (String)obj[AUTHENTICATION_Scheme_JSON_PROPERTY];
            var authValue = (String) obj[AUTHENTICATION_VALUE_JSON_PROPERTY];

            if (null == authScheme || null == authValue)
            {
                _restCallExecutor.AuthScheme = null;
                return null;
            }
            _restCallExecutor.AuthScheme = authScheme;
            var headers = new Dictionary<String, String>();
            headers.Add(AUTHORIZATION_HEADER_KEY, authValue);
            return headers;
        }

        private Uri ExtractUrlFromDefinition(String definitionParameters)
        {
            var obj = JObject.Parse(definitionParameters);
            return new Uri((String)obj[CALLOUT_URL_JSON_PROPERTY]);
        }
    }
}
