namespace Sitecore.Support.ContentSearch.SolrProvider.Administration
{
  using Diagnostics;
  using Sitecore.ContentSearch.SolrProvider;
  using SolrNet;
  using SolrNet.Exceptions;

  public static class SolrStatus
    {
        static SolrStatus()
        {
            InitStatusOk = OkSolrStatus();
        }
        public static bool InitStatusOk { get; }

        public static bool OkSolrStatus()
        {
            try
            {
                ISolrCoreAdmin solrAdmin = SolrContentSearchManager.SolrAdmin;
                if (solrAdmin != null)
                {
                    var list = solrAdmin.Status();
                }
                return true;
            }
            catch (SolrConnectionException solrException)
            {
                Log.Warn(
                    $"SUPPORT : Unable to connect to Solr: [{SolrContentSearchManager.ServiceAddress}], " +
                    $"the [{typeof (SolrConnectionException).FullName}] was caught.",
                    solrException, new object());
                return false;
            }
        }
    }
}
