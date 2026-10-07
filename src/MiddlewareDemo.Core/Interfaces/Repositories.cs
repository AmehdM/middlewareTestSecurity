using MiddlewareDemo.Core.Models;

namespace MiddlewareDemo.Core.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<User>> ListAsync(CancellationToken ct = default);
    Task<User> AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
    Task SaveResetTokenAsync(string email, string token, CancellationToken ct = default);
    Task<string?> GetResetTokenAsync(string email, CancellationToken ct = default);
}

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Customer>> ListAsync(string tenantId, int skip, int take, CancellationToken ct = default);
    Task<int> CountAsync(string tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<Customer>> SearchAsync(string tenantId, string term, CancellationToken ct = default);
    Task<Customer> AddAsync(Customer customer, CancellationToken ct = default);
    Task UpdateAsync(Customer customer, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}

public interface ICatalogRepository
{
    Task<Product?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> ListAsync(int skip, int take, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Product>> ListAllAsync();
    Task<Product> AddAsync(Product product, CancellationToken ct = default);
    Task UpdateAsync(Product product, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}

public interface IInventoryRepository
{
    Task<StockLevel?> GetStockAsync(int productId, CancellationToken ct = default);
    Task DecrementAsync(int productId, int quantity, CancellationToken ct = default);
    Task IncrementAsync(int productId, int quantity, CancellationToken ct = default);
    Task<bool> TryDecrementAsync(int productId, int quantity, CancellationToken ct = default);
    Task<IReadOnlyList<StockLevel>> ListBelowAsync(int threshold, CancellationToken ct = default);
}

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> ListByCustomerAsync(int customerId, int skip, int take, CancellationToken ct = default);
    Task<int> CountByCustomerAsync(int customerId, CancellationToken ct = default);
    Task<Order> AddAsync(Order order, CancellationToken ct = default);
    Task UpdateStatusAsync(int id, string status, CancellationToken ct = default);
    Task<Coupon?> GetCouponAsync(string code, CancellationToken ct = default);
    Task RedeemCouponAsync(string code, CancellationToken ct = default);
}

public interface IPrescriptionRepository
{
    Task<Prescription?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Prescription>> ListByCustomerAsync(int customerId, CancellationToken ct = default);
    Task<Prescription> AddAsync(Prescription prescription, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}

public interface IBillingRepository
{
    Task<Invoice?> GetInvoiceAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Invoice>> ListInvoicesAsync(int customerId, CancellationToken ct = default);
    Task<Invoice> AddInvoiceAsync(Invoice invoice, CancellationToken ct = default);
    Task UpdateInvoiceStatusAsync(int id, string status, CancellationToken ct = default);
    Task<Payment> AddPaymentAsync(Payment payment, CancellationToken ct = default);
    Task<IReadOnlyList<Invoice>> ListAllInvoicesAsync();
    Task<IReadOnlyList<Payment>> ListAllPaymentsAsync();
}

public interface IExportRepository
{
    Task<ExportJob> AddAsync(ExportJob job, CancellationToken ct = default);
    Task<ExportJob?> GetAsync(int id, CancellationToken ct = default);
    Task UpdateAsync(ExportJob job, CancellationToken ct = default);
}

public interface INotificationRepository
{
    Task<Notification> AddAsync(Notification notification, CancellationToken ct = default);
    Task<IReadOnlyList<Notification>> ListAsync(string recipient, CancellationToken ct = default);
}

public interface IFileRepository
{
    Task<FileRecord> AddAsync(FileRecord record, CancellationToken ct = default);
    Task<FileRecord?> GetAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<FileRecord>> ListByOwnerAsync(int ownerId, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}

public interface IAuditRepository
{
    Task AddAsync(AuditEntry entry, CancellationToken ct = default);
    Task<IReadOnlyList<AuditEntry>> ListAsync(int skip, int take, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
}

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<Tenant>> ListAsync(CancellationToken ct = default);
    Task<Tenant> AddAsync(Tenant tenant, CancellationToken ct = default);
}
