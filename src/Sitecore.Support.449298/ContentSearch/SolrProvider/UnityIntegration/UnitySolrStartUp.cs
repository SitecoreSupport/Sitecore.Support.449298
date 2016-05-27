namespace Sitecore.Support.ContentSearch.SolrProvider.UnityIntegration
{
    using Microsoft.Practices.ServiceLocation;
    using Microsoft.Practices.Unity;
    using Sitecore.ContentSearch.SolrProvider;
    using Sitecore.ContentSearch.SolrProvider.DocumentSerializers;
    using Sitecore.ContentSearch.Utilities;
    using Sitecore.Diagnostics;
    using Sitecore.Support.ContentSearch.SolrProvider.Configuration;
    using SolrNet;
    using SolrNet.Impl;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Unity.SolrNetIntegration;
    using Unity.SolrNetIntegration.Config;

    public class UnitySolrStartUp : ISolrStartUp
    {
        // Fields
        internal IUnityContainer Container;
        internal readonly SolrServers Cores;

        // Methods
        public UnitySolrStartUp(IUnityContainer container)
        {
            Assert.ArgumentNotNull(container, "container");
            if (SolrContentSearchManager.IsEnabled)
            {
                this.Container = container;
                this.Cores = new SolrServers();
            }
        }

        public void AddCore(string coreId, Type documentType, string coreUrl)
        {
            Assert.ArgumentNotNull(coreId, "coreId");
            Assert.ArgumentNotNull(documentType, "documentType");
            Assert.ArgumentNotNull(coreUrl, "coreUrl");
            SolrServerElement configurationElement = new SolrServerElement
            {
                Id = coreId,
                DocumentType = documentType.AssemblyQualifiedName,
                Url = coreUrl
            };
            this.Cores.Add(configurationElement);
        }

        private ISolrCoreAdmin BuildCoreAdmin()
        {
            SolrConnection connection = new SolrConnection(SolrContentSearchManager.ServiceAddress);
            connection.Timeout = Settings.ConnectionTimeout;
            if (SolrContentSearchManager.EnableHttpCache)
            {
                connection.Cache = this.Container.Resolve<ISolrCache>(new ResolverOverride[0]) ?? new NullCache();
            }
            return new SolrCoreAdmin(connection, this.Container.Resolve<ISolrHeaderResponseParser>(new ResolverOverride[0]), this.Container.Resolve<ISolrStatusResponseParser>(new ResolverOverride[0]));
        }

        public void Initialize()
        {
            if (!SolrContentSearchManager.IsEnabled)
            {
                throw new InvalidOperationException("Solr configuration is not enabled. Please check your settings and include files.");
            }
            // Register Solr index cores and aliases from custom SwitchOnRebuild index
            RegisterSolrServerUrls();
            //foreach (string str in SolrContentSearchManager.Cores)
            //{
            //    this.AddCore(str, typeof(Dictionary<string, object>), SolrContentSearchManager.ServiceAddress + "/" + str);
            //}
            this.Container = new SolrNetContainerConfiguration().ConfigureContainer(this.Cores, this.Container);
            //workaround to set the timeout

            foreach (SolrServerElement solrServer in this.Cores)
            {
                var coreConnectionId = solrServer.Id + (object)typeof(SolrConnection);
                this.Container.RegisterType<ISolrConnection, SolrConnection>(coreConnectionId, new InjectionMember[2]
                {
                                (InjectionMember) new InjectionConstructor(new object[1]
                                {
                                 (object) solrServer.Url
                                }),

                (InjectionMember) new InjectionProperty("Timeout", Settings.ConnectionTimeout)
                });
            }
            //end of workaround
            this.Container.RegisterType(typeof(ISolrDocumentSerializer<Dictionary<string, object>>), typeof(SolrFieldBoostingDictionarySerializer), new InjectionMember[0]);
            if (SolrContentSearchManager.EnableHttpCache)
            {
                this.Container.RegisterType(typeof(ISolrCache), typeof(HttpRuntimeCache), new InjectionMember[0]);
                List<ContainerRegistration> list = (from r in this.Container.Registrations
                                                    where r.RegisteredType == typeof(ISolrConnection)
                                                    select r).ToList<ContainerRegistration>();
                if (list.Count > 0)
                {
                    using (List<ContainerRegistration>.Enumerator enumerator2 = list.GetEnumerator())
                    {
                        while (enumerator2.MoveNext())
                        {
                            Func<SolrServerElement, bool> predicate = null;
                            ContainerRegistration registration = enumerator2.Current;
                            if (registration != null)
                            {
                                predicate = core => registration.Name == (core.Id + registration.MappedToType.FullName);
                                var element = this.Cores.FirstOrDefault<SolrServerElement>(predicate);
                                if (element == null)
                                {
                                    Log.Error("The Solr Core configuration for the '" + registration.Name + "' Unity registration could not be found. The HTTP cache for the Solr connection to the Core cannot be configured.", this);
                                }
                                else
                                {
                                    var injectionMembers = new InjectionMember[] { new InjectionConstructor(new object[] { element.Url }), new InjectionProperty("Cache", new ResolvedParameter<ISolrCache>()) };
                                    this.Container.RegisterType(typeof(ISolrConnection), typeof(SolrConnection), registration.Name, null, injectionMembers);
                                }
                            }
                        }
                    }
                }
            }
            ServiceLocator.SetLocatorProvider(() => new UnityServiceLocator(this.Container));
            //workaround to set the timeout
            SolrContentSearchManager.SolrAdmin = this.BuildCoreAdmin();
            //end of workaround
            SolrContentSearchManager.Initialize();
        }

        public bool IsSetupValid()
        {
            if (!SolrContentSearchManager.IsEnabled)
            {
                return false;
            }
            ISolrCoreAdmin admin = this.BuildCoreAdmin();
            return (from defaultIndex in SolrContentSearchManager.Cores select admin.Status(defaultIndex).First<CoreResult>()).All<CoreResult>(status => (status.Name != null));
        }

        protected void RegisterSolrServerUrls()
        {
            foreach (string str in SolrContentSearchManager.Cores)
            {
                this.AddCore(str, typeof(Dictionary<string, object>), SolrContentSearchManager.ServiceAddress + "/" + str);
            }

            foreach (var alias in Aliases)
            {
                if (!SolrContentSearchManager.Cores.Contains(alias))
                {
                    AddCore(alias, typeof(Dictionary<string, object>), SolrContentSearchManager.ServiceAddress + "/" + alias);
                }
            }

        }

        protected static IEnumerable<string> Aliases
        {
            get
            {
                List<string> source = new List<string>();
                foreach (var index in SolrContentSearchManager.Indexes.OfType<SolrProvider.SwitchOnRebuildSolrSearchIndex>())
                {
                    if (!string.IsNullOrEmpty(index.ActiveCollection))
                    {
                        source.Add(index.ActiveCollection);
                    }
                    if (!string.IsNullOrEmpty(index.RebuildCollection))
                    {
                        source.Add(index.RebuildCollection);
                    }
                }
                var aliases = source.ToHashSet();
                return aliases;
            }
        } 
    }
}