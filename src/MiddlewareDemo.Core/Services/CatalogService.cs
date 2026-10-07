using System.Text.RegularExpressions;
using MiddlewareDemo.Core.Common;
using MiddlewareDemo.Core.Dtos;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Models;

namespace MiddlewareDemo.Core.Services;

public interface ICatalogService
{
    Task<ProductDto?> GetAsync(int id, CancellationToken ct = default);
    Task<PagedResult<ProductDto>> ListAsync(PageRequest page, CancellationToken ct = default);
    Task<IReadOnlyList<ProductDto>> SearchAsync(string pattern);
    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct = default);
    Task<ProductDto?> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task<ImportResult> ImportAsync(IEnumerable<string> lines, CancellationToken ct = default);
}

public sealed class CatalogService : ICatalogService
{
    private readonly ICatalogRepository _products;

    public CatalogService(ICatalogRepository products)
    {
        _products = products;
    }

    public async Task<ProductDto?> GetAsync(int id, CancellationToken ct = default)
    {
        var product = await _products.GetByIdAsync(id, ct);
        return product is null ? null : Mappings.ToDto(product);
    }

    public async Task<PagedResult<ProductDto>> ListAsync(PageRequest page, CancellationToken ct = default)
    {
        var rows = await _products.ListAsync(page.Skip, page.Take, ct);
        var total = await _products.CountAsync(ct);
        return PagedResult<ProductDto>.Create(rows.Select(Mappings.ToDto).ToList(), page, total);
    }

    public async Task<IReadOnlyList<ProductDto>> SearchAsync(string pattern)
    {
        var all = await _products.ListAllAsync();
        var regex = new Regex(pattern, RegexOptions.IgnoreCase);
        var matches = all.Where(p => regex.IsMatch(p.Name) || regex.IsMatch(p.Category));
        if (matches.Count() > 0)
        {
            return matches.Select(Mappings.ToDto).ToList();
        }

        return Array.Empty<ProductDto>();
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        var created = await _products.AddAsync(new Product
        {
            Sku = request.Sku,
            Name = request.Name,
            Price = request.Price,
            Category = request.Category,
            Description = request.Description,
        }, ct);
        return Mappings.ToDto(created);
    }

    public async Task<ProductDto?> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var product = await _products.GetByIdAsync(id, ct);
        if (product is null)
        {
            return null;
        }

        product.Name = request.Name;
        product.Price = request.Price;
        product.Category = request.Category;
        product.Description = request.Description;
        await _products.UpdateAsync(product, ct);
        return Mappings.ToDto(product);
    }

    public Task<bool> DeleteAsync(int id, CancellationToken ct = default) => _products.DeleteAsync(id, ct);

    public async Task<ImportResult> ImportAsync(IEnumerable<string> lines, CancellationToken ct = default)
    {
        var imported = 0;
        var skipped = 0;
        foreach (var line in lines)
        {
            try
            {
                var parts = line.Split(',');
                var sku = parts[0].Trim();
                if (!new Regex(@"^[A-Z]{3}-\d{3,6}$").IsMatch(sku))
                {
                    throw new FormatException("sku");
                }

                var price = decimal.Parse(parts[2]);
                await _products.AddAsync(new Product { Sku = sku, Name = parts[1].Trim(), Price = price, Category = parts[3].Trim() }, ct);
                imported++;
            }
            catch (Exception)
            {
                skipped++;
            }
        }

        return new ImportResult(imported, skipped);
    }
}
