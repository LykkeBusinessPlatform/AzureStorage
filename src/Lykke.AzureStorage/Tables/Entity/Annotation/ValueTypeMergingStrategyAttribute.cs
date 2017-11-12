using System;
using JetBrains.Annotations;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;

namespace Lykke.AzureStorage.Tables.Entity.Annotation
{
    /// <summary>
    /// Marks an entity type to use specific <see cref="ValueTypeMergingStrategy"/> 
    /// when performing InsertOrMerge operation 
    /// </summary>
    [PublicAPI]
    [AttributeUsage(AttributeTargets.Class)]
    public class ValueTypeMergingStrategyAttribute : Attribute
    {
        /// <summary>
        /// Value type merging strategy
        /// </summary>
        public ValueTypeMergingStrategy Strategy { get; }

        /// <summary>
        /// Marks an entity type to use specific <see cref="ValueTypeMergingStrategy"/> 
        /// when performing InsertOrMerge operation
        /// </summary>
        public ValueTypeMergingStrategyAttribute(ValueTypeMergingStrategy strategy)
        {
            Strategy = strategy;
        }
    }
}
