using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Lykke.AzureStorage.Tables.Entity.PropertyAccessorCreation
{
    internal class PropertySettersFactory : IPropertySettersFactory
    {
        public Action<object, object> Create(PropertyInfo property)
        {
            var self = Expression.Parameter(typeof(object), "this");
            var theValue = Expression.Parameter(typeof(object), "value");
            var isValueType = property.PropertyType.IsValueType;



            Expression valueExpression;
            if (isValueType)
            {
                var theNull = Expression.Constant(null);
                var unboxValueExpression = Expression.Unbox(theValue, property.PropertyType);
                var defaultValueExpression = Expression.Default(property.PropertyType);
                valueExpression = Expression.Condition(
                    test: Expression.Equal(theValue, theNull),
                    ifTrue: defaultValueExpression,
                    ifFalse: unboxValueExpression);
            }
            else
            {
                valueExpression = Expression.Convert(theValue, property.PropertyType);
            }

            var propertyExpression = Expression.Property(Expression.Convert(self, property.ReflectedType), property);

            Expression body = Expression.Assign(propertyExpression, valueExpression);

            var block = Expression.Block(new[] { body, Expression.Empty() });
            var lambda = Expression.Lambda(block, self, theValue);

            return (Action<object, object>)lambda.Compile();
        }
    }
}
