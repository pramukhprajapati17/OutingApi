using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using Repositories.Interfaces;
using Repositories.Models;

namespace Repositories.Implementations
{
    public class UserRepository : IUserInterface
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
                await _conn.CloseAsync();
                await _conn.OpenAsync();

                var qrycheck = "SELECT * FROM t_users WHERE c_email = @Email";
                using (NpgsqlCommand cmdcheck = new NpgsqlCommand(qrycheck, _conn)) // Use 'using' for NpgsqlCommand
                {
                    cmdcheck.Parameters.AddWithValue("Email", user.Email);

                    var count = await cmdcheck.ExecuteReaderAsync();

                    if (count.HasRows)
                    {
                        System.Console.WriteLine("User with this email already exists.");
                        return -1; // User already exists
                    }
                }
                await _conn.CloseAsync(); // Close after checking
                await _conn.OpenAsync(); // Reopen for insertion
                var qry = "INSERT INTO t_users (c_full_name, c_email, c_password_hash, c_role, c_is_active) VALUES (@username, @email, @password, @role, @is_active)";
                using (NpgsqlCommand cmd = new NpgsqlCommand(qry, _conn)) // Use 'using' for NpgsqlCommand
                {
                    cmd.Parameters.AddWithValue("username", user.Username);
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
}