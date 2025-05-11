using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Tutorial9.Model;

namespace Tutorial9.Services;

public class DbService : IDbService
{
    private readonly IConfiguration _configuration;

    public DbService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<int> AddProductToWarehouseAsync(WareHouseRequest request)
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();

        command.Connection = connection;
        await connection.OpenAsync();

        DbTransaction transaction = await connection.BeginTransactionAsync();
        command.Transaction = transaction as SqlTransaction;

        try
        {
            command.Parameters.Clear();
            command.CommandText = "SELECT IdProduct FROM Product WHERE IdProduct = @IdProduct";
            command.Parameters.AddWithValue("@IdProduct", request.IdProduct);

            var isIdProductExist = await command.ExecuteScalarAsync();
            if (isIdProductExist == null)
                throw new Exception("Product not found");

            command.Parameters.Clear();
            command.CommandText = "SELECT IdWarehouse FROM Warehouse WHERE IdWarehouse = @IdWarehouse";
            command.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);
            var isIdWarehouseExist = await command.ExecuteScalarAsync();
            if (isIdWarehouseExist == null)
                throw new Exception("Warehouse not found");

            if (request.Amount <= 0)
                throw new Exception("Amount must be positive");

            command.Parameters.Clear();
            command.CommandText = @"SELECT IdOrder FROM [Order] 
                                    WHERE IdProduct = @IdProduct AND Amount = @Amount AND CreatedAt < @CreatedAt";
            command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
            command.Parameters.AddWithValue("@Amount", request.Amount);
            command.Parameters.AddWithValue("@CreatedAt", request.CreatedAt);

            var orderId = await command.ExecuteScalarAsync();
            if (orderId is null)
                throw new Exception("Order not found");

            command.Parameters.Clear();
            command.CommandText = "SELECT 1 FROM Product_Warehouse WHERE IdOrder = @IdOrder";
            command.Parameters.AddWithValue("@IdOrder", orderId);

            var isOrderExist = await command.ExecuteScalarAsync();
            if (isOrderExist != null)
                throw new Exception("Product already exist in warehouse");

            command.Parameters.Clear();
            command.CommandText = "UPDATE [Order] SET FulfilledAt = @Now WHERE IdOrder = @IdOrder";
            command.Parameters.AddWithValue("@Now", DateTime.Now);
            command.Parameters.AddWithValue("@IdOrder", orderId);
            await command.ExecuteNonQueryAsync();

            command.Parameters.Clear();
            command.CommandText = "SELECT Price FROM Product WHERE IdProduct = @IdProduct";
            command.Parameters.AddWithValue("@IdProduct", request.IdProduct);

            var priceObj = await command.ExecuteScalarAsync();
            if (priceObj is null)
                throw new Exception("Product price not found");

            decimal unitPrice = Convert.ToDecimal(priceObj);
            decimal totalPrice = unitPrice * request.Amount;

            command.Parameters.Clear();
            command.CommandText = @"
                INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
                VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, @CreatedAt);
                SELECT SCOPE_IDENTITY();";

            command.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);
            command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
            command.Parameters.AddWithValue("@IdOrder", orderId);
            command.Parameters.AddWithValue("@Amount", request.Amount);
            command.Parameters.AddWithValue("@Price", totalPrice);
            command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

            var insertedIdObj = await command.ExecuteScalarAsync();
            if (insertedIdObj is null)
                throw new Exception("Insert failed");

            await transaction.CommitAsync();
            return Convert.ToInt32(insertedIdObj);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task ProcedureAsync(WareHouseRequest request)
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand("AddProductToWarehouse", connection);

        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
        command.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);
        command.Parameters.AddWithValue("@Amount", request.Amount);
        command.Parameters.AddWithValue("@CreatedAt", request.CreatedAt);

        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();
    }
}
