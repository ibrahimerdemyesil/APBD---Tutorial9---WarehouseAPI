using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Tutorial9.Model;
using Tutorial9.Services;

namespace Tutorial9.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WarehouseController : ControllerBase
{
    private readonly IDbService _dbService;

    public WarehouseController(IDbService dbService)
    {
        _dbService = dbService;
    }

    [HttpPost("products")]
    public async Task<IActionResult> AddProductToWarehouse([FromBody] WareHouseRequest request)
    {
        try
        {
            var insertedId = await _dbService.AddProductToWarehouseAsync(request);
            return Ok(new { Id = insertedId });
        }
        catch (Exception e)
        {
            return e.Message switch
            {
                "Product not found" => NotFound(e.Message),
                "Warehouse not found" => NotFound(e.Message),
                "Amount must be positive" => BadRequest(e.Message),
                "Order not found" => NotFound(e.Message),
                "Product already exist in warehouse" => BadRequest(e.Message),
                "Product price not found" => NotFound(e.Message),
                "Insert failed" => StatusCode(500, e.Message),
                _ => StatusCode(500, e.Message)
            };
        }
    }

    [HttpPost("products/procedure")]
    public async Task<IActionResult> AddProductUsingProcedure([FromBody] WareHouseRequest request)
    {
        try
        {
            await _dbService.ProcedureAsync(request);
            return Ok("Stored procedure executed successfully");
        }
        catch (SqlException e)
        {
            return StatusCode(500, e.Message);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
}