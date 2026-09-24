using ECommerce.Application.Contracts;
using ECommerce.Application.Common;

namespace ECommerce.Application.Interfaces;

public interface ICustomerService
{
    Task<Result<PagedResponse<CustomerResponse>>> SearchCustomersAsync(
        string? searchTerm, 
        int page, 
        int pageSize, 
        CancellationToken cancellationToken = default);

    Task<Result<CustomerDetailsResponse>> GetCustomerDetailsAsync(
        string customerId, 
        CancellationToken cancellationToken = default);
}
