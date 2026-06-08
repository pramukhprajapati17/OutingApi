using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Repositories.Interfaces;
using Repositories.Models;
using StackExchange.Redis;

namespace Repositories.Implementations
{
    public class RedisService : IRedisService, IDisposable
    {
        private readonly ConnectionMultiplexer _muxer;
        private readonly IDatabase _db;

        // Simple constructor accepting a single connection string (convenience)
        public RedisService(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            _muxer = ConnectionMultiplexer.Connect(connectionString);
            _db = _muxer.GetDatabase();
        }

        public RedisService(IConfiguration config)
        {
            var section = config.GetSection("Redis");

            // Allow providing a single connection string (e.g. REDIS_URL or Redis:ConnectionString)
            var connectionString = section.GetValue<string>("ConnectionString");
            if (string.IsNullOrEmpty(connectionString))
            {
                var host = section.GetValue<string>("Host");
                var port = section.GetValue<int>("Port");
                var password = section.GetValue<string>("Password");
                var ssl = section.GetValue<bool?>("Ssl") ?? true;

                if (string.IsNullOrEmpty(host) || port == 0)
                    throw new InvalidOperationException("Redis configuration is missing or invalid.");

                // Use ConfigurationOptions to explicitly set SslHost (SNI) and timeouts.
                var options = new ConfigurationOptions
                {
                    AbortOnConnectFail = false,
                    Ssl = ssl,
                    SslHost = host,
                    Password = string.IsNullOrEmpty(password) ? null : password,
                    ConnectTimeout = 10000,
                    SyncTimeout = 10000
                };
                options.EndPoints.Add(host, port);

                try
                {
                    _muxer = ConnectionMultiplexer.Connect(options);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to connect to Redis at {host}:{port} (ssl={ssl}). See inner exception for details.", ex);
                }
            }
            else
            {
                // Parse a single connection string. Accept common redis URI formats.
                try
                {
                    var opts = ConfigurationOptions.Parse(connectionString);
                    // ensure sane defaults
                    opts.AbortOnConnectFail = false;
                    opts.ConnectTimeout = opts.ConnectTimeout == 0 ? 10000 : opts.ConnectTimeout;
                    opts.SyncTimeout = opts.SyncTimeout == 0 ? 10000 : opts.SyncTimeout;
                    // If no SslHost set, try to infer from endpoints
                    if (string.IsNullOrEmpty(opts.SslHost) && opts.EndPoints.Count > 0)
                    {
                        opts.SslHost = opts.EndPoints[0].ToString();
                    }
                    _muxer = ConnectionMultiplexer.Connect(opts);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to parse/connect to Redis using ConnectionString. See inner exception for details.", ex);
                }
            }
            _db = _muxer.GetDatabase();
        }

        private static string Key(string email) => $"otp:{{{email.ToLower()}}}";

        public async Task SetPendingRegistrationAsync(string email, UserRegister user, string otp, TimeSpan ttl)
        {
            var payload = JsonSerializer.Serialize(new PendingRegistration { User = user, Otp = otp });
            await _db.StringSetAsync(Key(email), payload, ttl);
        }

        // Simple helpers mirroring the user's example
        public void SetString(string key, string value)
        {
            _db.StringSet(key, value);
        }

        public string? GetString(string key)
        {
            return _db.StringGet(key);
        }

        public async Task SetStringAsync(string key, string value)
        {
            await _db.StringSetAsync(key, value);
        }

        public async Task<string?> GetStringAsync(string key)
        {
            var v = await _db.StringGetAsync(key);
            return v.IsNullOrEmpty ? null : v.ToString();
        }

        public async Task<(UserRegister? user, string? otp)> GetPendingRegistrationAsync(string email)
        {
            var v = await _db.StringGetAsync(Key(email));
            if (v.IsNullOrEmpty) return (null, null);
            try
            {
                var pr = JsonSerializer.Deserialize<PendingRegistration>(v.ToString());
                return (pr?.User, pr?.Otp);
            }
            catch
            {
                return (null, null);
            }
        }

        public async Task RemovePendingRegistrationAsync(string email)
        {
            await _db.KeyDeleteAsync(Key(email));
        }

        public async Task<bool> PingAsync()
        {
            try
            {
                var pong = await _db.PingAsync();
                return pong.TotalMilliseconds >= 0;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _muxer?.Dispose();
        }
    }
}
