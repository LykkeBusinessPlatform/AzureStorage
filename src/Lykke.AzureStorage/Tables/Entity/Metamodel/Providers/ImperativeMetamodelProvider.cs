using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using Lykke.AzureStorage.Tables.Entity.Serializers;

namespace Lykke.AzureStorage.Tables.Entity.Metamodel.Providers
{
    /// <summary>
    /// Metamodel provider, which builds metamodel according the particular user types or entity properties mappings to
    /// the serializers
    /// </summary>
    [PublicAPI]
    public sealed class ImperativeMetamodelProvider : IMetamodelProvider
    {
        #region Fields

        private readonly Dictionary<Type, IStorageValueSerializer> _typeSerializers;
        private readonly Dictionary<PropertyInfo, IStorageValueSerializer> _propertySerializers;

        #endregion


        #region Constructors

        /// <summary>
        /// Metamodel provider, which builds metamodel according the particular user types or entity properties mappings to
        /// the serializers
        /// </summary>
        public ImperativeMetamodelProvider()
        {
            _typeSerializers = new Dictionary<Type, IStorageValueSerializer>();
            _propertySerializers = new Dictionary<PropertyInfo, IStorageValueSerializer>();
        }

        #endregion


        #region Public API

        /// <summary>
        /// Registers the given <paramref name="serializer"/> for the given user <paramref name="type"/>
        /// </summary>
        /// <param name="type">User type which will be serialized</param>
        /// <param name="serializer">User type serializer</param>
        public IMetamodelProvider Register(Type type, IStorageValueSerializer serializer)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            _typeSerializers.Add(type, serializer);

            return this;
        }

        /// <summary>
        /// Registers the given <paramref name="serializer"/> for the entity <paramref name="property"/>
        /// </summary>
        /// <param name="property">Entity property which will be serialized</param>
        /// <param name="serializer">Entity property serializer</param>
        public IMetamodelProvider Register(PropertyInfo property, IStorageValueSerializer serializer)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }
            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }
            if (!typeof(AzureTableEntity).IsAssignableFrom(property.DeclaringType))
            {
                throw new ArgumentException($"Property should be declared in the entity type, which is descendant of the {typeof(AzureTableEntity)}");
            }

            _propertySerializers.Add(property, serializer);

            return this;
        }

        /// <summary>
        /// Registers the given <paramref name="serializer"/> for the given user type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">User type which will be serialized</typeparam>
        /// <param name="serializer">User type serializer</param>
        public IMetamodelProvider Register<T>(IStorageValueSerializer serializer)
        {
            return Register(typeof(T), serializer);
        }

        /// <summary>
        /// Registers the given <paramref name="serializer"/> for the entity property, 
        /// which is expressed in the <paramref name="propertyExpression"/>
        /// </summary>
        /// <param name="propertyExpression">Expression for entity property which will be serialized</param>
        /// <param name="serializer">Entity property serializer</param>
        public IMetamodelProvider Register<TEntity, TProperty>(
            Expression<Func<TEntity, TProperty>> propertyExpression,
            IStorageValueSerializer serializer)
        {
            if (propertyExpression == null)
            {
                throw new ArgumentNullException(nameof(propertyExpression));
            }

            var propertyInfo = GetPropertyInfo(propertyExpression);

            return Register(propertyInfo, serializer);
        }

        #endregion


        #region IMetamodelProvider

        IStorageValueSerializer IMetamodelProvider.TryGetTypeSerializer(Type type)
        {
            _typeSerializers.TryGetValue(type, out var serializer);

            return serializer;
        }

        IStorageValueSerializer IMetamodelProvider.TryGetPropertySerializer(PropertyInfo propertyInfo)
        {
            _propertySerializers.TryGetValue(propertyInfo, out var serializer);

            return serializer;
        }

        #endregion


        #region Private methods

        private static PropertyInfo GetPropertyInfo<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression)
        {
            if (propertyExpression.Body is UnaryExpression unaryExpression)
            {
                if (unaryExpression.Operand is MemberExpression memberExpression)
                {
                    if (memberExpression.Member is PropertyInfo propertyInfo)
                    {
                        return propertyInfo;
                    }
                }
            }
            else if (propertyExpression.Body is MemberExpression memberExpression)
            {
                if (memberExpression.Member is PropertyInfo propertyInfo)
                {
                    return propertyInfo;
                }
            }

            throw new ArgumentException("Expression should be property member expression", nameof(propertyExpression));
        }

        #endregion
    }
}
