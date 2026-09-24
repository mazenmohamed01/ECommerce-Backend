using ECommerce.Domain.Enums;

namespace ECommerce.Domain.ValueObjects;

/// <summary>
/// Shipping address owned entity — stored as flat columns on the Orders table (no join).
/// District is required: Saudi shipping couriers (Aramex, SMSA, Naqel) require the neighbourhood.
/// Country defaults to "Saudi Arabia" for single-market deployment.
/// </summary>
public sealed class ShippingAddress
{
    public string Street     { get; private set; } = default!;
    public string District   { get; private set; } = default!;  // الحي
    public SaudiCity City    { get; private set; }              // Enum instead of string
    public string State      { get; private set; } = default!;  // المنطقة
    public string PostalCode { get; private set; } = default!;
    public string Country    { get; private set; } = "Saudi Arabia";

    private ShippingAddress() { }  // EF Core

    public ShippingAddress(
        string street,
        string district,
        SaudiCity city,
        string state,
        string postalCode,
        string country = "Saudi Arabia")
    {
        Street     = street;
        District   = district;
        City       = city;
        State      = state;
        PostalCode = postalCode;
        Country    = country;
    }
}
