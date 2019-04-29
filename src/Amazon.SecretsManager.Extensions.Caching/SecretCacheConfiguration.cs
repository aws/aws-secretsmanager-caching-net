namespace Amazon.SecretsManager.Extensions.Caching
{
    /// <summary>
    /// A class used for configuring AWS Secrets Manager client-side caching.
    /// </summary>
    public class SecretCacheConfiguration
    {
        public const ushort DEFAULT_MAX_CACHE_SIZE = 1024;
        public const string DEFAULT_VERSION_STAGE = "AWSCURRENT";
        public const uint DEFAULT_CACHE_ITEM_TTL = 3600000;

        /// <summary>
        /// Gets or sets the TTL of a cache item in milliseconds. The default value for this is 3600000 millseconds, or one hour.
        /// </summary>
        public uint CacheItemTTL { get; set; } = DEFAULT_CACHE_ITEM_TTL;

        /// <summary>
        /// Gets or sets the maximum number of items the SecretsManagerCache will store before evicting items
        /// using the LRU strategy. The default value for this is 1024 items.
        /// </summary>
        public ushort MaxCacheSize { get; set; } = DEFAULT_MAX_CACHE_SIZE;

        /// <summary>
        /// Gets or sets the Version Stage the SecretsManagerCache will request when retrieving
        /// secrets from Secrets Manager. The default value for this is AWSCURRENT.
        /// </summary>
        public string VersionStage { get; set; } = DEFAULT_VERSION_STAGE;

        /// <summary>
        /// Gets or sets the <see cref="IAmazonSecretsManager"/> client implementation.
        /// </summary>
        public IAmazonSecretsManager Client { get; set; } = null;

        /// <summary>
        /// Gets or sets the optional <see cref="ISecretCacheHook"/> implementation.
        /// </summary>
        public ISecretCacheHook CacheHook { get; set; } = null;
    }
}