using Microsoft.AspNetCore.Mvc;
using MiddlewareDemo.Api.Endpoints.Support;
using MiddlewareDemo.Core.Common;
using MiddlewareDemo.Core.Services;

namespace MiddlewareDemo.Api.Controllers;

[Route("backoffice")]
public sealed class BackOfficeController : Controller
{
    private readonly IAuditService _audit;
    private readonly ICustomerService _customers;
    private readonly IOrderService _orders;

    public BackOfficeController(IAuditService audit, ICustomerService customers, IOrderService orders)
    {
        _audit = audit;
        _customers = customers;
        _orders = orders;
    }

    [HttpGet("audit")]
    public async Task<IActionResult> Audit(int page = 1, CancellationToken ct = default)
    {
        var entries = await _audit.ListAsync(new PageRequest(page, 25), ct);
        return View("~/Views/Audit/Index.cshtml", entries);
    }

    [HttpGet("customers/{id:int}")]
    public async Task<IActionResult> Customer(int id, CancellationToken ct)
    {
        var customer = await _customers.GetAsync(id, HttpContext.UserId(), HttpContext.IsAdmin(), ct);
        if (customer is null)
        {
            return NotFound();
        }

        return View("~/Views/Customers/Profile.cshtml", customer);
    }

    [HttpGet("orders/{id:int}/receipt")]
    public async Task<IActionResult> Receipt(int id, CancellationToken ct)
    {
        var order = await _orders.GetAsync(id, ct);
        if (order is null)
        {
            return NotFound();
        }

        return View("~/Views/Orders/Receipt.cshtml", order);
    }

    [HttpGet("notice")]
    public IActionResult Notice(string message, string title = "Notice")
    {
        ViewData["Title"] = title;
        return View("~/Views/Reports/Notice.cshtml", model: message);
    }
}
