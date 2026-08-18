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
 * and limitations under the License. test
 */

namespace Amazon.SecretsManager.Extensions.Caching
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon.SecretsManager.Model;
    using Microsoft.Extensions.Caching.Memory;

    /// <summary>
    /// A default class representing a cached secret from AWS Secrets Manager.
    /// </summary>
    public class SecretCacheItem : SecretCacheObject<DescribeSecretResponse>
    {
        /// The cached secret value versions for this cached secret.
        private readonly MemoryCache versions = new MemoryCache(new MemoryCacheOptions());
        private readonly SemaphoreSlim versionsLock = new SemaphoreSlim(1, 1);
        private const ushort MAX_VERSIONS_CACHE_SIZE = 10;
        
        public SecretCacheItem(String secretId, IAmazonSecretsManager client, SecretCacheConfiguration config)
            : base(secretId, client, config)
        {
        }
        
        /// <summary>
        /// Asynchronously retrieves the most current DescribeSecretResponse from Secrets Manager
        /// as part of the Refresh operation.
        /// </summary>
        protected override async Task<DescribeSecretResponse> ExecuteRefreshAsync(CancellationToken cancellationToken = default)
        {
            return await client.DescribeSecretAsync(new DescribeSecretRequest { SecretId = secretId }, cancellationToken);
        }

        /// <summary>
        /// Asynchronously retrieves the GetSecretValueResponse from the proper SecretCacheVersion.
        /// </summary>
        protected override async Task<GetSecretValueResponse> GetSecretValueAsync(DescribeSecretResponse result, CancellationToken cancellationToken = default)
        {
            SecretCacheVersion version = await GetVersion(result, cancellationToken);
            if (version == null)
            {
                return null;
            }
            return await version.GetSecretValue(cancellationToken);
        }

        public override int GetHashCode()
        {
            return (secretId ?? string.Empty).GetHashCode();
        }

        public override string ToString()
        {
            return $"SecretCacheItem: {secretId}";
        }

        public override bool Equals(object obj)
        {
            return obj is SecretCacheItem sci && string.Equals(this.secretId, sci.secretId);
        }

        /// <summary>
        /// Asynchronously retrieves the <see cref="SecretCacheVersion"/> corresponding to the version stage
        /// specified by the <see cref="SecretCacheConfiguration"/>.
        /// </summary>
        /// <param name="describeResult">The describe secret response containing version-to-stage mappings.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the matching <see cref="SecretCacheVersion"/>, or <c>null</c> if no version matches the configured stage.</returns>
        private async Task<SecretCacheVersion> GetVersion(DescribeSecretResponse describeResult, CancellationToken cancellationToken = default)
        {
            if (null == describeResult?.VersionIdsToStages) return null;
            String currentVersionId = null;
            foreach (KeyValuePair<String, List<String>> entry in describeResult.VersionIdsToStages)
            {
                if (entry.Value.Contains(config.VersionStage))
                {
                    currentVersionId = entry.Key;
                    break;
                }
            }
            if (currentVersionId != null)
            {
                SecretCacheVersion version = versions.Get<SecretCacheVersion>(currentVersionId);
                if (version == null)
                {
                    await this.versionsLock.WaitAsync(cancellationToken);
                    try
                    {
                        version = versions.GetOrCreate<SecretCacheVersion>(currentVersionId, entry =>
                        {
                            return new SecretCacheVersion(secretId, currentVersionId, client, config);
                        });

                        if (versions.Count > MAX_VERSIONS_CACHE_SIZE)
                        {
                            TrimCacheToSizeLimit();
                        }
                    }
                    finally
                    {
                        this.versionsLock.Release();
                    } 
                }
                return version;
            }
            return null;
        }

        private void TrimCacheToSizeLimit()
        {
            versions.Compact((double)(versions.Count - MAX_VERSIONS_CACHE_SIZE) / versions.Count);
        }
    }
}
