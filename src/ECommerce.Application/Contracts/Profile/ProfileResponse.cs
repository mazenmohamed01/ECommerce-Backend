namespace ECommerce.Application.Contracts.Profile;

public sealed record ProfileResponse(
    string UserId,
    string Email,
    string? FirstName,
    string? LastName,
    string FullName,
    string? PhoneNumber,
    string? Role,
    string CreatedAt
);
