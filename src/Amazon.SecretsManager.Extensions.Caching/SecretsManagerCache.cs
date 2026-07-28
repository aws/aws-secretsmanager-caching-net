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
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon.Runtime;
    using Amazon.SecretsManager.Model;
    using Microsoft.Extensions.Caching.Memory;

    /// <summary>
    /// A class used for clide-side caching of secrets stored in AWS Secrets Manager
    /// </summary>
    public class SecretsManagerCache : ISecretsManagerCache
    {
        private readonly IAmazonSecretsManager secretsManager;
        private readonly SecretCacheConfiguration config;
        private readonly MemoryCacheEntryOptions cacheItemPolicy;
        private readonly MemoryCache cache = new MemoryCache(new MemoryCacheOptions{ CompactionPercentage = 0 });
        private readonly SemaphoreSlim cacheLock = new SemaphoreSlim(1,1);

        /// <summary>
        /// Initializes a new instance of the <see cref="SecretsManagerCache"/> class.
        /// </summary>
        public SecretsManagerCache()
            : this(new AmazonSecretsManagerClient(), new SecretCacheConfiguration())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecretsManagerCache"/> class.
        /// </summary>
        public SecretsManagerCache(IAmazonSecretsManager secretsManager)
            : this(secretsManager, new SecretCacheConfiguration())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecretsManagerCache"/> class.
        /// </summary>
        public SecretsManagerCache(SecretCacheConfiguration config)
            : this(new AmazonSecretsManagerClient(), config)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecretsManagerCache"/> class.
        /// </summary>
        public SecretsManagerCache(IAmazonSecretsManager secretsManager, SecretCacheConfiguration config)
        {
            this.config = config;
            this.secretsManager = secretsManager;
            cacheItemPolicy = new MemoryCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(this.config.CacheItemTTL)
            };
            if (this.secretsManager is AmazonSecretsManagerClient sm)
            {
                sm.BeforeRequestEvent += this.ServiceClientBeforeRequestEvent;
            }
        }

        private void ServiceClientBeforeRequestEvent(object sender, RequestEventArgs e)
        {
            if (e is WebServiceRequestEventArgs args && args.Headers.ContainsKey(VersionInfo.USER_AGENT_HEADER) && !args.Headers[VersionInfo.USER_AGENT_HEADER].Contains(VersionInfo.USER_AGENT_STRING))
                args.Headers[VersionInfo.USER_AGENT_HEADER] = String.Format("{0}/{1}", args.Headers[VersionInfo.USER_AGENT_HEADER], VersionInfo.USER_AGENT_STRING);
        }

        /// <summary>
        /// Disposes all resources currently being used by the SecretManagerCache's underlying MemoryCache.
        /// </summary>
        public void Dispose()
        {
            cache.Dispose();
        }

        /// <summary>
        /// Asynchronously retrieves the specified SecretString after calling <see cref="GetCachedSecret"/>.
        /// </summary>
        /// <param name="secretId">The secret identifier (ARN or friendly name).</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the secret string value, or <c>null</c> if not present.</returns>
        public async Task<String> GetSecretString(String secretId, CancellationToken cancellationToken = default)
        {
            SecretCacheItem secret = await GetCachedSecret(secretId, cancellationToken);
            GetSecretValueResponse response = null;
            response = await secret.GetSecretValue(cancellationToken);
            return response?.SecretString;
        }

        /// <summary>
        /// Asynchronously retrieves the specified SecretBinary after calling <see cref="GetCachedSecret"/>.
        /// </summary>
        /// <param name="secretId">The secret identifier (ARN or friendly name).</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the secret binary value, or <c>null</c> if not present.</returns>
        public async Task<byte[]> GetSecretBinary(String secretId, CancellationToken cancellationToken = default)
        {
            SecretCacheItem secret = await GetCachedSecret(secretId, cancellationToken);
            GetSecretValueResponse response = null;
            response = await secret.GetSecretValue(cancellationToken);
            return response?.SecretBinary?.ToArray();
        }

        /// <summary>
        /// Requests the secret value from Secrets Manager asynchronously and updates the cache entry with any changes.
        /// If there is no existing cache entry, a new one is created.
        /// </summary>
        /// <param name="secretId">The secret identifier (ARN or friendly name).</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is <c>true</c> if the refresh succeeded; otherwise, <c>false</c>.</returns>
        public async Task<bool> RefreshNowAsync(String secretId, CancellationToken cancellationToken = default)
        {
            SecretCacheItem secretCacheItem = await GetCachedSecret(secretId, cancellationToken);
            return await secretCacheItem.RefreshNowAsync(cancellationToken);
        }

        /// <summary>
        /// Asynchronously returns the cache entry corresponding to the specified secret if it exists in the cache.
        /// Otherwise, the secret value is fetched from Secrets Manager and a new cache entry is created.
        /// </summary>
        /// <param name="secretId">The secret identifier (ARN or friendly name).</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="SecretCacheItem"/> for the specified secret.</returns>
        public async Task<SecretCacheItem> GetCachedSecret(string secretId, CancellationToken cancellationToken = default)
        {
            SecretCacheItem secret = cache.Get<SecretCacheItem>(secretId);

            if (secret == null)
            {
                await this.cacheLock.WaitAsync(cancellationToken);

                try
                {
                    secret = cache.GetOrCreate<SecretCacheItem>(secretId, entry =>
                    {
                        entry.SetOptions(cacheItemPolicy);
                        return new SecretCacheItem(secretId, secretsManager, config);
                    });

                    if (cache.Count > config.MaxCacheSize)
                    {
                        // Trim cache size to MaxCacheSize, evicting entries using LRU.
                        cache.Compact((double)(cache.Count - config.MaxCacheSize) / cache.Count);
                    }
                }
                finally
                {
                    this.cacheLock.Release();
                }
            }

            return secret;
        }
    }
}