using Microsoft.EntityFrameworkCore;
using PaperSite.Application.Common.Responses;
using PaperSite.Application.DTOs.Order;
using PaperSite.Application.Interfaces;
using PaperSite.Domain.Entities;
using PaperSite.Domain.Enums;

namespace PaperSite.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(IRepository<Order> orderRepository, IRepository<Product> productRepository, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<OrderDto>> CreateAsync(Guid userId, CreateOrderRequest request)
    {
        var groupedItems = request.Items
            .GroupBy(x => x.ProductId)
            .Select(x => new CreateOrderItemRequest { ProductId = x.Key, Quantity = x.Sum(i => i.Quantity) })
            .ToList();

        var productIds = groupedItems.Select(x => x.ProductId).ToList();
        var products = await _productRepository.Query().Where(x => productIds.Contains(x.Id)).ToListAsync();

        if (products.Count != productIds.Count)
        {
            return BaseResponse<OrderDto>.Failure(" محصولی یافت نشد");
        }

        foreach (var item in groupedItems)
        {
            var product = products.First(x => x.Id == item.ProductId);
            if (product.Stock < item.Quantity)
            {
                return BaseResponse<OrderDto>.Failure($"موجودی محصول '{product.Name}' کافی نیست");
            }
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = OrderStatus.Pending,
            ShippingAddress = request.ShippingAddress,
            ReceiverFullName = request.ReceiverFullName,
            ReceiverPhoneNumber = request.ReceiverPhoneNumber
        };

        foreach (var item in groupedItems)
        {
            var product = products.First(x => x.Id == item.ProductId);
            product.Stock -= item.Quantity;
            _productRepository.Update(product);

            order.Items.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = item.Quantity,
                TotalPrice = product.Price * item.Quantity
            });
        }

        order.TotalAmount = order.Items.Sum(x => x.TotalPrice);
        await _orderRepository.AddAsync(order);
        await _unitOfWork.SaveChangesAsync();

        return BaseResponse<OrderDto>.Success(ToDto(order), "سفارش با موفقیت ثبت شد");
    }

    public async Task<BaseResponse<OrderDto>> GetByIdAsync(Guid userId, Guid orderId, bool isAdmin = false)
    {
        var order = await _orderRepository.Query(true)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == orderId && (isAdmin || x.UserId == userId));

        return order == null
            ? BaseResponse<OrderDto>.Failure("سفارش یافت نشد")
            : BaseResponse<OrderDto>.Success(ToDto(order), "سفارش با موفقیت بازیابی شد");
    }

    public async Task<BaseResponse<IEnumerable<OrderDto>>> GetUserOrdersAsync(Guid userId)
    {
        var orders = await _orderRepository.Query(true)
            .Include(x => x.Items)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return BaseResponse<IEnumerable<OrderDto>>.Success(orders.Select(ToDto), "سفارش‌ها با موفقیت دریافت شدند");
    }

    public async Task<BaseResponse<IEnumerable<OrderDto>>> GetAllOrdersAsync()
    {
        var orders = await _orderRepository.Query(true)
            .Include(x => x.Items)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return BaseResponse<IEnumerable<OrderDto>>.Success(orders.Select(ToDto), "سفارش‌ها با موفقیت دریافت شدند");
    }

    public async Task<BaseResponse<OrderDto>> ChangeStatusAsync(Guid orderId, ChangeOrderStatusRequest request)
    {
        var order = await _orderRepository.Query().Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == orderId);
        if (order == null)
        {
            return BaseResponse<OrderDto>.Failure("سفارش یافت نشد");
        }

        order.Status = request.Status;
        _orderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync();

        return BaseResponse<OrderDto>.Success(ToDto(order), "وضعیت سفارش با موفقیت تغییر یافت");
    }

    private static OrderDto ToDto(Order order) => new()
    {
        Id = order.Id,
        UserId = order.UserId,
        Status = order.Status.ToString(),
        TotalAmount = order.TotalAmount,
        ShippingAddress = order.ShippingAddress,
        ReceiverFullName = order.ReceiverFullName,
        ReceiverPhoneNumber = order.ReceiverPhoneNumber,
        CreatedAt = order.CreatedAt,
        Items = order.Items.Select(x => new OrderItemDto
        {
            Id = x.Id,
            ProductId = x.ProductId,
            ProductName = x.ProductName,
            UnitPrice = x.UnitPrice,
            Quantity = x.Quantity,
            TotalPrice = x.TotalPrice
        }).ToList()
    };
}
