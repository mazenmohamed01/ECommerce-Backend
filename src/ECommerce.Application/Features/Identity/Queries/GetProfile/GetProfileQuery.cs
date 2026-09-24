using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;

namespace ECommerce.Application.Features.Identity.Queries.GetProfile;

public sealed record GetProfileQuery(string UserId) : IQuery<Result<UserProfileResponse>>;
