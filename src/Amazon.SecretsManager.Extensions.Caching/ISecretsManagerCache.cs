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

    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// A class used for clide-side caching of secrets stored in AWS Secrets Manager
    /// </summary>
    public interface ISecretsManagerCache : IDisposable
    {

        /// <summary>
        /// Returns the cache entry corresponding to the specified secret if it exists in the cache.
        /// Otherwise, the secret value is fetched from Secrets Manager and a new cache entry is created.
        /// </summary>
        SecretCacheItem GetCachedSecret(string secretId);

        /// <summary>
        /// Asynchronously retrieves the specified SecretBinary after calling <see cref="GetCachedSecret"/>.
        /// </summary>
        Task<byte[]> GetSecretBinary(string secretId);

        /// <summary>
        /// Asynchronously retrieves the specified SecretString after calling <see cref="GetCachedSecret"/>.
        /// </summary>
        Task<string> GetSecretString(string secretId);

        /// <summary>
        /// Requests the secret value from SecretsManager asynchronously and updates the cache entry with any changes.
        /// If there is no existing cache entry, a new one is created.
        /// Returns true or false depending on if the refresh is successful.
        /// </summary>
        Task<bool> RefreshNowAsync(string secretId);
    }
}