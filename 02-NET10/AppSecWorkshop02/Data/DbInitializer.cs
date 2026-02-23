using Microsoft.Data.Sqlite;

namespace AppSecWorkshop02.Data;

public static class DbInitializer
{
    public static void Initialize(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS users (
                id INTEGER PRIMARY KEY,
                username TEXT NOT NULL,
                role TEXT NOT NULL
            );

            DELETE FROM users;

            INSERT INTO users (username, role) VALUES
            ('alice', 'User'),
            ('bob', 'Admin'),
            ('charlie', 'User');
            """;
        command.ExecuteNonQuery();
    }
}
