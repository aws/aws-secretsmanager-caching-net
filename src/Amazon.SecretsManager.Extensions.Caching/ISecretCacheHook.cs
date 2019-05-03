/*
 * Copyright 2018 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License"). You may not use this file except in compliance with
 * the License. A copy of the License is located at
 *
 * http://aws.amazon.com/apache2.0
 *
 * or in the "license" file accompanying this file. This file is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR
 * CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
 * and limitations under the License.
 */

namespace Amazon.SecretsManager.Extensions.Caching
{
    /// <summary>
    /// Interface to hook the local in-memory cache.  This interface will allow
    /// for clients to perform actions on the items being stored in the in-memory
    /// cache. One example would be encrypting/decrypting items stored in the
    /// in-memory cache.
    /// </summary>
    public interface ISecretCacheHook
    {
        /// <summary>
        /// Prepare the object for storing in the cache.
        /// </summary>
        object Put(object o);

        /// <summary>
        /// Derive the object from the cached object.
        /// </summary>
        object Get(object cachedObject);
    }
}
