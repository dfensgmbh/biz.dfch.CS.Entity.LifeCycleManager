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
﻿using System.Configuration;

namespace biz.dfch.CS.Entity.LifeCycleManager.UserData
{
    public class DefaultAuthenticationProvider : IAuthenticationProvider
    {
        private const String AUTHENTICATION_VALUE_PROPERTY_KEY = "EntityController.Authentication.Value";
        private const String AUTHENTICATION_SCHEME_PROPERTY_KEY = "EntityController.Authentication.Scheme";

        private static String AUTH_VALUE = ConfigurationManager.AppSettings[AUTHENTICATION_VALUE_PROPERTY_KEY];
        private static String AUTH_SCHEME = ConfigurationManager.AppSettings[AUTHENTICATION_SCHEME_PROPERTY_KEY]; 

        public String GetAuthValue()
        {
            return AUTH_VALUE;
        }

        public String GetAuthScheme()
        {
            return AUTH_SCHEME;
        }
    }
}
