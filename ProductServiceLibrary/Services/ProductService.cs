using Dapper;
using Microsoft.Data.SqlClient;
using ModelsLibrary.Models;
using ProductServiceLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductServiceLibrary.Services
{
    public class ProductService : IProductService
    {
        private readonly string _connectionString;
        public ProductService(string connectionString)
        {
            _connectionString = connectionString;
        }
        public async Task<int> AddProduct(Product product)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                string sqlQuery = "INSERT INTO Product (FileId, Name, Price, Category, StockQuantity) VALUES (@FileId, @Name, @Price, @Category, @StockQuantity); SELECT CAST(SCOPE_IDENTITY() as int)";
                try
                {
                    return await db.ExecuteScalarAsync<int>(sqlQuery, product);
                }
                catch (SqlException ex)
                {
                    throw;
                }
            }
        }

        public async Task UpdateFileProcessingStatus(int fileId)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                const string sql = @"
                    UPDATE FileProcessing 
                    SET StatusCode = 2
                    WHERE Id = @Id";

                var parameters = new
                {
                    Id = fileId,
                };

                try
                {
                    await db.ExecuteAsync(sql, parameters);
                }
                catch (SqlException ex)
                {
                    throw;
                }
            }
        }
    }
}
