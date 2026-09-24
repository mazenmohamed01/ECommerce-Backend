using AutoMapper;
using ECommerce.Application.Contracts;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Mapping;

public sealed class OrderProfile : BaseProfile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderResponse>()
            .ForMember(d => d.Street, opt => opt.MapFrom(s => s.ShippingAddress.Street))
            .ForMember(d => d.District, opt => opt.MapFrom(s => s.ShippingAddress.District))
            .ForMember(d => d.City, opt => opt.MapFrom(s => s.ShippingAddress.City.ToString()))
            .ForMember(d => d.State, opt => opt.MapFrom(s => s.ShippingAddress.State))
            .ForMember(d => d.PostalCode, opt => opt.MapFrom(s => s.ShippingAddress.PostalCode))
            .ForMember(d => d.PaymentMethod, opt => opt.MapFrom(s => s.PaymentMethod == ECommerce.Domain.Enums.PaymentMethod.Online ? "Online" : "Cash"))
            .ForMember(d => d.PaymentStatus, opt => opt.MapFrom(s => s.PaymentStatus.ToString()))
            .ForMember(d => d.OrderStatus, opt => opt.MapFrom(s => s.OrderStatus.ToString()));

        CreateMap<OrderItem, OrderItemResponse>()
            .ForMember(d => d.ProductSlug, opt => opt.MapFrom(s => s.Product != null ? s.Product.Slug : ""))
            .ForMember(d => d.MainImageUrl, opt => opt.MapFrom(s => 
                s.Product != null && s.Product.Images != null 
                    ? (s.Product.Images.FirstOrDefault(i => i.IsMain) != null 
                        ? s.Product.Images.FirstOrDefault(i => i.IsMain)!.ImageUrl 
                        : (s.Product.Images.FirstOrDefault() != null ? s.Product.Images.FirstOrDefault()!.ImageUrl : ""))
                    : ""));

        CreateMap<OrderStatusHistory, OrderStatusHistoryResponse>()
            .ForMember(d => d.PreviousStatus, opt => opt.MapFrom(s => s.PreviousStatus.ToString()))
            .ForMember(d => d.NewStatus, opt => opt.MapFrom(s => s.NewStatus.ToString()));

        CreateMap<Order, OrderSummaryResponse>()
            .ForMember(d => d.OrderStatus, opt => opt.MapFrom(s => s.OrderStatus.ToString()))
            .ForMember(d => d.PaymentMethod, opt => opt.MapFrom(s => s.PaymentMethod == ECommerce.Domain.Enums.PaymentMethod.Online ? "Online" : "Cash"))
            .ForMember(d => d.PaymentStatus, opt => opt.MapFrom(s => s.PaymentStatus.ToString()));
    }
}
