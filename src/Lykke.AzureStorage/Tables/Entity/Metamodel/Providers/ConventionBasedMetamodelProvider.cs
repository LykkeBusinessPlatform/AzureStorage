using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Lykke.AzureStorage.Tables.Entity.Serializers;

namespace Lykke.AzureStorage.Tables.Entity.Metamodel.Providers
{
    /// <summary>
    /// Metamodel provider, which builds metamodel according to the registered rules 
    /// </summary>
    [PublicAPI]
    public sealed class ConventionBasedMetamodelProvider : IMetamodelProvider
    {
        #region Fields

        private List<(Func<Type, bool> filter, Func<Type, IStorageValueSerializer> factory)> _typeIncludeRules;
        private List<(Func<PropertyInfo, bool> filter, Func<PropertyInfo, IStorageValueSerializer> factory)> _propertyIncludeRules;

        #endregion


        #region Constructors

        /// <summary>
        /// Metamodel provider, which builds metamodel according to the registered rules 
        /// </summary>
        public ConventionBasedMetamodelProvider()
        {
            _typeIncludeRules = new List<(Func<Type, bool>, Func<Type, IStorageValueSerializer>)>();
            _propertyIncludeRules = new List<(Func<PropertyInfo, bool>, Func<PropertyInfo, IStorageValueSerializer>)>();
        }

        #endregion


        #region Public API

        /// <summary>
        /// Adds rule, which assigns the <paramref name="factory"/> of the serializer for the types, 
        /// which satisfies the <paramref name="filter"/>.
        /// Rules will be applied in that order, in which they were added. First matching rule will be used
        /// </summary>
        /// <param name="filter">Types filter</param>
        /// <param name="factory">Serializers factory</param>
        public ConventionBasedMetamodelProvider AddTypeRule(
            Func<Type, bool> filter, 
            Func<Type, IStorageValueSerializer> factory)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            _typeIncludeRules.Add((filter, factory));

            return this;
        }

        /// <summary>
        /// Adds rule, which assigns the <paramref name="factory"/> of the serializer for the particular entitiy properties, 
        /// which satisfies the <paramref name="filter"/>
        /// Rules will be applied in that order, in which they were added. First matching rule will be used
        /// </summary>
        /// <param name="filter">Entity properties filter</param>
        /// <param name="factory">Serializers factory</param>
        public ConventionBasedMetamodelProvider AddPropertyRule(
            Func<PropertyInfo, bool> filter, 
            Func<PropertyInfo, IStorageValueSerializer> factory)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            _propertyIncludeRules.Add((filter, factory));

            return this;
        }

        #endregion


        #region IMetamodelProvider

        IStorageValueSerializer IMetamodelProvider.TryGetTypeSerializer(Type type)
        {
            return _typeIncludeRules
                .Where(rule => rule.filter(type))
                .Select(rule => rule.factory(type))
                .FirstOrDefault();
        }

        IStorageValueSerializer IMetamodelProvider.TryGetPropertySerializer(PropertyInfo propertyInfo)
        {
            return _propertyIncludeRules
                .Where(rule => rule.filter(propertyInfo))
                .Select(rule => rule.factory(propertyInfo))
                .FirstOrDefault();
        }

        #endregion
    }
}
