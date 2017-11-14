using System.Reflection;

namespace Lykke.AzureStorage.Tables.Entity.PropertyAccessorCreation
{
    internal class EntityPropertyAccessorsFactory : IEntityPropertyAccessorsFactory
    {
        private readonly IPropertyGettersFactory _gettersFactory;
        private readonly IPropertySettersFactory _settersFactory;
        private readonly IStorageValueConvertersFactory _convertersFactory;

        public EntityPropertyAccessorsFactory(
            IPropertyGettersFactory gettersFactory,
            IPropertySettersFactory settersFactory,
            IStorageValueConvertersFactory convertersFactory)
        {
            _gettersFactory = gettersFactory;
            _settersFactory = settersFactory;
            _convertersFactory = convertersFactory;
        }

        public EntityPropertyAccessor Create(PropertyInfo property)
        {
            var getter = _gettersFactory.Create(property);
            var setter = _settersFactory.Create(property);
            var converter = _convertersFactory.Create(property);

            return new EntityPropertyAccessor(property.Name, getter, setter, converter);
        }
    }
}
