using System.ComponentModel.DataAnnotations;

namespace Tutorial9.Model;

public class WareHouseRequest
{
    public int IdProduct { get; set; }
    public int IdWarehouse  { get; set; }
    public int Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}