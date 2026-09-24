namespace ECommerce.Application.Common.Errors;

public static class DomainErrors
{
    public static class Address
    {
        public static readonly Error NotFound = Error.NotFound(
            "Address.NotFound", "The address was not found.");
            
        public static readonly Error InvalidId = Error.Validation(
            "Address.InvalidId", "The provided address ID is invalid.");
    }
}
