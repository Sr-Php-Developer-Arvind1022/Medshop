using System;
using Npgsql;

class Program
{
    static void Main()
    {
        var connString = "Host=localhost;Port=5432;Database=Medshop;Username=postgres;Password=yes";
        using var conn = new NpgsqlConnection(connString);
        conn.Open();

        Console.WriteLine("--- MIGRATIONS HISTORY COLUMNS ---");
        using (var cmd = new NpgsqlCommand("SELECT table_name, column_name, data_type FROM information_schema.columns WHERE table_schema='public' AND lower(table_name)='__efmigrationshistory' ORDER BY ordinal_position", conn))
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read()) Console.WriteLine($"{reader.GetString(0)}: {reader.GetString(1)} - {reader.GetString(2)}");
        }

        Console.WriteLine("--- MIGRATIONS HISTORY ROWS ---");
        using (var cmd = new NpgsqlCommand("SELECT * FROM \"__EFMigrationsHistory\"", conn))
        using (var reader = cmd.ExecuteReader())
        {
            var schema = reader.GetSchemaTable();
            if (schema != null)
            {
                foreach (System.Data.DataRow row in schema.Rows)
                {
                    Console.WriteLine($"COL: {row["ColumnName"]} - {row["DataTypeName"]}");
                }
            }
            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    Console.WriteLine($"{reader.GetName(i)} = {reader.GetValue(i)}");
                }
                Console.WriteLine("---");
            }
        }

        Console.WriteLine("--- PRODUCTS TABLE EXISTS ---");
        using (var cmd = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema='public' AND table_name='Products')", conn))
        {
            Console.WriteLine(cmd.ExecuteScalar());
        }

        Console.WriteLine("--- PRODUCTS PK COLUMN TYPE ---");
        using (var cmd = new NpgsqlCommand("SELECT data_type FROM information_schema.columns WHERE table_schema='public' AND table_name='Products' AND column_name='product_id_pk'", conn))
        {
            Console.WriteLine(cmd.ExecuteScalar());
        }
    }
}
