using Tutorial9.Model;

namespace Tutorial9.Services;

public interface IDbService
{
    Task AddProductToWarehouseAsync(WareHouseRequest request);
    Task ProcedureAsync(WareHouseRequest request);
}