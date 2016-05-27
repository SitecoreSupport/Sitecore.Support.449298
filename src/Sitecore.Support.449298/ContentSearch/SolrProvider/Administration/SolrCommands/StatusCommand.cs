namespace Sitecore.Support.ContentSearch.SolrProvider.Administration.SolrCommands
{
    using System;
    using SolrNet.Commands;

    // This class isn't used as Summary property for SolrSearchIndex should be reworked to
    // account for situations when Solr collection has > 1 shard or contains > 1 Sitecore indexes.
    // IndexingManager UI in such situations would show false information.
    // As a result it was decided to let Summary function fail with an error message in Sitecore log rather than show incorrect data.

    /// <summary>
    /// Class represents STATUS command for SolrCloud collection API.
    /// </summary>
    /// <example>
    /// /admin/cores?action=STATUS&collection=scitems
    /// </example>
    public class StatusCommand : CoreCommand
    {
        public StatusCommand(string collection)
        {
            if (string.IsNullOrEmpty(collection))
            {
                throw new ArgumentException("Collection name must be specified.", nameof(collection));
            }
            base.AddParameter("action", "STATUS");
            base.AddParameter("collection", collection);
        }
    }
}
