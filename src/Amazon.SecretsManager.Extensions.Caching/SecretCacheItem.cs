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

        /// The next scheduled refresh time for this item.  Once the item is accessed
        /// after this time, the item will be synchronously refreshed.
        private long nextRefreshTime = 0;
        
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
            DescribeSecretResponse response = await client.DescribeSecretAsync(new DescribeSecretRequest { SecretId = secretId });
            int ttl = Convert.ToInt32(config.CacheItemTTL);
            nextRefreshTime = Environment.TickCount + SecretCacheObject<DescribeSecretRequest>.random.Value.Next(ttl / 2, ttl + 1);
            return response;
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