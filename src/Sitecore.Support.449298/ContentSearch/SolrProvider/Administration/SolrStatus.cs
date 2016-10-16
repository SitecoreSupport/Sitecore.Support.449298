namespace Sitecore.Support.ContentSearch.SolrProvider.Administration
{
  using Sitecore.ContentSearch.SolrProvider;
  using SolrNet;
  using SolrNet.Exceptions;
    using System.Collections.Generic;

  public static class SolrStatus
    {
        static SolrStatus()
        {
            InitStatusOk = OkSolrStatus();
            IndexListForReinitialization = new List<SolrSearchIndex>();
        }
        public static bool InitStatusOk { get; }
        public static void RegisterIndexForReinitialization(Sitecore.ContentSearch.SolrProvider.SolrSearchIndex solrIndex)
        {
            if (solrIndex != null)
            {
                if (!IndexListForReinitialization.Contains(solrIndex))
                {
                    IndexListForReinitialization.Add(solrIndex);
                }
                else
                {
                    Trace.Warn($"Index re-initialization list already contains '{solrIndex.Name}' index. Skipping this operation to avoid duplicating the entry.");
                }
            }
        }
        public static List<SolrSearchIndex> IndexListForReinitialization { get; private set; }

        public static bool OkSolrStatus()
        {
            try
            {
                ISolrCoreAdmin solrAdmin = SolrContentSearchManager.SolrAdmin;
                if (solrAdmin != null)
                {
                    var list = solrAdmin.Status();
                }
                else
                {
                    return false; // solrAdmin is NULL - assume SOLR is not available
                }
                return true;
            }
            catch (SolrConnectionException solrException)
            {
                Trace.Warn(
                    $"SUPPORT : Unable to connect to Solr: [{SolrContentSearchManager.ServiceAddress}], " +
                    $"the [{typeof(SolrConnectionException).FullName}] was caught.",
                    solrException);
                return false;
            }
        }
    }
}
