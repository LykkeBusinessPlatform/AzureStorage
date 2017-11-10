using System;
using System.Reflection;
using JetBrains.Annotations;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.Serializers;

namespace Lykke.AzureStorage.Tables.Entity.Metamodel.Providers
{
    /// <summary>
    /// Metamodel provider, which builds metamodel according to entities annotations 
    /// with <see cref="ValueSerializerAttribute"/> attribute
    /// </summary>
    [PublicAPI]
    public sealed class AnnotationsBasedMetamodelProvider : IMetamodelProvider
    {
        #region Nested classes

        private class ActivatorServiceProvider : IServiceProvider
        {
            public object GetService(Type serviceType)
            {
                return Activator.CreateInstance(serviceType);
            }
        }

        private class FactoryServiceProvider : IServiceProvider
        {
            private readonly Func<Type, IStorageValueSerializer> _serializersFactory;

            public FactoryServiceProvider(Func<Type, IStorageValueSerializer> serializersFactory)
            {
                _serializersFactory = serializersFactory ?? throw new ArgumentNullException(nameof(serializersFactory));
            }

            public object GetService(Type serviceType)
            {
                return _serializersFactory(serviceType);
            }
        }

        #endregion


        #region Fields

        private readonly IServiceProvider _serviceProvider;

        #endregion


        #region Constructors

        /// <summary>
        /// Metamodel provider, which builds metamodel according to entities annotations 
        /// with <see cref="ValueSerializerAttribute"/> attribute. 
        /// Serializer should has default public constructor
        /// </summary>
        public AnnotationsBasedMetamodelProvider() :
            this(new ActivatorServiceProvider())
        {
        }

        /// <summary>
        /// Metamodel provider, which builds metamodel according to entities annotations 
        /// with <see cref="ValueSerializerAttribute"/> attribute.
        /// </summary>
        /// <param name="serializersFactory">User serializers factory</param>
        public AnnotationsBasedMetamodelProvider(Func<Type, IStorageValueSerializer> serializersFactory) :
            this(new FactoryServiceProvider(serializersFactory))
        {
        }

        /// <summary>
        /// Metamodel provider, which builds metamodel according to entities annotations 
        /// with <see cref="ValueSerializerAttribute"/> attribute.
        /// </summary>
        /// <param name="serializersServiceProvider">User service provider, used to create serializers instance</param>
        public AnnotationsBasedMetamodelProvider(IServiceProvider serializersServiceProvider)
        {
            _serviceProvider = serializersServiceProvider ?? throw new ArgumentNullException(nameof(serializersServiceProvider));
        }

        #endregion


        #region IMetamodelProvider

        IStorageValueSerializer IMetamodelProvider.TryGetTypeSerializer(Type type)
        {
            var serializerType = type.GetCustomAttribute<ValueSerializerAttribute>()?.SerializerType;

            if (serializerType == null)
            {
                return null;
            }

            try
            {
                return CreateSerializer(serializerType);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create storage value serializer of type {serializerType} specified for the type", ex);
            }
        }

        IStorageValueSerializer IMetamodelProvider.TryGetPropertySerializer(PropertyInfo propertyInfo)
        {
            var serializerType = propertyInfo.GetCustomAttribute<ValueSerializerAttribute>()?.SerializerType;

            if (serializerType == null)
            {
                return null;
            }

            try
            {
                return CreateSerializer(serializerType);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create storage value serializer of type {serializerType} specified for the property", ex);
            }
        }

        #endregion


        #region Private methods

        private IStorageValueSerializer CreateSerializer(Type serializerType)
        {
            if (!typeof(IStorageValueSerializer).IsAssignableFrom(serializerType))
            {
                throw new InvalidOperationException($"Type specified as the serializer should implements {typeof(IStorageValueSerializer)} interface");
            }

            var serializer = _serviceProvider.GetService(serializerType);

            if (serializer == null)
            {
                throw new InvalidOperationException($"Service provider {_serviceProvider.GetType()} created null instead of the serializer instance");
            }

            if (serializer is IStorageValueSerializer storageValueSerializer)
            {
                return storageValueSerializer;
            }

            throw new InvalidOperationException($"Service provider {_serviceProvider.GetType()} created instance of the {serializer.GetType()} type which is not implements {typeof(IStorageValueSerializer)}");
        }

        #endregion
    }
}
