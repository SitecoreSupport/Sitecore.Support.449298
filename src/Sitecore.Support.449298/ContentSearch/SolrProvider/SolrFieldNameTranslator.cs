namespace Sitecore.Support.ContentSearch.SolrProvider
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    public class SolrFieldNameTranslator : Sitecore.ContentSearch.SolrProvider.SolrFieldNameTranslator
    {
        private readonly ConcurrentDictionary<string, IEnumerable<string>> typeFieldMap;

        public SolrFieldNameTranslator(Sitecore.ContentSearch.SolrProvider.SolrSearchIndex solrSearchIndex) : base(solrSearchIndex)
        {
            this.typeFieldMap = new ConcurrentDictionary<string, IEnumerable<string>>();
        }

        public override IEnumerable<string> GetTypeFieldNames(string fieldName)
        {
            Func<string, IEnumerable<string>> valueFactory = delegate (string key)
            {
                List<string> list = new List<string>();
                string item = this.StripKnownExtensions(fieldName);
                if (item != fieldName)
                {
                    list.Add(item);
                    return list;
                }
                fieldName = item;
                if (!fieldName.StartsWith("_", StringComparison.Ordinal))
                {
                    list.Add(fieldName.Replace("_", " ").Trim());
                    return list;
                }
                list.Add(fieldName);
                return list;
            };
            return this.typeFieldMap.GetOrAdd(fieldName, valueFactory);
        }
    }
}
