namespace Sitecore.Support
{
    using Sitecore.Diagnostics;
    using Sitecore.Support.ContentSearch.SolrProvider.Configuration;
    using Sitecore.Tasks;
    using System.Web.Hosting;
    using ContentSearch.SolrProvider.Administration;

  // Got this class from one of support ticket on issue 391039.
    public class IsSolrAliveAgent : BaseAgent
    {
        private static bool initialSolrConnectionStatus;
        private static bool lastSolrConnectionStatus;

        static IsSolrAliveAgent()
        {
            initialSolrConnectionStatus = SolrStatus.InitStatusOk;
            lastSolrConnectionStatus = initialSolrConnectionStatus;
#if DEBUG
            Log.Info($"SUPPORT:DEBUG: Initial Solr connection status = '{initialSolrConnectionStatus}'", typeof(IsSolrAliveAgent));
#endif
        }

        public IsSolrAliveAgent()
        {
        }

        protected virtual void RestartTheProcess()
        {
            HostingEnvironment.InitiateShutdown();
        }

        public void Run()
        {
            bool currentStatus = SolrStatus.OkSolrStatus();
            switch (ConnectionRestorePolicy)
            {
                case "InitialFail":
                    if (!initialSolrConnectionStatus && currentStatus)
                    {
                        StatusLogging("restart");
                        RestartTheProcess();
                    }
                    break;
                case "Always":
                    if (!lastSolrConnectionStatus && currentStatus)
                    {
                        StatusLogging("restart");
                        RestartTheProcess();
                    }
                    break;
                case "Off":
                    break;
            }
            StatusLogging(currentStatus ? "solrok" : "solrfail");
#if DEBUG
            Log.Info($"SUPPORT:DEBUG: before initialStatus update: initialStatus='{initialSolrConnectionStatus}' and currentStatus='{currentStatus}'", this);
#endif
            // never allow initial status to switch to FALSE.
            // decision is based on the assumption that nothing breaks if Solr goes down after initial successful initialization.
            initialSolrConnectionStatus |= currentStatus;
            lastSolrConnectionStatus = currentStatus;
#if DEBUG
            Log.Info($"SUPPORT:DEBUG: after initialStatus update: initialStatus='{initialSolrConnectionStatus}' and currentStatus='{currentStatus}'", this);
#endif
            LogMessage($"SolrAliveAgent: connection status is {(currentStatus ? "OK" : "FAILED")}", currentStatus ? LogNotificationLevel.Info : LogNotificationLevel.Error);
        }

        protected virtual void StatusLogging(string parameter)
        {
            if (parameter == "restart")
            {
                Log.Warn("SUPPORT: Solr connection was restored. The restart is initiated to initialize Solr provider inside <initilize> pipeline.", this);
            }
            else if ((parameter != "solrok") && (parameter == "solrfail"))
            {
                Log.Warn("SUPPORT: Solr connection failed.", this);
            }
        }

        protected virtual string ConnectionRestorePolicy => Settings.ConnectionRestartType;
    }
}
