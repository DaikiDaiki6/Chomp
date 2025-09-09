namespace Contracts;

public record CreateUserEvent(Guid UserId, string Username, string Email, string ContactNo, DateTime CreateAt);
public record UpdateUserEvent(Guid UserId, string Username, string Email, string ContactNo, DateTime UpdatedAt);
public record DeleteUserEvent(Guid UserId, string Username, string Email, string ContactNo, DateTime UpdatedAt); // not sure with UpdatedAt

public record CreateOrderEvent(Guid OrderId, Guid CustomerId, decimal TotalPrice, DateTime CreatedAt, List<OrderItem> OrderItems);
public record OrderSuccessEvent(Guid OrderId, Guid CustomerId, decimal TotalPrice, DateTime CreatedAt, List<OrderItem> OrderItems);
public record OrderFailedEvent(Guid OrderId, Guid CustomerId, string Reason);
public record OrderItem(Guid OrderItemId, int Quantity, decimal UnitPrice, Guid ProductId, string ProductName, decimal TotalPrice);

public record PaymentSuccessEvent(Guid PaymentId, Guid CustomerId, Guid OrderId, string Status);
public record PaymentFailedEvent(Guid PaymentId, Guid CustomerId, Guid OrderId, string Reason);


