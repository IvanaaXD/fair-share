namespace FairShare.Application.DTOs.Categories;

public class RejectCategoryRequest
{
    /// <summary>Shown to the user who proposed the category.</summary>
    public string Reason { get; set; } = string.Empty;
}
