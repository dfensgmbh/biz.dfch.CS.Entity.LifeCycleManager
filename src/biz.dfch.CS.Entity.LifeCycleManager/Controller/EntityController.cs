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
﻿using System.Collections.Generic;
﻿using System.Net.Http;
﻿using biz.dfch.CS.Entity.LifeCycleManager.Logging;
﻿using biz.dfch.CS.Entity.LifeCycleManager.UserData;
﻿using biz.dfch.CS.Utilities.Rest;
﻿using HttpMethod = biz.dfch.CS.Utilities.Rest.HttpMethod;

namespace biz.dfch.CS.Entity.LifeCycleManager.Controller
{
    public class EntityController
    {
        private const String AUTHORIZATION_HEADER_KEY = "Authorization";

        private RestCallExecutor _restCallExecutor;
        private IDictionary<String, String> _headers;
        private String _authType;

        public EntityController(IAuthenticationProvider authenticationProvider)
        {
            if (null != authenticationProvider)
            {
                _headers = new Dictionary<String, String>();
                _headers.Add(AUTHORIZATION_HEADER_KEY, authenticationProvider.GetAuthValue());
                _authType = authenticationProvider.GetAuthScheme();
            }

            Debug.Write("Initializing RestCallExecutor");
            _restCallExecutor = new RestCallExecutor(true);
            _restCallExecutor.AuthScheme = _authType;
        }

        public String LoadEntity(Uri entityUri)
        {
            Debug.WriteLine("Loading entity with Uri: '{0}'", entityUri);
            CheckEntityUri(entityUri);

            try
            {
                return _restCallExecutor.Invoke(entityUri.ToString(), _headers);
            }
            catch (HttpRequestException e)
            {
                Debug.WriteLine("Error occurred while fetching entity from '{0}': {1}", entityUri.ToString(), e.Message);
                throw;
            }
        }

        public void UpdateEntity(Uri entityUri, String entity)
        {
            Debug.WriteLine("Updating entity with Uri: '{0}'", entityUri);
            CheckEntityUri(entityUri);

            try
            {
                _restCallExecutor.Invoke(HttpMethod.Put, entityUri.ToString(), _headers, entity);
            }
            catch (HttpRequestException e)
            {
                Debug.WriteLine("Error occurred while updating entity '{0}': {1}", entityUri.ToString(), e.Message);
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
    }
}
