using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Blacksmith_Store
{
    public struct StockDetails
    {
        public long StockId { get; set; }
        public decimal BasePrice { get; set; }
    }

    public static class DatabaseHelper
    {
        private const string ConnectionString = @"Data Source=D:\Все для навчання\4_Курс\Blacksmith_Store\Blacksmith_Store\bin\Debug\Blacksmith_StoreBD";

        public static SqliteConnection GetConnection()
        {
            return new SqliteConnection(ConnectionString);
        }

        public static long GetOrCreateId(string tableName, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return 0;

            using (var connection = GetConnection())
            {
                connection.Open();
                long id = 0;

                string idColumn;


                if (tableName.Equals("categories", StringComparison.OrdinalIgnoreCase))
                {
                    idColumn = "category_id";
                }
                else if (tableName.Equals("sizes", StringComparison.OrdinalIgnoreCase))
                {
                    idColumn = "size_id";
                }
                else if (tableName.Equals("product_subtypes", StringComparison.OrdinalIgnoreCase))
                {
                    idColumn = "subtype_id";
                }
                else if (tableName.Equals("brands", StringComparison.OrdinalIgnoreCase))
                {
                    idColumn = "brand_id";
                }
                else if (tableName.Equals("colors", StringComparison.OrdinalIgnoreCase))
                {
                    idColumn = "color_id";
                }
                else
                {
                    idColumn = tableName.TrimEnd('s') + "_id";
                }

                string selectSql = $"SELECT {idColumn} FROM {tableName} WHERE name = @Name COLLATE NOCASE";

                using (var selectCommand = new SqliteCommand(selectSql, connection))
                {
                    selectCommand.Parameters.AddWithValue("@Name", name);

                    object result = selectCommand.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        return Convert.ToInt64(result);
                    }
                }

                try
                {
                    string insertSql = $"INSERT INTO {tableName} (name) VALUES (@Name)";
                    using (var insertCommand = new SqliteCommand(insertSql, connection))
                    {
                        insertCommand.Parameters.AddWithValue("@Name", name);
                        insertCommand.ExecuteNonQuery();

                        insertCommand.CommandText = "SELECT last_insert_rowid()";
                        id = (long)insertCommand.ExecuteScalar();
                    }
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
                {
                    using (var selectCommand = new SqliteCommand(selectSql, connection))
                    {
                        selectCommand.Parameters.AddWithValue("@Name", name);
                        object result = selectCommand.ExecuteScalar();
                        if (result != null && result != DBNull.Value) return Convert.ToInt64(result);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка при створенні ID в таблиці {tableName}: {ex.Message}", "Помилка БД", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                return id;
            }
        }

        public static StockDetails GetStockDetails(int productId, string colorName, float? sizeValue, SqliteConnection connection, SqliteTransaction transaction)
        {
            string sql = @"
                SELECT
                    T1.stock_id,
                    T2.base_price
                FROM stock AS T1
                INNER JOIN products AS T2 ON T1.product_id = T2.product_id
                INNER JOIN colors AS T3 ON T1.color_id = T3.color_id
                LEFT JOIN sizes AS T4 ON T1.size_id = T4.size_id
                WHERE T1.product_id = @ProductId
                  AND T3.name = @ColorName COLLATE NOCASE
                  AND (T4.value = @SizeValue OR (@SizeValue IS NULL AND T4.value IS NULL))";

            using (var cmd = new SqliteCommand(sql, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@ProductId", productId);
                cmd.Parameters.AddWithValue("@ColorName", colorName);

                object sizeParam = sizeValue.HasValue ? (object)sizeValue.Value : DBNull.Value;
                cmd.Parameters.AddWithValue("@SizeValue", sizeParam);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        long stockId = reader.IsDBNull(reader.GetOrdinal("stock_id")) ? 0 : reader.GetInt64(reader.GetOrdinal("stock_id"));
                        decimal basePrice = reader.IsDBNull(reader.GetOrdinal("base_price")) ? 0 : Convert.ToDecimal(reader["base_price"]);

                        return new StockDetails
                        {
                            StockId = stockId,
                            BasePrice = basePrice
                        };
                    }
                }
            }
            return new StockDetails { StockId = 0, BasePrice = 0 };
        }
    }
}