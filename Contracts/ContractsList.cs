namespace Contracts;

public record UserCreatedEvent(
    Guid UserId,
    string Username,
    string Email,
    string ContactNo,
    DateTime CreatedAt);

public record UserUpdatedEvent(
    Guid UserId,
    string Username,
    string Email,
    string ContactNo,
    DateTime UpdatedAt);

public record UserDeletedEvent(
    Guid UserId,
    string Username,
    string Email,
    string ContactNo,
    DateTime DeletedAt);

// ----------------------
// ORDER EVENTS
// ----------------------
public record OrderPlacedEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalPrice,
    DateTime CreatedAt,
    List<OrderItem> Items);

public record OrderUpdatedEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalPrice,
    DateTime UpdatedAt,
    List<OrderItem> Items);

public record OrderCancelledEvent(
    Guid OrderId,
    Guid CustomerId,
    string Reason,
    DateTime CancelledAt);

public record OrderCompletedEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalPrice,
    DateTime CompletedAt);

// Reuseable record for nested details
public record OrderItem(
    Guid OrderItemId,
    int Quantity,
    decimal UnitPrice,
    Guid ProductId,
    string ProductName);

// ----------------------
// PRODUCT EVENTS
// ----------------------
public record ProductCreatedEvent(
    Guid ProductId,
    string ProductName,
    decimal Price,
    int Stock,
    DateTime CreatedAt);

public record ProductUpdatedEvent(
    Guid ProductId,
    string ProductName,
    decimal Price,
    int Stock,
    DateTime UpdatedAt);

public record ProductDeletedEvent(
    Guid ProductId,
    DateTime DeletedAt);

// ----------------------
// PAYMENT EVENTS
// ----------------------
public record PaymentSucceededEvent(
    Guid PaymentId,
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    DateTime PaidAt);

public record PaymentFailedEvent(
    Guid PaymentId,
    Guid OrderId,
    Guid CustomerId,
    string Reason,
    DateTime FailedAt);


