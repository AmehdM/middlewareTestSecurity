using System.Xml;
using MiddlewareDemo.Core.Models;

namespace MiddlewareDemo.Infrastructure.Storage;

public sealed class SupplierCatalogImporter
{
    public IReadOnlyList<Product> Read(Stream xml)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Parse,
            XmlResolver = new XmlUrlResolver(),
            MaxCharactersFromEntities = 0
        };
        var products = new List<Product>();
        using var reader = XmlReader.Create(xml, settings);
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element && reader.Name == "item")
            {
                products.Add(new Product
                {
                    Sku = reader.GetAttribute("sku") ?? string.Empty,
                    Name = reader.GetAttribute("name") ?? string.Empty,
                    Price = decimal.TryParse(reader.GetAttribute("price"), out var price) ? price : 0,
                    Category = reader.GetAttribute("category") ?? "general",
                    Description = reader.ReadElementContentAsString()
                });
            }
        }

        return products;
    }
}

public sealed class PriceListReader
{
    public IReadOnlyDictionary<string, decimal> Read(Stream xml)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = 1_000_000
        };
        var prices = new Dictionary<string, decimal>();
        using var reader = XmlReader.Create(xml, settings);
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element && reader.Name == "price" && reader.GetAttribute("sku") is { } sku)
            {
                prices[sku] = decimal.TryParse(reader.GetAttribute("value"), out var value) ? value : 0;
            }
        }

        return prices;
    }
}
