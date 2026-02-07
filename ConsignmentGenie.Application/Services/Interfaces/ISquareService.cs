namespace ConsignmentGenie.Application.Services.Interfaces;

public interface ISquareService
{
    Task<int> GetItemQuantityAsync(string accessToken, string variationId, CancellationToken ct = default);
    Task UpdateItemQuantityAsync(string accessToken, string variationId, int quantity, CancellationToken ct = default);
}

public class SquareApiException : Exception
{
    public SquareApiException(string message) : base(message) { }
    public SquareApiException(string message, Exception innerException) : base(message, innerException) { }
}