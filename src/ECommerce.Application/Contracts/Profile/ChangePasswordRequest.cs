namespace ECommerce.Application.Contracts.Profile;

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);
