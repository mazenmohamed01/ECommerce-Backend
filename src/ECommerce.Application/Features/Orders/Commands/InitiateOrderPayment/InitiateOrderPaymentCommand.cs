using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using System;

namespace ECommerce.Application.Features.Orders.Commands.InitiateOrderPayment;

public sealed record InitiateOrderPaymentCommand(Guid OrderId, string CustomerId) : ICommand<Result<PaymentInitiationResponse>>;
