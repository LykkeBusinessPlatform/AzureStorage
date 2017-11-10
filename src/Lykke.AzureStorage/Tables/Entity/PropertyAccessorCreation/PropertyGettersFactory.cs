using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Lykke.AzureStorage.Tables.Entity.PropertyAccessorCreation
{
    internal class PropertyGettersFactory : IPropertyGettersFactory
    {
        public Func<object, object> Create(PropertyInfo property)
        {
            var self = Expression.Parameter(typeof(object), "this");
            var propertyExpression = Expression.Property(Expression.Convert(self, property.ReflectedType), property);

            Expression castExpression = Expression.Convert(propertyExpression, typeof(object));

            var lambda = Expression.Lambda(castExpression, self);

            return (Func<object, object>)lambda.Compile();
        }
    }
}
