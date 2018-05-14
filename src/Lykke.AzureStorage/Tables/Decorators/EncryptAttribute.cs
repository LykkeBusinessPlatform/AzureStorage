using System;
using AzureStorage.Tables.Decorators;

namespace Lykke.AzureStorage.Tables.Decorators
{
    /// <summary>
    /// Properties, marked with this attribute, will be stored encrypted if EncryptedTableStorageDecorator is using.
    /// </summary>
    /// <seealso cref="EncryptedTableStorageDecorator{T}"/>
    [AttributeUsage(AttributeTargets.Property)]
    public class EncryptAttribute : Attribute
    {
    }
}
