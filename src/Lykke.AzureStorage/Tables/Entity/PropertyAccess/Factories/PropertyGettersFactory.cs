using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Lykke.AzureStorage.Tables.Entity.PropertyAccess.Factories
{
    internal class PropertyGettersFactory : IPropertyGettersFactory
    {
        public Func<AzureTableEntity, object> Create(PropertyInfo property)
        {
            var self = Expression.Parameter(typeof(AzureTableEntity), "this");
            var propertyExpression = Expression.Property(Expression.Convert(self, property.ReflectedType), property);

            Expression castExpression = Expression.Convert(propertyExpression, typeof(object));

            var lambda = Expression.Lambda(castExpression, self);

            return (Func<AzureTableEntity, object>)lambda.Compile();
        }
    }
}
