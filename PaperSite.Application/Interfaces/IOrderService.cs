using PaperSite.Application.Common.Responses;
using PaperSite.Application.DTOs.Order;

namespace PaperSite.Application.Interfaces;

public interface IOrderService
{
    Task<BaseResponse<OrderDto>> CreateAsync(Guid userId, CreateOrderRequest request);
    Task<BaseResponse<OrderDto>> GetByIdAsync(Guid userId, Guid orderId, bool isAdmin = false);
    Task<BaseResponse<IEnumerable<OrderDto>>> GetUserOrdersAsync(Guid userId);
    Task<BaseResponse<IEnumerable<OrderDto>>> GetAllOrdersAsync();
    Task<BaseResponse<OrderDto>> ChangeStatusAsync(Guid orderId, ChangeOrderStatusRequest request);
}
