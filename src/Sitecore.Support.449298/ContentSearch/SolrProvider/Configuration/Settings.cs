namespace Sitecore.Support.ContentSearch.SolrProvider.Configuration
{
  public class Settings
    {
        public static string ConnectionRestartType => Sitecore.Configuration.Settings.GetSetting("ContentSearch.Solr.Connection.RestartWhenEstablished", "InitialFail");

        public static string PropertyStoreDatabase => Sitecore.Configuration.Settings.GetSetting("ContentSearch.Solr.PropertyStoreDatabase", "core");

        public static string IndexingInstance => StringUtil.GetString(new string[]
        {
            Sitecore.Configuration.Settings.GetSetting("ContentSearch.Solr.IndexingInstance"),
            Sitecore.Configuration.Settings.InstanceName
        });

        public static bool OptimizeOnRebuildEnabled => Sitecore.Configuration.Settings.GetBoolSetting("ContentSearch.Solr.OptimizeOnRebuild.Enabled", false);

        public static string ReadOnlyStrategyPath => Sitecore.Configuration.Settings.GetSetting("ContentSearch.Solr.ReadOnlyStrategy", "contentSearch/indexUpdateStrategies/manual");

        public static bool EnforceAliasCreation => Sitecore.Configuration.Settings.GetBoolSetting("ContentSearch.Solr.EnforceAliasCreation", false);
    }
}
