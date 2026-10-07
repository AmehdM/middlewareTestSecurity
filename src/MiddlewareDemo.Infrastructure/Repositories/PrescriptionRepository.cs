using Microsoft.Data.Sqlite;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Models;
using MiddlewareDemo.Infrastructure.Data;

namespace MiddlewareDemo.Infrastructure.Repositories;

public sealed class PrescriptionRepository(SqliteDb db, ICryptoService crypto) : IPrescriptionRepository
{
    private const string Columns = "id,customer_id,patient_name,patient_national_id,diagnosis,doctor_name,card_number,issued_at";

    public async Task<Prescription?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command($"SELECT {Columns} FROM prescriptions WHERE id=@id", ("@id", id));
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<IReadOnlyList<Prescription>> ListByCustomerAsync(int customerId, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command($"SELECT {Columns} FROM prescriptions WHERE customer_id=@c ORDER BY id DESC", ("@c", customerId));
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new List<Prescription>();
        while (await reader.ReadAsync(ct))
        {
            result.Add(Map(reader));
        }

        return result;
    }

    public async Task<Prescription> AddAsync(Prescription prescription, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        const string sql = "INSERT INTO prescriptions (customer_id,patient_name,patient_national_id,diagnosis,doctor_name,card_number,issued_at) VALUES (@c,@p,@n,@d,@o,@k,@i)";
        prescription.Id = await connection.InsertAsync(sql, ct, ("@c", prescription.CustomerId), ("@p", prescription.PatientName),
            ("@n", prescription.PatientNationalId), ("@d", crypto.Encrypt(prescription.Diagnosis)), ("@o", prescription.DoctorName),
            ("@k", prescription.CardNumber), ("@i", prescription.IssuedAt.Iso()));
        return prescription;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command("DELETE FROM prescriptions WHERE id=@id", ("@id", id));
        return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    private Prescription Map(SqliteDataReader r) => new()
    {
        Id = r.Int("id"),
        CustomerId = r.Int("customer_id"),
        PatientName = r.Str("patient_name"),
        PatientNationalId = r.Str("patient_national_id"),
        Diagnosis = crypto.Decrypt(r.Str("diagnosis")),
        DoctorName = r.Str("doctor_name"),
        CardNumber = r.Str("card_number"),
        IssuedAt = r.Date("issued_at")
    };
}
