// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigureSitecore.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2017
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Sitecore.Support.Commerce.Plugin.Sample
{
    using System.Reflection;
    using Microsoft.Extensions.DependencyInjection;
    using Sitecore.Commerce.Core;
    using Sitecore.Commerce.Plugin.Catalog;
    using Sitecore.Framework.Configuration;
    using Sitecore.Framework.Pipelines.Definitions.Extensions;
    using Sitecore.Support.Pipelines.Blocks;

    /// <summary>
    /// The configure sitecore class.
    /// </summary>
    public class ConfigureSitecore : IConfigureSitecore
    {
        /// <summary>
        /// The configure services.
        /// </summary>
        /// <param name="services">
        /// The services.
        /// </param>
        public void ConfigureServices(IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();
            services.RegisterAllPipelineBlocks(assembly);
            
            services.Sitecore().Pipelines(config => config
               .ConfigurePipeline<IGetSellableItemsConnectPipeline>(configure => configure.Replace<Sitecore.Commerce.Plugin.Catalog.GetCustomRelationshipsBlock, Sitecore.Support.Pipelines.Blocks.GetCustomRelationshipsBlock>())
               .ConfigurePipeline<IGetCategoryConnectPipeline>(configure => configure.Replace<Sitecore.Commerce.Plugin.Catalog.GetCustomRelationshipsBlock, Sitecore.Support.Pipelines.Blocks.GetCustomRelationshipsBlock>())
               .ConfigurePipeline<IGetCatalogConnectPipeline>(configure => configure.Replace<Sitecore.Commerce.Plugin.Catalog.GetCustomRelationshipsBlock, Sitecore.Support.Pipelines.Blocks.GetCustomRelationshipsBlock>()));
            
            services.RegisterAllCommands(assembly);
        }
    }
}