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
    using System.Collections.Generic;
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
        private const ushort MAX_VERSIONS_CACHE_SIZE = 10;
        
        public SecretCacheItem(String secretId, IAmazonSecretsManager client, SecretCacheConfiguration config)
            : base(secretId, client, config)
        {
        }
        
        /// <summary>
        /// Asynchronously retrieves the most current DescribeSecretResponse from Secrets Manager
        /// as part of the Refresh operation.
        /// </summary>
        protected override async Task<DescribeSecretResponse> ExecuteRefreshAsync()
        {
            return await client.DescribeSecretAsync(new DescribeSecretRequest { SecretId = secretId });
        }

        /// <summary>
        /// Asynchronously retrieves the GetSecretValueResponse from the proper SecretCacheVersion.
        /// </summary>
        protected override async Task<GetSecretValueResponse> GetSecretValueAsync(DescribeSecretResponse result)
        {
            SecretCacheVersion version = GetVersion(result);
            if (version == null)
            {
                return null;
            }
            return await version.GetSecretValue();
        }

        public override int GetHashCode()
        {
            return String.Format("%s", secretId).GetHashCode();
        }

        public override string ToString()
        {
            return String.Format("SecretCacheItem: %s", secretId);
        }

        public override bool Equals(object obj)
        {
            return obj is SecretCacheItem sci && string.Equals(this.secretId, sci.secretId);
        }

        /// <summary>
        /// Retrieves the SecretCacheVersion corresponding to the Version Stage
        /// specified by the SecretCacheConfiguration.
        /// </summary>
        private SecretCacheVersion GetVersion(DescribeSecretResponse describeResult)
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
                if (null == version)
                {
                    version = versions.Set(currentVersionId, new SecretCacheVersion(secretId, currentVersionId, client, config));
                    if (versions.Count > MAX_VERSIONS_CACHE_SIZE)
                    {
                        TrimCacheToSizeLimit();
                    }
                }
                return version;
            }
            return null;
        }

        private void TrimCacheToSizeLimit()
        {
            versions.Compact((double)(versions.Count - config.MaxCacheSize) / versions.Count);
        }
    }
}