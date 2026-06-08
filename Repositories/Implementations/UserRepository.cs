using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using Repositories.Interfaces;
using Repositories.Models;

namespace Repositories.Implementations
{
    public partial class UserRepository : IUserInterface
    {
        private readonly NpgsqlConnection _conn;
        public UserRepository(NpgsqlConnection conn)
        {
            _conn = conn;   
        }

        public async Task<int> RegisterUser(UserRegister user)
        {
            try
            {
                if (_conn.State != System.Data.ConnectionState.Open)
                    await _conn.OpenAsync();

                var qrycheck = "SELECT EXISTS(SELECT 1 FROM t_users WHERE c_email = @Email)";
                using (NpgsqlCommand cmdcheck = new NpgsqlCommand(qrycheck, _conn)) // Use 'using' for NpgsqlCommand
                {
                    cmdcheck.Parameters.AddWithValue("Email", user.Email);
                    var exists = (bool?)await cmdcheck.ExecuteScalarAsync() ?? false;
                    
                    if (exists)
                    {
                        System.Console.WriteLine("User with this email already exists.");
                        return -1; // User already exists
                    }
                }

                var qry = "INSERT INTO t_users (c_full_name, c_email, c_password_hash, c_role, c_is_active) VALUES (@username, @email, @password, @role, @is_active)";
                using (NpgsqlCommand cmd = new NpgsqlCommand(qry, _conn)) // Use 'using' for NpgsqlCommand
                {
                    cmd.Parameters.AddWithValue("username", (object?)user.Username ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("email", user.Email);
                    cmd.Parameters.AddWithValue("password", user.Password); // Note: In production, hash the password!
                    cmd.Parameters.AddWithValue("role", user.Role);
                    cmd.Parameters.AddWithValue("is_active", user.IsActive);
                    await cmd.ExecuteNonQueryAsync();
                }
                return 1; // Success    
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error registering user: {ex.Message}");
                return 0; // Error
            }
            finally
            {
                if (_conn.State == System.Data.ConnectionState.Open) // Only close if the connection was successfully opened
                {
                    await _conn.CloseAsync();
                }
            }
        }
    
    }

    // Partial extension implementing lookup by email
    public partial class UserRepository
    {
        public async Task<UserRegister?> GetUserByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            try
            {
                if (_conn.State != System.Data.ConnectionState.Open)
                    await _conn.OpenAsync();

                var qry = "SELECT c_full_name, c_email, c_password_hash, c_role, c_is_active FROM t_users WHERE c_email = @Email LIMIT 1";
                using var cmd = new NpgsqlCommand(qry, _conn);
                cmd.Parameters.AddWithValue("Email", email);
                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) return null;
                var u = new UserRegister
                {
                    Username = reader.IsDBNull(0) ? null : reader.GetString(0),
                    Email = reader.IsDBNull(1) ? null : reader.GetString(1),
                    Password = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Role = reader.IsDBNull(3) ? null : reader.GetString(3),
                    IsActive = !reader.IsDBNull(4) && reader.GetBoolean(4)
                };
                return u;
            }
            catch
            {
                return null;
            }
            finally
            {
                if (_conn.State == System.Data.ConnectionState.Open)
                    await _conn.CloseAsync();
            }
        }
    }
}