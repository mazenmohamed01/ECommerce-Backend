using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

/// <summary>
/// Represents a saved shipping address for a user.
/// </summary>
public sealed class UserAddress : AuditableEntity
{
    public string UserId { get; private set; } = default!;
    
    /// <summary>e.g. "المنزل", "العمل"</summary>
    public string Title { get; private set; } = default!;
    
    public string Street { get; private set; } = default!;
    public string District { get; private set; } = default!;
    public string City { get; private set; } = default!;
    public string State { get; private set; } = default!;
    public string PostalCode { get; private set; } = default!;
    public string Country { get; private set; } = "Saudi Arabia";
    
    public bool IsDefault { get; private set; }

    private UserAddress() { } // EF Core

    public static UserAddress Create(
        string userId,
        string title,
        string street,
        string district,
        string city,
        string state,
        string postalCode,
        string country = "Saudi Arabia",
        bool isDefault = false)
    {
        return new UserAddress
        {
            UserId = userId,
            Title = title,
            Street = street,
            District = district,
            City = city,
            State = state,
            PostalCode = postalCode,
            Country = country,
            IsDefault = isDefault
        };
    }

    public void Update(
        string title,
        string street,
        string district,
        string city,
        string state,
        string postalCode,
        string country)
    {
        Title = title;
        Street = street;
        District = district;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
        SetUpdatedAt();
    }

    public void SetDefault(bool isDefault)
    {
        IsDefault = isDefault;
        SetUpdatedAt();
    }
}
