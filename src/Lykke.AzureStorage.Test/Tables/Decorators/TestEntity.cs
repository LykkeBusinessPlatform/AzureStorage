using System;
using Lykke.AzureStorage.Tables.Decorators;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.AzureStorage.Test.Tables.Decorators
{
    public sealed class TestEntity : TableEntity, IEquatable<TestEntity>
    {
        public TestEntity()
        {
        }

        public TestEntity(int id, string propertyAsEncrypted, string partition, string row)
        {
            Id = id;
            PropertyAsEncrypted = propertyAsEncrypted;
            PartitionKey = partition;
            RowKey = row;
        }

        public int Id { get; set; }
        [Encrypt]
        public string PropertyAsEncrypted { get; set; }
        public string PlainProperty { get; set; }
        [Encrypt]
        public string SecondPropertyAsEncrypted { get; set; }

        public bool Equals(TestEntity other)
        {
            return Id == other.Id
                   && string.Equals(PropertyAsEncrypted, other.PropertyAsEncrypted)
                   && string.Equals(PlainProperty, other.PlainProperty)
                   && string.Equals(SecondPropertyAsEncrypted, other.SecondPropertyAsEncrypted)
                   && string.Equals(PartitionKey, other.PartitionKey)
                   && string.Equals(RowKey, other.RowKey);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((TestEntity)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var res = (Id * 397) ^ (PropertyAsEncrypted != null ? PropertyAsEncrypted.GetHashCode() : 0);
                res = (res * 397) ^ (PlainProperty != null ? PlainProperty.GetHashCode() : 0);
                res = (res * 397) ^ (SecondPropertyAsEncrypted != null ? SecondPropertyAsEncrypted.GetHashCode() : 0);
                res = (res * 397) ^ (PartitionKey != null ? PartitionKey.GetHashCode() : 0);
                res = (res * 397) ^ (RowKey != null ? RowKey.GetHashCode() : 0);
                return res;
            }
        }
    }
}
