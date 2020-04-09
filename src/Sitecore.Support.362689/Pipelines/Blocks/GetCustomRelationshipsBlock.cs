using Sitecore.Commerce.Core;
using Sitecore.Commerce.Core.Caching;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Plugin.Catalog.Models;
using Sitecore.Commerce.Plugin.EntityVersions;
using Sitecore.Framework.Caching;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sitecore.Support.Pipelines.Blocks
{
    public class GetCustomRelationshipsBlock : PipelineBlock<CommerceEntity, CommerceEntity, CommercePipelineExecutionContext>
    {
        private readonly IGetEnvironmentCachePipeline _cachePipeline;

        private readonly IFindEntitiesInListPipeline _findEntitiesInListPipeline;

        private readonly IFindSitecoreIdsInListPipeline _findSitecoreIdsInListPipeline;

        public GetCustomRelationshipsBlock(IGetEnvironmentCachePipeline cachePipeline, IFindEntitiesInListPipeline findEntitiesInListPipeline, IFindSitecoreIdsInListPipeline findSitecoreIdsInListPipeline)
            : base((string)null)
        {
            _cachePipeline = cachePipeline;
            _findEntitiesInListPipeline = findEntitiesInListPipeline;
            _findSitecoreIdsInListPipeline = findSitecoreIdsInListPipeline;
        }

        public override async Task<CommerceEntity> Run(CommerceEntity commerceEntity, CommercePipelineExecutionContext context)
        {
            Condition.Requires(commerceEntity).IsNotNull(base.Name + ": The argument cannot be null");
            string entityTypeName = commerceEntity.GetType().GetFullyQualifiedName();
            string cacheKey = context.CommerceContext.Environment.Name + "|GetCustomRelationshipsBlock.RelationshipDefinition|" + entityTypeName;
            EntityMemoryCachingPolicy cachePolicy = EntityMemoryCachingPolicy.GetCachePolicy(context.CommerceContext, typeof(RelationshipDefinition));
            ICache cache = null;
            IEnumerable<RelationshipDefinition> relevantRelationshipDefinitions = null;
            if (cachePolicy.AllowCaching)
            {
                cache = await _cachePipeline.Run(new EnvironmentCacheArgument
                {
                    CacheName = cachePolicy.CacheName
                }, context);
                relevantRelationshipDefinitions = await cache.Get<IEnumerable<RelationshipDefinition>>(cacheKey);
            }
            if (relevantRelationshipDefinitions == null)
            {
                FindEntitiesInListArgument arg = new FindEntitiesInListArgument(typeof(RelationshipDefinition), context.GetPolicy<KnownRelationshipListsPolicy>().CustomRelationshipDefinitions, 0, int.MaxValue);
                relevantRelationshipDefinitions = from RelationshipDefinition x in (await _findEntitiesInListPipeline.Run(arg, context)).List.Items
                                                  where x.SourceType.Equals(entityTypeName, StringComparison.OrdinalIgnoreCase)
                                                  select x;
                if (cachePolicy.AllowCaching && cache != null)
                {
                    await cache.Set(cacheKey, new Cachable<IEnumerable<RelationshipDefinition>>(relevantRelationshipDefinitions, 1L), cachePolicy.GetCacheEntryOptions());
                }
            }
            RelationshipsComponent component = new RelationshipsComponent();
            ConcurrentQueue<Relationship> concurrentItems = new ConcurrentQueue<Relationship>();
            await Task.WhenAll(relevantRelationshipDefinitions.Select((Func<RelationshipDefinition, Task>)async delegate (RelationshipDefinition relationshipDefinition)
            {
                Relationship relationship = new Relationship
                {
                    Name = relationshipDefinition.Name
                };
                Type type = Type.GetType(relationshipDefinition.TargetType);
                string listName = CommerceEntity.VersionedListName(commerceEntity, relationshipDefinition.Name + "-" + commerceEntity.FriendlyId);
                FindEntitiesInListArgument findEntitiesInListArgument = await _findEntitiesInListPipeline.Run(new FindEntitiesInListArgument(type, listName, 0, int.MaxValue)
                {
                    LoadEntities = false,
                    LoadTotalItemCount = false
                }, context);
                if (findEntitiesInListArgument.IdVersionMap.Any())
                {
                    foreach (string item in await _findSitecoreIdsInListPipeline.Run(findEntitiesInListArgument, context))
                    {
                        relationship.RelationshipList.Add(item);
                    }
                }
                concurrentItems.Enqueue(relationship);
            }));
            component.Relationships = concurrentItems.ToList();

            var clone = commerceEntity.Clone<CommerceEntity>();

            clone.SetComponent(component);

            return clone;
        }
    }
}
