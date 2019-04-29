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
