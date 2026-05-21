using Microsoft.AspNetCore.Mvc;
using OrderHistory.Api.Dtos;
using OrderHistory.Api.Models;
using OrderHistory.Api.Services;

namespace OrderHistory.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(OrderService orders) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<OrderSummaryDto>>> ListByUser(
        [FromQuery] int userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] OrderStatus[]? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken ct = default)
    {
        if (userId <= 0)
        {
            return BadRequest(new { error = "userId is required." });
        }

        // ORD-1612: filter parameters land on the controller surface so clients
        // and tests can hit the endpoint with them, but they are not yet
        // threaded into the service call. Wiring them up is the body of work.
        var result = await orders.ListByUserAsync(userId, page, pageSize, includeDeleted, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDetailDto>> GetById(int id, CancellationToken ct)
    {
        var result = await orders.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<OrderDetailDto>> Place([FromBody] PlaceOrderRequest request, CancellationToken ct)
    {
        if (request.Items.Count == 0)
        {
            return BadRequest(new { error = "Order must have at least one line item." });
        }
        try
        {
            var created = await orders.PlaceAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var outcome = await orders.CancelAsync(id, ct);
        return outcome switch
        {
            CancelResult.Cancelled => NoContent(),
            CancelResult.NotFound => NotFound(),
            CancelResult.AlreadyCancelled => Conflict(new { error = "Order is already cancelled." }),
            CancelResult.NotCancellable => Conflict(new { error = "Shipped or delivered orders cannot be cancelled." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
