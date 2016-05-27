# Sitecore.Support.449298
Out of the box Solr integration for Sitecore 7.x, 8.0 and 8.1 supports Solr 4.x and does not work with SolrCloud that was introduced in Solr 4.10.x versions and was fully implemented in Solr 5.x. 

## Main
This repo contains Sitecore patch 449298. The patch extends OOTB Solr integration with Sitecore to add basic support for SolrCloud. 
When one configures SolrCloud instance to be used with Sitecore, one needs to have a load balancer to sit in front of SolrCloud instance and configure Sitecore to talk to common end point configured in the load balancer.

## Security
There are a few options that one can use to secure Solr admin interfaces:
- Configure reverse proxy server that would control access to Solr admin URLs. It could be IIS on Windows or an http container on Linux (apache, resin, etc.)
- Use patch 438539 that implements basic authentication using SolrNet HttpWebRequestFactory.
    - If one requires Kerberos authenticaiton, consider implementing your own HttpWebRequestFactory that would work over Kerberos protocol.

## Dependencies
Sitecore Solr integration uses SolrNet assembly to work with Solr instance. This patch requires an IoC container that injects proper implementations for SolrNet interfaces. 
It depends on patch 405677.

## Configuration
The patch was developed with Solr 5.2.1 and tested up to Solr 5.5.1.  
This patch works only with SolrCloud instance configured to use `ClassicIndexSchemaFactory` which means `schema.xml` file must be used. Make sure `schemaFactory` configuration in `solrconfig.xml` file looks like this:
```
<schemaFactory class="ClassicIndexSchemaFactory"/>
```  


## Deployment

## Content
The patch includes the following files:
1. `one`
2. `two`