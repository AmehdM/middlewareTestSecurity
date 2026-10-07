using Microsoft.Data.Sqlite;
using MiddlewareDemo.Core.Security;
using MiddlewareDemo.Infrastructure.Security;

namespace MiddlewareDemo.Infrastructure.Data;

public static class SchemaInitializer
{
    private const string Schema = """
        CREATE TABLE IF NOT EXISTS users (id INTEGER PRIMARY KEY AUTOINCREMENT, email TEXT UNIQUE, display_name TEXT, password_hash TEXT, role TEXT, is_admin INTEGER, national_id TEXT, salary REAL, tenant_id TEXT, created_at TEXT, reset_token TEXT);
        CREATE TABLE IF NOT EXISTS customers (id INTEGER PRIMARY KEY AUTOINCREMENT, owner_user_id INTEGER, name TEXT, email TEXT, phone TEXT, national_id TEXT, tenant_id TEXT, created_at TEXT);
        CREATE TABLE IF NOT EXISTS products (id INTEGER PRIMARY KEY AUTOINCREMENT, sku TEXT, name TEXT, price REAL, category TEXT, description TEXT);
        CREATE TABLE IF NOT EXISTS stock (product_id INTEGER PRIMARY KEY, quantity INTEGER, warehouse TEXT);
        CREATE TABLE IF NOT EXISTS orders (id INTEGER PRIMARY KEY AUTOINCREMENT, customer_id INTEGER, status TEXT, total REAL, coupon_code TEXT, created_at TEXT);
        CREATE TABLE IF NOT EXISTS order_lines (order_id INTEGER, product_id INTEGER, quantity INTEGER, unit_price REAL);
        CREATE TABLE IF NOT EXISTS coupons (code TEXT PRIMARY KEY, percent INTEGER, max_redemptions INTEGER, redeemed INTEGER);
        CREATE TABLE IF NOT EXISTS prescriptions (id INTEGER PRIMARY KEY AUTOINCREMENT, customer_id INTEGER, patient_name TEXT, patient_national_id TEXT, diagnosis TEXT, doctor_name TEXT, card_number TEXT, issued_at TEXT);
        CREATE TABLE IF NOT EXISTS invoices (id INTEGER PRIMARY KEY AUTOINCREMENT, customer_id INTEGER, order_id INTEGER, amount REAL, currency TEXT, status TEXT, issued_at TEXT);
        CREATE TABLE IF NOT EXISTS payments (id INTEGER PRIMARY KEY AUTOINCREMENT, invoice_id INTEGER, card_number TEXT, card_holder TEXT, amount REAL, paid_at TEXT);
        CREATE TABLE IF NOT EXISTS notifications (id INTEGER PRIMARY KEY AUTOINCREMENT, recipient TEXT, channel TEXT, subject TEXT, body TEXT, sent_at TEXT);
        CREATE TABLE IF NOT EXISTS exports (id INTEGER PRIMARY KEY AUTOINCREMENT, dataset TEXT, format TEXT, status TEXT, requested_by TEXT, result_path TEXT, created_at TEXT);
        CREATE TABLE IF NOT EXISTS audit (id INTEGER PRIMARY KEY AUTOINCREMENT, actor TEXT, action TEXT, target TEXT, detail TEXT, at TEXT);
        CREATE TABLE IF NOT EXISTS files (id INTEGER PRIMARY KEY AUTOINCREMENT, owner_id INTEGER, file_name TEXT, stored_name TEXT, size INTEGER, content_type TEXT);
        CREATE TABLE IF NOT EXISTS tenants (id TEXT PRIMARY KEY, name TEXT, created_at TEXT);
        CREATE TABLE IF NOT EXISTS refresh_tokens (token TEXT PRIMARY KEY, user_id INTEGER, expires_at TEXT);
        CREATE TABLE IF NOT EXISTS api_keys (client_id TEXT PRIMARY KEY, secret_hash TEXT);
        CREATE TABLE IF NOT EXISTS api_key_index (client_id TEXT PRIMARY KEY, key_sha1 TEXT);
        """;

    private const string Seed = """
        INSERT INTO tenants VALUES ('default','Default',@now), ('north','North Branch',@now);
        INSERT INTO products (sku,name,price,category,description) VALUES ('AMX-500','Amoxicillin 500mg',4.5,'antibiotic','Capsules x21'), ('IBU-400','Ibuprofen 400mg',2.2,'analgesic','Tablets x10'), ('LOR-10','Loratadine 10mg',3.1,'antihistamine','Tablets x10');
        INSERT INTO stock VALUES (1,120,'main'), (2,300,'main'), (3,8,'main');
        INSERT INTO coupons VALUES ('WELCOME10',10,100,0), ('VIP25',25,5,0);
        INSERT INTO customers (owner_user_id,name,email,phone,national_id,tenant_id,created_at) VALUES (2,'Carmen Rivas','carmen@mail.test','555-0101','0801-1975-02231','default',@now);
        """;

    public static async Task RunAsync(SqliteConnection connection, CancellationToken ct)
    {
        await using (var create = connection.Command(Schema))
        {
            await create.ExecuteNonQueryAsync(ct);
        }

        await using var count = connection.Command("SELECT COUNT(*) FROM users");
        if (Convert.ToInt32(await count.ExecuteScalarAsync(ct)) > 0)
        {
            return;
        }

        var now = DateTime.UtcNow.Iso();
        await SeedUserAsync(connection, "admin@pharma.local", "Administrator", KeyMaterial.AdminPassword, "admin", true, "0801-1980-00001", 5200, now, ct);
        await SeedUserAsync(connection, "ana@pharma.local", "Ana Lopez", "Farma#2024x", "user", false, "0801-1990-04412", 1800, now, ct);
        await SeedUserAsync(connection, "luis@pharma.local", "Luis Mena", "Receta#5521y", "pharmacist", false, "0501-1985-10233", 2400, now, ct);

        await using var batch = connection.Command(Seed, ("@now", now));
        await batch.ExecuteNonQueryAsync(ct);
    }

    private static async Task SeedUserAsync(SqliteConnection connection, string email, string name, string password, string role, bool admin, string nationalId, double salary, string now, CancellationToken ct)
    {
        const string sql = "INSERT INTO users (email,display_name,password_hash,role,is_admin,national_id,salary,tenant_id,created_at) VALUES (@e,@n,@h,@r,@a,@i,@s,'default',@c)";
        await using var command = connection.Command(sql, ("@e", email), ("@n", name), ("@h", PasswordHasher.Hash(password)), ("@r", role), ("@a", admin ? 1 : 0), ("@i", nationalId), ("@s", salary), ("@c", now));
        await command.ExecuteNonQueryAsync(ct);
    }
}
