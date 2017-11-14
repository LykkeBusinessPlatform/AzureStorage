using System;
using JetBrains.Annotations;
using Lykke.AzureStorage.Tables.Entity.Metamodel.Providers;

namespace Lykke.AzureStorage.Tables.Entity.Metamodel
{
    /// <summary>
    /// Entity metamodel, which you should configure in order to use custom serialization for entities
    /// </summary>
    [PublicAPI]
    public static class EntityMetamodel
    {
        internal static IEntityMetamodel Instance { get; private set; }

        private static bool _isConfigured;
        
        static EntityMetamodel()
        {
            Instance = EntityMetamodelImpl.Empty;
        }

        /// <summary>
        /// Configures metamodel with the given <paramref name="provider"/>
        /// </summary>
        /// <param name="provider">Metamodel provider</param>
        public static void Configure(IMetamodelProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            if (_isConfigured)
            {
                throw new InvalidOperationException("Metamodel has already been configured");
            }

            Instance = new EntityMetamodelImpl(provider);

            _isConfigured = true;
        }
    }
}
