using System.Data;
using System.Data.SqlClient;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WebApplication
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.Configure<KestrelServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });

            // using (var connection = new SqlConnection(_configuration.GetConnectionString("ConnectionString")))
            // {
            //     connection.Open();
            //
            //     using (var command = new SqlCommand("INSERT INTO content(id, body) VALUES(@id, @body)", connection))
            //     {
            //         var random = new Random();
            //         var buffer = new byte[1024 * 1024 * 10];
            //         random.NextBytes(buffer);
            //         
            //         command.Parameters.AddWithValue("@id", 1);
            //         command.Parameters.AddWithValue("@body", buffer);
            //
            //         command.ExecuteNonQuery();
            //     }
            // }
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }

    [Route("[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public TestController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        
        public async Task Get()
        {
            HttpContext.Response.Headers["Content-Type"] = "application/zip";
            
            await using (var connection = new SqlConnection(_configuration.GetConnectionString("ConnectionString")))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand("SELECT body FROM content WHERE id = 1", connection))
                {
                    await using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess))
                    {
                        await reader.ReadAsync();

                        using (var stream = reader.GetStream(0))
                        {
                            await HttpContext.Response.StartAsync();
                            using (var zip = new ZipArchive(HttpContext.Response.Body, ZipArchiveMode.Create))
                            {
                                using (var entry = zip.CreateEntry("test").Open())
                                {
                                    await stream.CopyToAsync(entry);
                                }
                            }
                            await HttpContext.Response.CompleteAsync();    
                        }
                    }
                }
            }
        }
    }
}