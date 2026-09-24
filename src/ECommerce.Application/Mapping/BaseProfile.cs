using AutoMapper;

namespace ECommerce.Application.Mapping;

/// <summary>
/// Base AutoMapper profile that all feature-level profiles should inherit from.
/// Centralises common mapping conventions (e.g., null substitution, naming).
/// Add feature profiles by creating additional Profile subclasses in their
/// respective feature folders — AutoMapper discovers them via assembly scan.
/// </summary>
public abstract class BaseProfile : Profile
{
    protected BaseProfile()
    {
        // Global null-to-empty-string substitution for string properties
        AllowNullCollections = false;
    }
}
