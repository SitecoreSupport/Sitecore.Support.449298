namespace Sitecore.Support.ContentSearch.SolrProvider.Administration.SolrCommands
{
    using System;

    /// <summary>
    /// Class represents CREATEALIAS command for SolrCloud collections API.
    /// The command can be used to create a new or modify existing alias.
    /// </summary>
    public class CreateAliasCommand : CollectionCommand
    {
        public CreateAliasCommand(string aliasName, string cores)
        {
            if (string.IsNullOrEmpty(aliasName))
            {
                throw new ArgumentException("Alias name must be specified.", nameof(aliasName));
            }
            if (string.IsNullOrEmpty(cores))
            {
                throw new ArgumentException("At least one core must be specified", nameof(cores));
            }
            base.AddParameter("action", "CREATEALIAS"); 
            base.AddParameter("name", aliasName);
            base.AddParameter("collections", cores);
        }
    }

}
