using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts.Profile;

namespace ECommerce.Application.Features.Profiles.Queries.GetProfile;

public sealed record GetCustomerProfileQuery(string UserId) : IQuery<Result<ProfileResponse>>;
