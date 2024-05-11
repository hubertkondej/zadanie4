using APBD_Task_6.Models;
using System.Data.SqlClient;

namespace Zadanie5.Services
{
    public class WarehouseService : IWarehouseService
    {
        private readonly IConfiguration _configuration;

        public WarehouseService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task <int> AddProduct(ProductWarehouse productWarehouse)
        {
            var connectionString = _configuration.GetConnectionString("Database");
            using var connection = new SqlConnection(connectionString);
            using var cmd = new SqlCommand();

            cmd.Connection = connection;
            await connection.OpenAsync();
            cmd.CommandText = "SELECT  TOP 1 [Order].IdOrder From [Order]"+
                "LEFT JOIN Product_WareHouse ON [Order].IdOrder=Product_Warehouse .IdOrder" +
                "ADN [Order].Amount= @Amount" +
                "ADN Product_Warehouse.IdProductWareHouse IS NULL" +
                "AND [Order].CreateAt< @CreateAt";
            cmd.Parameters.AddWithValue("IdProduct", productWarehouse.IdWarehouse);
            cmd.Parameters.AddWithValue("Amount", productWarehouse.Amount);
            cmd.Parameters.AddWithValue("CreatedAt", productWarehouse.CreatedAt);
            var reader=await cmd.ExecuteReaderAsync();
            if (!reader.HasRows) throw new Exception();
            await reader.ReadAsync();
            int idOrder = int.Parse(reader["idOrder"].ToString());
            await reader.CloseAsync();
           // await cmd.ExecuteNonQueryAsync();
            cmd.CommandText = "Select Price From Product Where IdProcudt=@IdProduct";
            cmd.Parameters.AddWithValue("IdProduct", productWarehouse.IdProduct);

            reader = await cmd.ExecuteReaderAsync();

            if(!reader.HasRows)throw new Exception();   
            await reader.ReadAsync();
            double price = double.Parse(reader["Price"].ToString());
            await reader.CloseAsync();
            cmd.Parameters.Clear();

            cmd.CommandText = "Select IdWarehouse From Warehouse WHERE IdWarehouse=@IdWarehouse";
            cmd.Parameters.AddWithValue("IdWarehouse",productWarehouse.IdWarehouse);
            reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows) throw new Exception();
            await reader.CloseAsync();
            cmd.Parameters.Clear();

            var transaction= (SqlTransaction)await connection.BeginTransactionAsync();
            cmd.Transaction = transaction;

            try
            {
                cmd.CommandText = "Update [Order] SET FulfilledAt=@CreatedAt Where IdOrder=@IdOrder";
                cmd.Parameters.AddWithValue("CreatedAt", productWarehouse.CreatedAt);
                cmd.Parameters.AddWithValue("IdOrder", idOrder);
                int rowsUpdated= await cmd.ExecuteNonQueryAsync();

                if(rowsUpdated > 1)throw new Exception();
                cmd.Parameters.Clear();
                cmd.CommandText = "Insert Inot Product_Warehouse (IdWarehouse, IdProduct, IdOrder,Amount ,Price,CreatedAt" +
                    $"VALUES(@idWarehouse, @IdProduct, @IdOrder,@Amount*{price},@CreatedAt)";
                cmd.Parameters.AddWithValue("CreatedAt", productWarehouse.IdWarehouse);
                cmd.Parameters.AddWithValue("IdProduct", productWarehouse.IdProduct);
                cmd.Parameters.AddWithValue("idOrder", idOrder);
                cmd.Parameters.AddWithValue("Amount", productWarehouse.Amount);
                cmd.Parameters.AddWithValue("CreatedAt", productWarehouse.CreatedAt);

                int rowsInserted = await cmd.ExecuteNonQueryAsync();
                if(rowsInserted > 1) throw new Exception();     



                await reader.CloseAsync();  
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw new Exception();  
            }
            cmd.Parameters.Clear();
            cmd.CommandText = "SELECT TOP 1 IdProductWarehouse From Product_Warehouse ORDER BY IdProductWarehouse";

            reader = await cmd.ExecuteReaderAsync();

            int idProductWarehouse = int.Parse(reader["IdProductWarehouse"].ToString());
            await reader.CloseAsync();

            await connection.CloseAsync();

            return idProductWarehouse;
        }
    }
}
