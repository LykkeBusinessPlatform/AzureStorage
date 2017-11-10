using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Lykke.AzureStorage.Tables.Entity.Serializers;

namespace Lykke.AzureStorage.Tables.Entity.Metamodel.Providers
{
    /// <summary>
    /// Metamodel provider, which provides metamodel composed from other metamodel providers 
    /// </summary>
    [PublicAPI]
    public class CompositeMetamodelProvider : IMetamodelProvider
    {
        private readonly List<IMetamodelProvider> _builders;

        /// <summary>
        /// Metamodel provider, which provides metamodel composed from other metamodel providers 
        /// </summary>
        public CompositeMetamodelProvider()
        {
            _builders = new List<IMetamodelProvider>();
        }

        /// <summary>
        /// Adds provider. Composite metamodel will be built in that order, in which providers were added
        /// </summary>
        public CompositeMetamodelProvider AddProvider(IMetamodelProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            _builders.Add(provider);

            return this;
        }

        IStorageValueSerializer IMetamodelProvider.TryGetTypeSerializer(Type type)
        {
            return _builders
                .Select(builder => builder.TryGetTypeSerializer(type))
                .FirstOrDefault(serializer => serializer != null);
        }

        IStorageValueSerializer IMetamodelProvider.TryGetPropertySerializer(PropertyInfo propertyInfo)
        {
            return _builders
                .Select(builder => builder.TryGetPropertySerializer(propertyInfo))
                .FirstOrDefault(serializer => serializer != null);
        }
    }
}
