namespace Sitecore.Support.ContentSearch.SolrProvider.Administration.SolrCommands
{
    using System.Collections.Generic;
    using SolrNet;

    /// <summary>
    /// Base class for SolrCloud commands.
    /// Any command should be derived from this class.
    /// </summary>
    public class CollectionCommand : ISolrCommand
    {
        public CollectionCommand()
        {
            Paramemters = new List<KeyValuePair<string, string>>();
        }

        public string Execute(ISolrConnection connection)
        {
            return connection.Get("/admin/collections", Paramemters.ToArray());
        }

        protected void AddParameter(string key, string value)
        {
            Paramemters.Add(new KeyValuePair<string, string>(key, value));
        }

        protected List<KeyValuePair<string, string>> Paramemters { get; set; } 
    }
}
