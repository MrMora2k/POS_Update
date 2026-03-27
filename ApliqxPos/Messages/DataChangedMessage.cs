using CommunityToolkit.Mvvm.Messaging.Messages;

namespace ApliqxPos.Messages;

/// <summary>
/// Type of data that has changed.
/// </summary>
public enum DataType
{
    Product,
    Category,
    Customer,
    Sale,
    Inventory
}

/// <summary>
/// Message sent when data entity is added, updated, or deleted.
/// </summary>
public class DataChangedMessage : ValueChangedMessage<DataType>
{
    public DataChangedMessage(DataType value) : base(value)
    {
    }
}
