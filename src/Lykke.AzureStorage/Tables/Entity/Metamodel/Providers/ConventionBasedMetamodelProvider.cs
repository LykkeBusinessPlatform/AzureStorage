using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Lykke.AzureStorage.Tables.Entity.Serializers;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;

namespace Lykke.AzureStorage.Tables.Entity.Metamodel.Providers
{
    /// <summary>
    /// Metamodel provider, which builds metamodel according to the registered rules 
    /// </summary>
    [PublicAPI]
    public sealed class ConventionBasedMetamodelProvider : IMetamodelProvider
    {
        #region Fields

        private ImmutableList<(Func<Type, bool> filter, Func<Type, IStorageValueSerializer> factory)> _typeSerializerRules;
        private ImmutableList<(Func<PropertyInfo, bool> filter, Func<PropertyInfo, IStorageValueSerializer> factory)> _propertySerializerRules;
        private ImmutableList<(Func<Type, bool> filter, ValueTypeMergingStrategy strategy)> _typeValueTypeMergingRules;

        #endregion


        #region Constructors

        /// <summary>
        /// Metamodel provider, which builds metamodel according to the registered rules 
        /// </summary>
        public ConventionBasedMetamodelProvider()
        {
            _typeSerializerRules = ImmutableList.Create<(Func<Type, bool>, Func<Type, IStorageValueSerializer>)>();
            _propertySerializerRules = ImmutableList.Create<(Func<PropertyInfo, bool>, Func<PropertyInfo, IStorageValueSerializer>)>();
            _typeValueTypeMergingRules = ImmutableList.Create<(Func<Type, bool> filter, ValueTypeMergingStrategy)>();
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
        public ConventionBasedMetamodelProvider AddTypeSerializerRule(
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

            _typeSerializerRules = _typeSerializerRules.Add((filter, factory));

            return this;
        }

        /// <summary>
        /// Adds rule, which assigns the <paramref name="factory"/> of the serializer for the particular entitiy properties, 
        /// which satisfies the <paramref name="filter"/>
        /// Rules will be applied in that order, in which they were added. First matching rule will be used
        /// </summary>
        /// <param name="filter">Entity properties filter</param>
        /// <param name="factory">Serializers factory</param>
        public ConventionBasedMetamodelProvider AddPropertySerializerRule(
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

            _propertySerializerRules = _propertySerializerRules.Add((filter, factory));

            return this;
        }

        /// <summary>
        /// Adds rule, which assigns the specific <see cref="ValueTypeMergingStrategy"/> for the types,
        /// which satisfies the <paramref name="filter"/>.
        /// Rules will be applied in that order, in which they were added. First matching rule will be used
        /// </summary>
        /// <param name="filter">Types filter</param>
        /// <param name="strategy">Value type merging strategy</param>
        public ConventionBasedMetamodelProvider AddTypeValueTypesMergingStrategyRule(
            Func<Type, bool> filter,
            ValueTypeMergingStrategy strategy)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            _typeValueTypeMergingRules = _typeValueTypeMergingRules.Add((filter, strategy));

            return this;
        }

        #endregion


        #region IMetamodelProvider

        IStorageValueSerializer IMetamodelProvider.TryGetTypeSerializer(Type type)
        {
            return _typeSerializerRules
                .Where(rule => rule.filter(type))
                .Select(rule => rule.factory(type))
                .FirstOrDefault();
        }

        IStorageValueSerializer IMetamodelProvider.TryGetPropertySerializer(PropertyInfo propertyInfo)
        {
            return _propertySerializerRules
                .Where(rule => rule.filter(propertyInfo))
                .Select(rule => rule.factory(propertyInfo))
                .FirstOrDefault();
        }

        ValueTypeMergingStrategy IMetamodelProvider.TryGetValueTypeMergingStrategy(Type type)
        {
            return _typeValueTypeMergingRules
                .Where(rule => rule.filter(type))
                .Select(rule => rule.strategy)
                .FirstOrDefault();
        }

        #endregion
    }
}
