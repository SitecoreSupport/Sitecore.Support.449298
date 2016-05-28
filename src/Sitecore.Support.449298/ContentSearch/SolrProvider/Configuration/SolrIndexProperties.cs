namespace Sitecore.Support.ContentSearch.SolrProvider.Configuration
{
    public static class SolrIndexProperties
    {
        /// <summary>
        /// Property returns data store property postfix for active collection 
        /// </summary>
        public static string ActiveCollection => "solr_active_collection";

        /// <summary>
        /// Property returns data store property postfix for rebuild collection 
        /// </summary>
        public static string RebuildCollection => "solr_rebuild_collection";
    }
}
