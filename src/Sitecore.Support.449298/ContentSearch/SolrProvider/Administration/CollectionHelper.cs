namespace Sitecore.Support.ContentSearch.SolrProvider.Administration
{
    using System.Collections.Generic;
    using System.Xml.Linq;
    using SolrNet;
    using SolrNet.Impl;
    using SolrNet.Schema;

    /// <summary>
    /// Class is helper to retrieve collection schema.
    /// Note: Currently is not in use.
    /// </summary>
    public class CollectionHelper
    {
        public static SolrSchema GetSchema(ISolrSchemaParser schemaParser, ISolrCommand getSchemaCommand, ISolrConnection connection)
        {
            return schemaParser.Parse(getSchemaCommand.Execute(connection));
        }

        public List<CoreResult> GetCollectionStatus(SolrCoreAdmin solrAdmin, ISolrCommand solrCommand,
            ISolrStatusResponseParser statusResponseParser)
        {
            var xDoc = XDocument.Parse(solrAdmin.Send(solrCommand));
            return statusResponseParser.Parse(xDoc);
        }
    }
}
