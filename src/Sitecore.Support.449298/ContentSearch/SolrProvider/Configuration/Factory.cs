namespace Sitecore.Support.ContentSearch.SolrProvider.Configuration
{
    using System;
    using Data.DataProviders;
    using Sitecore.Support.ContentSearch.SolrProvider.Configuration;

    public static class Factory
    {
        private static object syncRoot;
        private static IRetryable retryer;

        static Factory()
        {
            syncRoot = new object();
        }

        public static IRetryable GetSolrRetryer()
        {
            if (Factory.retryer != null)
            {
                ICloneable retryer = Factory.retryer as ICloneable;
                if (retryer != null)
                {
                    return (IRetryable) retryer.Clone();
                }
            }
            lock (syncRoot)
            {
                if (Factory.retryer == null)
                {
                    Factory.retryer = new Retryer(Settings.RetryInterval, Settings.RetryWhenFailedAtInitialization, Settings.RetryerExtendedLog);
                }
                ICloneable cloneable = Factory.retryer as ICloneable;
                if (cloneable != null)
                {
                    return (IRetryable) cloneable.Clone();
                }
                return Factory.retryer;
            }
        }
    }
}
