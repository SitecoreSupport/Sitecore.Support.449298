namespace Sitecore.Support.ContentSearch.SolrProvider.Administration.SolrCommands
{
    using System.Collections.Generic;
    using SolrNet;

    /// <summary>
    /// Class represents SCHEMA request from SolrCloud.
    /// It was created to allow to get XML, JSON and Schema.xml formats of SolrCloud collections.
    /// NOTE: currently is not in use.
    /// </summary>
    public class GetSchemaCommand : ISolrCommand
    {
        /// <summary>
        /// Collection name.
        /// </summary>
        public string Collection { get; set; }

        /// <summary>
        /// Schema format. Possible values: json, xml and schema.xml
        /// </summary>
        public string SchemaFormat { get; set; }

        public GetSchemaCommand(string collection) : this (collection, "json")
        {
        }

        public GetSchemaCommand(string collection, string schemaFormat)
        {
            this.Collection = collection;
            this.SchemaFormat = schemaFormat;
        }

        public string Execute(ISolrConnection connection)
        {
            var parameters = new KeyValuePair<string, string>[0];
            if (!string.IsNullOrEmpty(this.SchemaFormat))
            {
                parameters = new []{new KeyValuePair<string, string>("wt", this.SchemaFormat)};
            }
            return connection.Get($"/{this.Collection}/schema", parameters);
        }
    }
}
