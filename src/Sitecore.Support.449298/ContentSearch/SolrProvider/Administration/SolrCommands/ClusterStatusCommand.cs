namespace Sitecore.Support.ContentSearch.SolrProvider.Administration.SolrCommands
{
    /// <summary>
    /// Class represents CLUSTERSTATUS command for SolrCloud collections API.
    /// </summary>
    public class ClusterStatusCommand : CollectionCommand
    {
        public ClusterStatusCommand()
        {
            AddParameter("action", "CLUSTERSTATUS");
        }
    }
}
