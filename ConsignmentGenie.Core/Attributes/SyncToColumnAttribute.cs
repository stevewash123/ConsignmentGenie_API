namespace ConsignmentGenie.Core.Attributes;

/// <summary>
/// Marks a property to be synchronized with a database column
/// Used for backward compatibility and business logic access
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class SyncToColumnAttribute : Attribute
{
    public string ColumnName { get; }

    public SyncToColumnAttribute(string columnName)
    {
        ColumnName = columnName;
    }
}