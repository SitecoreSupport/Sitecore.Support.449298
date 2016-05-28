namespace Sitecore.Support.ContentSearch.SolrProvider.Administration
{
    using SolrNet.Schema;

    public interface ISolrSchemaParser
    {
        SolrSchema Parse(string data);
    }
}