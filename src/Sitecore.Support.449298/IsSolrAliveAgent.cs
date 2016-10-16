namespace Sitecore.Support
{
    using Sitecore.Tasks;
    using ContentSearch.SolrProvider.Administration;
    using Sitecore.ContentSearch.SolrProvider;
    using System.Collections.Generic;
    using System;

    // Got this class from one of support ticket on issue 391039.
    public class IsSolrAliveAgent : BaseAgent
    {
        public void Run()

        {
            if (SolrStatus.IndexListForReinitialization.Count <= 0)
            {
                Trace.Info("IsSolrAliveAgent: No indexes are pending for re-initialization. Terminating execution");
                return;
            }

            Trace.Info($"IsSolrAliveAgent: {SolrStatus.IndexListForReinitialization.Count} SOLR indexes are pending for re-initialization...");
            Trace.Info("IsSolrAliveAgent: probing for current SOLR satatus...");
            bool solrOK = SolrStatus.OkSolrStatus();
            Trace.Info($" > '{(solrOK ? "OK" : "UNAVAILABLE")}'");
            if (!solrOK)
            {
                Trace.Info("  > Quitting");
                return;
            }

            Trace.Info(" > Attempting index re-initialization");
            var reinitializedIndexes = new List<SolrSearchIndex>();
            // Attempting re-initialization for pending indexes
            foreach (var index in SolrStatus.IndexListForReinitialization)
            {
                try
                {
                    Trace.Info($"  - Re-initializing index '{index.Name}' ...");
                    index.Initialize();
                    Trace.Info("     ~ DONE");
                    reinitializedIndexes.Add(index);
                }
                catch (Exception ex)
                {
                    Trace.Warn("     ~ FAILED", ex);
                }
            }

            // Reviewing list of pending indexes
            foreach (var index in reinitializedIndexes)
            {
                Trace.Info($"IsSolrAliveAgent: Un-registering {index.Name} index after successfull re-initialization...");

                SolrStatus.IndexListForReinitialization.Remove(index);
                Trace.Info($"IsSolrAliveAgent: DONE");
            }
        }

    }
}
