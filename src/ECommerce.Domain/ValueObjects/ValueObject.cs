namespace ECommerce.Domain.ValueObjects;

/// <summary>
/// Value Object base record — equality is by value, not by reference.
/// Use C# record types so structural equality is automatic.
/// </summary>
public abstract record ValueObject;
