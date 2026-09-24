namespace ECommerce.Application.Contracts.Addresses;

public sealed record AddressDto(
    string Id,
    string Title,
    string Street,
    string District,
    string City,
    string State,
    string PostalCode,
    string Country,
    bool IsDefault
);
