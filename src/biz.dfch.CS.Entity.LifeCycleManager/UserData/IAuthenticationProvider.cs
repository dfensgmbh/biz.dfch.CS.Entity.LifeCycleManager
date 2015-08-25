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

namespace biz.dfch.CS.Entity.LifeCycleManager.UserData
{
    public interface IAuthenticationProvider
    {
        /// <summary>
        /// Value according the specified scheme
        /// (i.e. the bearer token, if AuthScheme is 'Bearer'
        /// </summary>
        /// <returns></returns>
        String GetAuthValue();

        /// <summary>
        /// Authentication scheme used to create Authorization header
        /// (i.e. 'Basic', 'Bearer',...)
        /// </summary>
        /// <returns></returns>
        String GetAuthScheme();
    }
}
