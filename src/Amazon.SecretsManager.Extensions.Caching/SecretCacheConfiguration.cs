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

    /// <summary>
    /// A class used for configuring AWS Secrets Manager client-side caching.
    /// </summary>
    public class SecretCacheConfiguration
    {
        public const ushort DEFAULT_MAX_CACHE_SIZE = 1024;
        public const string DEFAULT_VERSION_STAGE = "AWSCURRENT";
        public const uint DEFAULT_CACHE_ITEM_TTL = 3600000;
        public static readonly TimeSpan DefaultExceptionRetryDelayBase = TimeSpan.FromSeconds(1);
        public static readonly TimeSpan DefaultExceptionRetryDelayMax = TimeSpan.FromSeconds(128);
        public static readonly TimeSpan DefaultForceRefreshDelayBase = TimeSpan.FromMilliseconds(3500);
        public static readonly TimeSpan DefaultForceRefreshDelayJitter = TimeSpan.FromSeconds(1);

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
        /// Gets or sets the base delay for exponential backoff after a failed request.
        /// The default value is 1 second.
        /// </summary>
        public TimeSpan ExceptionRetryDelayBase { get; set; } = DefaultExceptionRetryDelayBase;

        /// <summary>
        /// Gets or sets the maximum delay for exponential backoff after repeated failures.
        /// The default value is 128 seconds.
        /// </summary>
        public TimeSpan ExceptionRetryDelayMax { get; set; } = DefaultExceptionRetryDelayMax;

        /// <summary>
        /// Gets or sets the base delay for the jitter sleep during a forced refresh.
        /// The default value is 3.5 seconds.
        /// </summary>
        public TimeSpan ForceRefreshDelayBase { get; set; } = DefaultForceRefreshDelayBase;

        /// <summary>
        /// Gets or sets the random jitter variance added to the forced refresh delay.
        /// The default value is 1 second.
        /// </summary>
        public TimeSpan ForceRefreshDelayJitter { get; set; } = DefaultForceRefreshDelayJitter;
    }
}