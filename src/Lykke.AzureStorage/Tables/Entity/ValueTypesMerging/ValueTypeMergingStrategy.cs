using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.AzureStorage.Tables.Entity.ValueTypesMerging
{
    /// <summary>
    /// Strategy of entity value type properties merging
    /// </summary>
    public enum ValueTypeMergingStrategy
    {
        /// <summary>
        /// Represents no actual strategy
        /// </summary>
        None,

        /// <summary>
        /// Default strategy. Forbids merging of entities with value type properties
        /// </summary>
        /// <remarks>
        /// Will throw <see cref="InvalidOperationException"/> exception, if you try to merge the entity with value type properties
        /// </remarks>
        Forbid,

        /// <summary>
        /// Mimics default <see cref="TableEntity"/> behaviour. Always updates value type properties while merging
        /// </summary>
        UpdateAlways,

        /// <summary>
        /// Prefered strategy to select. Give you opportunity to correctly merge entities with value type properties
        /// </summary>
        /// <remarks>
        /// If you wan't to correctly merge the entity with value type propertie, implement all value type properties
        /// in your entity as property with backing field and call the <see cref="AzureTableEntity.MarkValueTypePropertyAsDirty"/>
        /// method when setter is being called, even if value doesn't changed.
        /// </remarks>
        UpdateIfDirty
    }
}
