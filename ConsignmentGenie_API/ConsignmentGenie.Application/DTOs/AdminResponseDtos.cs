namespace ConsignmentGenie.Application.DTOs;

public class ApprovalResponseDto
{
    public string Message { get; set; } = string.Empty;
}

public class ReseedResponseDto
{
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public TestAccountDto[] TestAccounts { get; set; } = Array.Empty<TestAccountDto>();
    public object CypressTestData { get; set; } = new();
}

public class TestAccountDto
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Store { get; set; } = string.Empty;
}

public class PasswordChangeResponseDto
{
    public string Message { get; set; } = string.Empty;
}

public class OrderDetailResponseDto
{
    public string Message { get; set; } = string.Empty;
}

public class PasswordResetResponseDto
{
    public string Message { get; set; } = string.Empty;
}

public class StoreCodeValidationResponseDto
{
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
}

public class ProviderRegistrationResponseDto
{
    public Guid ProviderId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class DeleteResponseDto
{
    public string Message { get; set; } = string.Empty;
}

public class ReorderResponseDto
{
    public string Message { get; set; } = string.Empty;
}