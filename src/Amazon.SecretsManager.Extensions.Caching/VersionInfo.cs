namespace Amazon.SecretsManager.Extensions.Caching
{
    internal static class VersionInfo
    {
        /// <summary>
        /// Incremented for design changes that break backward compatibility.
        /// </summary>
        public const string VERSION_NUM = "1";

        /// <summary>
        /// Incremented for major changes to the implementation
        /// </summary>
        public const string MAJOR_REVISION_NUM = "1";

        /// <summary>
        /// Incremented for minor changes to the implementation
        /// </summary>
        public const string MINOR_REVISION_NUM = "0";

        /// <summary>
        /// Incremented for releases containing an immediate bug fix.
        /// </summary>
        public const string BUGFIX_REVISION_NUM = "0";

        /// <summary>
        /// The value used as the user agent header name.
        /// </summary>
        public const string USER_AGENT_HEADER = "User-Agent";

        /// <summary>
        /// The release version string.
        /// </summary>
        public static readonly string RELEASE_VERSION = $"{VERSION_NUM}.{MAJOR_REVISION_NUM}.{MINOR_REVISION_NUM}.{BUGFIX_REVISION_NUM}";

        /// <summary>
        /// The user agent string that will be appended to the SDK user agent string
        /// </summary>
        public static readonly string USER_AGENT_STRING = $"AwsSecretCache/{RELEASE_VERSION}";
    }
}
