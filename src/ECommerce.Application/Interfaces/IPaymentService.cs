using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Interfaces;

public interface IPaymentService
{
    Task<Result<PaymentInitiationResponse>> InitiatePaymentAsync(Order order, CancellationToken cancellationToken = default);
}
