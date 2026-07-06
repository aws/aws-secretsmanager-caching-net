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
    /// A class used for configuring AWS Secrets Manager client-side caching.
    /// </summary>
    public class SecretCacheConfiguration
    {
        public const ushort DEFAULT_MAX_CACHE_SIZE = 1024;
        public const string DEFAULT_VERSION_STAGE = "AWSCURRENT";
        public const uint DEFAULT_CACHE_ITEM_TTL = 3600000;
        public const uint DEFAULT_EXCEPTION_RETRY_DELAY_BASE = 1000;
        public const uint DEFAULT_EXCEPTION_RETRY_DELAY_MAX = 128000;
        public const uint DEFAULT_FORCE_REFRESH_DELAY_BASE = 3500;
        public const uint DEFAULT_FORCE_REFRESH_DELAY_JITTER = 1000;

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

        /// <summary>
        /// Gets or sets the base delay in milliseconds for exponential backoff after a failed request.
        /// The default value is 1000 milliseconds (1 second).
        /// </summary>
        public uint ExceptionRetryDelayBase { get; set; } = DEFAULT_EXCEPTION_RETRY_DELAY_BASE;

        /// <summary>
        /// Gets or sets the maximum delay in milliseconds for exponential backoff after repeated failures.
        /// The default value is 128000 milliseconds (128 seconds).
        /// </summary>
        public uint ExceptionRetryDelayMax { get; set; } = DEFAULT_EXCEPTION_RETRY_DELAY_MAX;

        /// <summary>
        /// Gets or sets the base delay in milliseconds for the jitter sleep during a forced refresh.
        /// The default value is 3500 milliseconds (3.5 seconds).
        /// </summary>
        public uint ForceRefreshDelayBase { get; set; } = DEFAULT_FORCE_REFRESH_DELAY_BASE;

        /// <summary>
        /// Gets or sets the random jitter variance in milliseconds added to the forced refresh delay.
        /// The default value is 1000 milliseconds (1 second).
        /// </summary>
        public uint ForceRefreshDelayJitter { get; set; } = DEFAULT_FORCE_REFRESH_DELAY_JITTER;
    }
}