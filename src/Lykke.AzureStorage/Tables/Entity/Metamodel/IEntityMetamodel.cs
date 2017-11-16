using System;
using System.Reflection;
using Lykke.AzureStorage.Tables.Entity.Serializers;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;

namespace Lykke.AzureStorage.Tables.Entity.Metamodel
{
    /// <summary>
    /// Implementation should be thread safe
    /// </summary>
    internal interface IEntityMetamodel
    {
        IStorageValueSerializer TryGetSerializer(PropertyInfo property);
        ValueTypeMergingStrategy TryGetValueTypeMergingStrategy(Type type);
    }
}
