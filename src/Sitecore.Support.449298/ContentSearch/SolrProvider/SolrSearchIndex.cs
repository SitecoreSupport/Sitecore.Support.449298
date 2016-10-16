using System;

namespace Sitecore.Support.ContentSearch.SolrProvider
{
    using Sitecore.ContentSearch.Maintenance;
    using Sitecore.Diagnostics;
    using Administration;

  /// <summary>
    /// Class was added to support SolrSearchIndex that does not block Sitecore application from starting up if Solr connection is not available.
    /// </summary>
    public class SolrSearchIndex : Sitecore.ContentSearch.SolrProvider.SolrSearchIndex
    {
        public SolrSearchIndex(string name, string core, IIndexPropertyStore propertyStore)
            : base(name, core, propertyStore)
        {
        }

        public override void Initialize()
        {
                try
                {
                    base.Initialize();
                    // Use custom SolrFieldNameTranslator from patch 426716
                    base.FieldNameTranslator = new SolrFieldNameTranslator(this);
                }
                catch (Exception exception)
                {
                Trace.Warn($"Failed to initialize '{this.Name}' index. Registering the index for re-initialization once connection to SOLR becomes available ...");
                SolrStatus.RegisterIndexForReinitialization(this);
                Trace.Warn("DONE");
                    Log.Error(exception.Message, exception, this);
                }
            }
        }
    }
