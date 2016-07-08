# Sitecore.Support.449298
Out of the box Solr integration for Sitecore 7.x, 8.0 and 8.1 supports Solr 4.x and does not work with SolrCloud that was introduced in Solr 4.10.x versions and was fully implemented in Solr 5.x. 

## Disclaimer
This patch is not an official Sitecore solution to integration with SolrCloud but rather an experimental work that aims to fill in the gap while an official solution is built.

## Main
This repo contains Sitecore patch 449298. The patch extends OOTB Solr integration with Sitecore to add basic support for SolrCloud using `SwitchOnRebuildSolrSearchIndex` index type. 
When one configures SolrCloud instance to be used with Sitecore, one needs to have a load balancer to sit in front of SolrCloud instance and configure Sitecore to talk to common end point configured in the load balancer.

## Security
There are a few options that one can use to secure Solr admin interfaces:
- Configure reverse proxy server that would control access to Solr admin URLs. It could be IIS on Windows or an http container on Linux (apache, resin, etc.)
- Use patch 438539 that implements basic authentication using SolrNet HttpWebRequestFactory.
    - If one requires Kerberos authenticaiton, consider implementing your own HttpWebRequestFactory that would work over Kerberos protocol.

## Dependencies
Sitecore Solr integration uses SolrNet assembly to work with Solr instance. This patch requires an IoC container that injects proper implementations for SolrNet interfaces. 
It depends on patch [Sitecore.Support.405677](https://github.com/SitecoreSupport/Sitecore.Support.405677).
> This patch does not work without one of IoC container integrations implemented by 405677.

## Configuration
The patch was developed with Solr 5.2.1 and tested up to Solr 5.5.1.  
This patch works only with SolrCloud instance configured to use `ClassicIndexSchemaFactory` which means `schema.xml` file must be used. Make sure `schemaFactory` configuration in `solrconfig.xml` file looks like this:
```XML
<schemaFactory class="ClassicIndexSchemaFactory"/>
```  
Main configuration settings for the patch can be found in `Sitecore.Support.449298.config` file. Here are the configuration settings that come with the patch:
- `ContentSearch.Solr.Connection.RestartWhenEstablished` sets recovery strategy when Solr connection is established. Default and recommended value: `InitialFail`.  
  Possible values:
  + `Always` Sitecore gets restarted every time when Solr connection is re-established.
  + `InitialFail` Sitecore gets restarted only if Solr connection wasn't available during initial application start. 
  + `Off` Sitecore doesn't get restarted. Solr connection may not be initialized if it was not available during initial application start.
- `ContentSearch.Solr.PropertyStoreDatabase` specifies database name to save alias-collection mappings. Default value: `core`.
- `ContentSearch.Solr.IndexingInstance` assigns the instance name of dedicated Sitecore installation for indexing operations. When empty, all indexing operations are triggered on this Sitecore instance. Default value: *`(empty)`*.
  > When dedicated indexing instance is established, all instances should have the name of dedicated indexing instance set in this setting. 
- `ContentSearch.Solr.OptimizeOnRebuild.Enabled` if enabled, runs index optimization command once rebuild is completed. Default and recommended value: `false`.
- `ContentSearch.Solr.ReadOnlyStrategy` specifies strategy that is used for read only indexes. Default value: `contentSearch/indexUpdateStrategies/manual`.
- `ContentSearch.Solr.EnforceAliasCreation` if enabled, index aliases will be created on Solr side during index initialization process. Default value: `true`.
  
The patch uses a scheduled task that checks whether the Solr instance up and running and can react when it comes back online according to the behavior set by `ContentSearch.Solr.Connection.RestartWhenEstablished` setting.  

Other configuration files `Sitecore.Support.449298.SwitchOnRebuild.IndexConfig.example` and `Sitecore.Support.449298.SwitchOnRebuild.IndexConfig.ReuseCollection.config.example` are provide as examples how to configure SwitchOnRebuild Solr index. Explore these files to understand index configuration changes that need to be implemented to patch index definitions. 

## Content
The patch includes the following files:  
- `bin/Sitecore.Support.449298.dll`
- `App_Config/Include/Sitecore.Support.449298/Sitecore.Support.449298.config`
- `App_Config/Include/Sitecore.Support.449298/Sitecore.Support.449298.SwitchOnRebuild.IndexConfig.example`
- `App_Config/Include/Sitecore.Support.449298/Sitecore.Support.449298.SwitchOnRebuild.IndexConfig.ReuseCollection.config.example`