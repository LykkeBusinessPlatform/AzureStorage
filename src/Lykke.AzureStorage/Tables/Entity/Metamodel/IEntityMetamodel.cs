using System.Reflection;
using Lykke.AzureStorage.Tables.Entity.Serializers;

namespace Lykke.AzureStorage.Tables.Entity.Metamodel
{
    internal interface IEntityMetamodel
    {
        IStorageValueSerializer TryGetSerializer(PropertyInfo property);
    }
}
