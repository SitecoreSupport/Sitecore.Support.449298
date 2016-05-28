namespace Sitecore.Support.ContentSearch.SolrProvider.Administration
{
    using System;
    using Newtonsoft.Json.Linq;
    using SolrNet.Schema;
    using System.Linq;
    using Sitecore.Diagnostics;
    using SolrNet.Exceptions;
    using System.Collections.Generic;

    /// <summary>
    /// Class represents Solr schema parser for JSON format.
    /// NOTE: Currently is not in use.
    /// </summary>
    public class JsonSchemaParser : ISolrSchemaParser
    {
        public SolrSchema Parse(string data)
        {
            JObject jData = JObject.Parse(data);
            JToken jSchema = jData["schema"];
            SolrSchema schema = new SolrSchema();
            
            // Parsing field types
            var types = jSchema["types"] ?? jSchema["fieldTypes"];
            Assert.IsNotNull(types, "fieldTypes");
            schema.SolrFieldTypes.AddRange(ParseFieldTypes(types));

            // Parsing fields
            var fields = jSchema["fields"];
            Assert.IsNotNull(fields, "fields");
            schema.SolrFields.AddRange(ParseFields(fields, schema));

            // Parsing dynamic fields
            var dynamicFields = jSchema["dynamicFields"];
            if (dynamicFields != null && dynamicFields.Any())
            {
                schema.SolrDynamicFields.AddRange(ParseDynamicFields(dynamicFields, schema));
            }

            // Parsing dynamic fields
            var copyFields = jSchema["copyFields"];
            if (copyFields != null && copyFields.Any())
            {
                schema.SolrCopyFields.AddRange(ParseCopyFields(copyFields, schema));
            }

            var uniqueKey = jSchema.Value<string>("uniqueKey");
            if (!string.IsNullOrEmpty(uniqueKey))
            {
                schema.UniqueKey = uniqueKey;
            }

            return schema;
        }

        protected virtual IEnumerable<SolrCopyField> ParseCopyFields(JToken fields, SolrSchema schema)
        {
            return fields.Children().Select(jToken => new SolrCopyField(jToken.Value<string>("source"), jToken.Value<string>("dest")));
        }

        protected virtual IEnumerable<SolrDynamicField> ParseDynamicFields(JToken fields, SolrSchema schema)
        {
            return fields.Children().Select(jToken => new SolrDynamicField(jToken.Value<string>("name")));
        }

        protected virtual IEnumerable<SolrField> ParseFields(JToken fields, SolrSchema solrSchema)
        {
            var solrFields = new List<SolrField>();
            foreach (var field in fields)
            {
                var type = field.Value<string>("type");
                var fieldType = solrSchema.FindSolrFieldTypeByName(type);
                if (fieldType == null)
                {
                    throw new SolrNetException($"Field type '{type}' not found");
                }
                var solrField = new SolrField(field.Value<string>("name"), fieldType)
                {
                    IsRequired =
                        !string.IsNullOrEmpty(field.Value<string>("required")) &&
                        field.Value<string>("required").ToLower().Equals(bool.TrueString, StringComparison.InvariantCultureIgnoreCase),
                    IsMultiValued = !string.IsNullOrEmpty(field.Value<string>("multiValued")) &&
                        field.Value<string>("multiValued").ToLower().Equals(bool.TrueString, StringComparison.InvariantCultureIgnoreCase),
                };
                solrFields.Add(solrField);
            }
            return solrFields;
        }

        protected virtual IEnumerable<SolrFieldType> ParseFieldTypes(JToken types)
        {
            return types.Children().Select(jToken => new SolrFieldType(jToken.Value<string>("name"), jToken.Value<string>("class"))).ToList();
        }
    }
}
