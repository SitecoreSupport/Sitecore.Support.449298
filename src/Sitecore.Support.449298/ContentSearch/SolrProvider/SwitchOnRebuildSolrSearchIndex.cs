namespace Sitecore.Support.ContentSearch.SolrProvider
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using Administration;
    using Microsoft.Practices.ServiceLocation;
    using Sitecore.ContentSearch;
    using Sitecore.ContentSearch.Diagnostics;
    using Sitecore.ContentSearch.Maintenance;
    using Sitecore.ContentSearch.Maintenance.Strategies;
    using Sitecore.ContentSearch.SolrProvider;
    using Sitecore.ContentSearch.Utilities;
    using Sitecore.Diagnostics;
    using Sitecore.Exceptions;
    using Sitecore.Support.ContentSearch.SolrProvider.Administration.SolrCommands;
    using Sitecore.Support.ContentSearch.SolrProvider.Configuration;
    using SolrNet;
    using SolrNet.Impl;
    using Trace = Sitecore.Support.Trace;

    // NOTE: you must inherit your custom SwitchOnRebuildSolrSearchIndex from default Sitecore.ContentSearch.SolrProvider.SwitchOnRebuildSolrSearchIndex class
    // as SolrContentSearchManager.Cores property casts indexes to default implementations of SolrSearchIndex and SwitchOnRebuildSolrSearchIndex.
    public class SwitchOnRebuildSolrSearchIndex : Sitecore.ContentSearch.SolrProvider.SwitchOnRebuildSolrSearchIndex
    {
        // Fields
        private ISolrOperations<Dictionary<string, object>> rebuildSolrOperations;

        // Properties
        #region Properties

        /// <summary>
        /// Active Solr mainalias name. Previously it was set as the Core property.
        /// </summary>
        public string ActiveCollection { get; set; }
        
        /// <summary>
        /// Rebuid Solr mainalias name. Previously it was set as the RebuildCollection property.
        /// </summary>
        public string RebuildCollection { get; set; }

        /// <summary>
        /// Gets or sets database property store for the index.
        /// </summary>
        public DatabasePropertyStore DatabasePropertyStore { get; set; }

        #endregion Properties

        public SwitchOnRebuildSolrSearchIndex(string name, string mainalias, string rebuildalias, IIndexPropertyStore propertyStore)
            : this(name, mainalias, rebuildalias, mainalias, rebuildalias, propertyStore)
        {
        }

        public SwitchOnRebuildSolrSearchIndex(string name, string mainalias, string rebuildalias, string activecollection, string rebuildcollection, IIndexPropertyStore propertyStore)
            : base(name, mainalias, rebuildalias, propertyStore)
        {
            ActiveCollection = activecollection;
            RebuildCollection = rebuildcollection;
            if (string.Empty != Settings.PropertyStoreDatabase)
            {
                var database = Sitecore.Configuration.Factory.GetDatabase(Settings.PropertyStoreDatabase);
                DatabasePropertyStore = new DatabasePropertyStore(base.Name, database);
            }
            else
            {
                DatabasePropertyStore = null;
            }
        }

        private IProviderUpdateContext CreateRebuildUpdateContext(ISolrOperations<Dictionary<string, object>> solrOperations)
        {
            ICommitPolicyExecutor commitPolicyExecutor = (ICommitPolicyExecutor)this.CommitPolicyExecutor.Clone();
            commitPolicyExecutor.Initialize(this);
            IContentSearchConfigurationSettings instance = this.Locator.GetInstance<IContentSearchConfigurationSettings>();
            if (instance.IndexingBatchModeEnabled())
            {
                return new SolrBatchUpdateContext(this, solrOperations, instance.IndexingBatchSize(), commitPolicyExecutor);
            }
            return new SolrUpdateContext(this, solrOperations, this.CommitPolicyExecutor);
        }

#region override

        public override void Initialize()
        {
            try
            {
                // Loads ActiveCollection and RebuildCollection from the DatabasePropertyStore.
                LoadLastPreservedCoreStates();
                // Adjust aliases configuration only if instance is configured to run indexing operations.
                if (Sitecore.Configuration.Settings.InstanceName.Equals(Settings.IndexingInstance,
                    StringComparison.InvariantCultureIgnoreCase))
                {
                    CrawlingLog.Log.Info(
                        $"The instance is configured to run indexing operations. Indexing instance name: {Settings.IndexingInstance}");
                    if (Settings.EnforceAliasCreation)
                    {
                        // Set aliases to proper collections
                        SetAliasesConfiguration();
                    }
                }
                else
                {
                    CrawlingLog.Log.Info(
                        $"Instance is configured in index read-only mode. Current indexing instance: {Settings.IndexingInstance}");
                }
                // Call base class initialization.
                base.Initialize();
                // Use custom SolrFieldNameTranslator from patch 426716
                base.FieldNameTranslator = new SolrFieldNameTranslator(this);
                // Trying to set index.schema field using CollectionsAPI.
                // NOTE: curretnly this is not necessary as requesting collection schema thru alias works fine as long as collection exists in Solr.
                //SetIndexSchema();
                try
                {
                    this.rebuildSolrOperations =
                        ServiceLocator.Current.GetInstance<ISolrOperations<Dictionary<string, object>>>(this.RebuildCore);
                }
                catch (Exception exception)
                {
                    Log.Error($"Unable to access rebuild alias [{this.RebuildCore}]", this);
                    throw new ProviderConfigurationException(
                        $"You have selected a Solr SwitchOnRebuild index. Unable to access rebuild collection [{RebuildCollection}] via rebuild alias [{this.RebuildCore}] please configure before continuing.",
                        exception);
                }
                CrawlingLog.Log.Debug(
                    $"[Index={this.Name}] Created access to rebuild collection [{RebuildCollection}] via rebuild alias [{this.RebuildCore}]",
                    null);
            }
            catch (ProviderConfigurationException ex) {
                // Re-throw ProviderConfigurationException ( there's no point in re-initializing index )
                throw ex;
            }
            catch (Exception ex)
            {
                Trace.Warn($"Failed to initialize '{this.Name}' index. Registering the index for re-initialization once connection to SOLR becomes available ...");
                SolrStatus.RegisterIndexForReinitialization(this);
                Trace.Warn("DONE");
                Log.Error(ex.Message, ex, this);
            }
        }

        public override void Rebuild(bool resetIndex = true, bool optimizeOnComplete = true)
        {
            if (Sitecore.Configuration.Settings.InstanceName == Settings.IndexingInstance)
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                // Clear collection before populating it.
                if (resetIndex)
                {
                    this.rebuildSolrOperations =
                        ServiceLocator.Current.GetInstance<ISolrOperations<Dictionary<string, object>>>(
                            RebuildCollection);
                    this.Reset();
                }
                using (IProviderUpdateContext context = this.CreateRebuildUpdateContext(this.rebuildSolrOperations))
                {
                    foreach (IProviderCrawler crawler in base.Crawlers)
                    {
                        crawler.RebuildFromRoot(context, IndexingOptions.Default, CancellationToken.None);
                    }
                    context.Commit();
                }
                if (optimizeOnComplete && Settings.OptimizeOnRebuildEnabled)
                {
                    CrawlingLog.Log.Debug(
                        $"[Index={this.Name}] Optimizing collection [Collection: {RebuildCollection}]", null);
                    this.rebuildSolrOperations.Optimize();
                }
                stopwatch.Stop();
                this.PropertyStore.Set(IndexProperties.RebuildTime,
                stopwatch.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture));
                SwitchAliasesCollections();
                SetAliasesConfiguration();
                PreserveAliasesCollections();
                // Clear old collection.
                // NOTE: clearing old collection after it's rebuild might be overengineering.
                // However, if the indexes are big, it may save some disk space.
                // Keep in mind disk space must be large enough to hold double of the current index size as both collections need to hold data while index is being rebuilt.
                //if (resetIndex)
                //{
                //    this.Reset();
                //}
            }
            else
            {
                throw new Sitecore.Exceptions.ConfigurationException("Index rebuild action canont be executed. This instance is configured in index read-only mode.");
            }
        }

        public override void Reset()
        {
            CrawlingLog.Log.Debug($"[Index={this.Name}] Resetting index records [Alias: {this.RebuildCore}, Core: {RebuildCollection}]", null);
            SolrQueryByField q = new SolrQueryByField("_indexname", this.Name);
            this.rebuildSolrOperations.Delete(q);
            this.rebuildSolrOperations.Commit();
        }

        // This method needs to be overriden in order to set strategy for non-indexing instances.
        // This is necessary to make config files consistent to simplify deployments.
        // NOTE: there might be some indexes that reqire indexing capabilities on all instances. This approach forces all indexes to use predefined index strategy.
        public override void AddStrategy(Sitecore.ContentSearch.Maintenance.Strategies.IIndexUpdateStrategy strategy)
        {
            if (!Sitecore.Configuration.Settings.InstanceName.Equals(Settings.IndexingInstance,
                StringComparison.InvariantCultureIgnoreCase))
            {
                var readOnlyStrategy = GetReadOnlyStrategy();
                if (readOnlyStrategy != null)
                {
                    strategy = readOnlyStrategy;
                    CrawlingLog.Log.Debug(
                        $"Index '{this.Name}' strategy is set to '{Settings.ReadOnlyStrategyPath}' strategy.");
                }
            }
            base.AddStrategy(strategy);
        }

        // This method is necessary for IndexingManager UI.
        // This method designed to return collection summary rather than index summary. 
        // It fails if collection consists of > 1 shards or collection has > 1 Sitecore indexes.
        // NOTE: Without this method 'Object null reference' exception will be logged in Sitecore log file.
        /// <summary>
        /// Returns collection status.
        /// </summary>
        //public override ISearchIndexSummary Summary
        //{
        //    get
        //    {
        //        var solrAdmin = SolrContentSearchManager.SolrAdmin as SolrCoreAdmin;
        //        // This line will fail if number of shards > 1. 
        //        // In SolrCloud each index may have more than 1 shard. If that's the case, then this code is not valid.
        //        // If any solr collection contains > 1 Sitecore index, then Summary cannot be used as it was used for Solr core when match is 1-to-1.
        //        var collectionStatus = new CollectionHelper().GetCollectionStatus(solrAdmin,
        //            new StatusCommand(this.ActiveCollection), new SolrStatusResponseParser()).Single();
        //        //if (coreAdmin)
        //        var summary = new SolrIndexSummary(collectionStatus, this);
        //        return summary;
        //    }
        //}

        #endregion override

        /// <summary>
        /// Returns index strategy configured thru "ContentSearch.Solr.ReadOnlyStrategy" setting.
        /// </summary>
        /// <returns></returns>
        private IIndexUpdateStrategy GetReadOnlyStrategy()
        {
            var strategy = Sitecore.Configuration.Factory.CreateObject(Settings.ReadOnlyStrategyPath, true) as IIndexUpdateStrategy;
            return strategy;
        }

        /// <summary>
        /// Sets main alias to the active collection and rebuild alias to the rebuild collection.
        /// </summary>
        private void SetAliasesConfiguration()
        {
            // Set primary alias (Core property value) to last preserved active collection.
            SetAlias(Core, ActiveCollection);
            // Set rebuild alias (RebuildCollection property value) to last preserved rebuild collection.
            SetAlias(RebuildCore, RebuildCollection);
        }

        /// <summary>
        /// Writes main alias and rebuild alias values to the index property store.
        /// </summary>
        private void PreserveAliasesCollections()
        {
            // Have to use specified database since a file based property store could be used: https://kb.sitecore.net/articles/930920
            // Ticket #417664
            if (null != DatabasePropertyStore)
            {
                this.DatabasePropertyStore.Set(SolrIndexProperties.RebuildCollection, RebuildCollection);
                this.DatabasePropertyStore.Set(SolrIndexProperties.ActiveCollection, ActiveCollection);
            }
            else
            {
                this.PropertyStore.Set(SolrIndexProperties.RebuildCollection, RebuildCollection);
                this.PropertyStore.Set(SolrIndexProperties.ActiveCollection, ActiveCollection);                
            }
        }

        /// <summary>
        /// Swaps aliases collections of main and rebuild aliases.
        /// </summary>
        private void SwitchAliasesCollections()
        {
            var activeCore = ActiveCollection.Clone().ToString();
            ActiveCollection = RebuildCollection;
            RebuildCollection = activeCore;
            CrawlingLog.Log.Debug(
                $"SwitchOnRebuildSolrSearchIndex: SwitchAliasesCollections: AliaseActiveCore switched to '{ActiveCollection}' and RebuildCollection switched to '{RebuildCollection}'.");
        }

        // It is not required to set index schema as it can be requested thru an alias.
        // Though, it may work only if alias points to a single collection.
        /// <summary>
        /// Sets index.schema field.
        /// </summary>
        //protected void SetIndexSchema()
        //{
        //    var schema = CollectionHelper.GetSchema(new JsonSchemaParser(), new GetSchemaCommand(ActiveCollection), new SolrConnection(SolrContentSearchManager.ServiceAddress) {Timeout = Settings.ConnectionTimeout});
        //    OverwriteMember(this, "schema", new SolrIndexSchema(schema));
        //}

        /// <summary>
        /// Loads last states of active and rebuild collections from property store.
        /// </summary>
        protected void LoadLastPreservedCoreStates()
        {
            var lastActiveCollection = null != this.DatabasePropertyStore ? this.DatabasePropertyStore.Get(SolrIndexProperties.ActiveCollection) : this.PropertyStore.Get(SolrIndexProperties.ActiveCollection);
            if (!string.IsNullOrEmpty(lastActiveCollection))
            {
                ActiveCollection = lastActiveCollection;
                CrawlingLog.Log.Debug($"SwitchOnRebuildSolrSearchIndex: ActiveCollection set to '{lastActiveCollection}'");
            }
            var lastRebuildCollection = null != this.DatabasePropertyStore ? this.DatabasePropertyStore.Get(SolrIndexProperties.RebuildCollection) : this.PropertyStore.Get(SolrIndexProperties.RebuildCollection);
            if (!string.IsNullOrEmpty(lastRebuildCollection))
            {
                RebuildCollection = lastRebuildCollection;
                CrawlingLog.Log.Debug(
                    $"SwitchOnRebuildSolrSearchIndex: RebuildCollection set to '{lastRebuildCollection}'");
            }
        }

        /// <summary>
        /// Sets alias to a specified collection.
        /// </summary>
        /// <param name="aliasName">Alias name</param>
        /// <param name="collection">Collection name</param>
        protected virtual void SetAlias(string aliasName, string collection)
        {
            var response = CreateAlias(aliasName, collection);
            if (0 == response.Status)
            {
                CrawlingLog.Log.Debug(
                    $"SwitchOnRebuildSolrSearchIndex: CreateAlias: Alias '{aliasName}' created/modified with collection '{collection}'");
            }
            else
            {
                CrawlingLog.Log.Error(
                    $"SwitchOnRebuildSolrSearchIndex: CreateAlias: Setting alias '{aliasName}' to collection '{collection}' failed. Response body: {response}");
            }

        }

        /// <summary>
        /// Runs CREATEALIAS action for Solr collections REST API.
        /// </summary>
        /// <param name="aliasName">Alias name</param>
        /// <param name="collection">Collection(s) name. Multiple collections should be separated by comma.</param>
        /// <returns></returns>
        protected ResponseHeader CreateAlias(string aliasName, string collection)
        {
            var solrAdmin = SolrContentSearchManager.SolrAdmin as SolrCoreAdmin;
            var response = new ResponseHeader {Status = -1};
            if (null != solrAdmin)
            {
                response = solrAdmin.SendAndParseHeader(new CreateAliasCommand(aliasName, collection));
            }
            return response;
        }

        // This method was added to overwrite schema property. 
        // However, it was found to be unnecessary.
        //protected void OverwriteMember(object obj, string memberName, object value)
        //{
        //    var member = FindMember(obj.GetType(), memberName);
        //    if (null != member)
        //    {
        //        switch (member.MemberType)
        //        {
        //            case MemberTypes.Property:
        //                var propertyInfo = member.DeclaringType?.GetProperty(memberName);
        //                propertyInfo?.SetValue(obj, value);
        //                break;
        //            case MemberTypes.Field:
        //                var fieldInfo = member.DeclaringType?.GetField(memberName);
        //                fieldInfo?.SetValue(obj, value);
        //                break;
        //        }
        //    }
        //    //obj.GetType().BaseType.BaseType.GetProperty(memberName).SetValue(obj, value);
        //}

        // This method comes along with OverwriteMember and considered to be obsolete.
        //private MemberInfo FindMember(Type objType, string memberName, BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance)
        //{
        //    var member = objType.GetMember(memberName, bindingFlags).FirstOrDefault();
        //    if (null == member && typeof(System.Object) != objType.BaseType)
        //    {
        //        member = FindMember(objType.BaseType, memberName, bindingFlags);
        //    }
        //    return member;
        //}
    }
}
