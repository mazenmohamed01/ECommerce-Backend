namespace ECommerce.Application.Contracts.Profile;

public sealed record UpdateProfileRequest(
    string FullName,
    string? PhoneNumber
);
