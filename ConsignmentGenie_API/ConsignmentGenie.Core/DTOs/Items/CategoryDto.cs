namespace ConsignmentGenie.Core.DTOs.Items;

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public int? DisplayOrder { get; set; }
}

public class UpdateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}

public class ReorderCategoriesRequest
{
    public List<CategoryOrderUpdate> CategoryOrders { get; set; } = new();
}

public class CategoryOrderUpdate
{
    public Guid CategoryId { get; set; }
    public int DisplayOrder { get; set; }
}

public class CategoryUsageDto
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public int AvailableItemCount { get; set; }
    public int SoldItemCount { get; set; }
}