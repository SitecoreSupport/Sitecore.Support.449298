namespace Sitecore.Support.ContentSearch.SolrProvider
{
    using Sitecore.ContentSearch.Diagnostics;
    using Sitecore.Data;
    using Sitecore.Diagnostics;
    using Sitecore.Support.ContentSearch.SolrProvider.Configuration;

    /// <summary>
    /// This class was added to work around the issue when DatabasePropertyStore could be redirected to the file system due to the issue #417664.
    /// See KB article: https://kb.sitecore.net/articles/930920
    /// With cetralized indexing server the indexing properties should be maintained in a common data source to allow all instances to share them.
    /// </summary>
    public class DatabasePropertyStore
    {
        public string Key { get; set; }

        // NOTE: since this property uses InstanceName, it's mandatory to have only a single server that performs indexing operations.
        // Otherwise, other instances may reindex the same data.
        /// <summary>
        /// Gets the key constructed of Key and InstanceName properties.
        /// </summary>
        public string MasterKey => $"{this.Key}_{Settings.IndexingInstance}";

        public Database Database { get; set; }

        public DatabasePropertyStore(string key, Database database)
        {
            Assert.IsNotNullOrEmpty(key, "propertyKey");
            Assert.IsNotNull(database, "database");
            Key = key;
            Database = database;
        }

        public void Set(string key, string value)
        {
            Database.Properties[GetFullKey(key)] = value;
            CrawlingLog.Log.Debug(
                $"SwitchOnRebuildSolrSearchIndex: Setting database property '{GetFullKey(key)}'='{value}'");
        }

        public string Get(string key)
        {
            return Database.Properties[GetFullKey(key)];
        }

        public void Clear(string key)
        {
            Database.Properties.RemovePrefix(GetFullKey(key));
        }

        public void ClearAll()
        {
            Database.Properties.RemovePrefix(Key);
        }

        protected string GetFullKey(string key)
        {
            return $"{MasterKey}_{key}";
        }
    }
}
