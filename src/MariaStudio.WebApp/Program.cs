using MariaStudio.WebApp.Services;
using MySqlConnector;

namespace MariaStudio.WebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();

            var connectionString = new MySqlConnectionStringBuilder(
                builder.Configuration.GetConnectionString("MariaDb")
                    ?? throw new InvalidOperationException("ConnectionStrings:MariaDb is required."))
            {
                AllowLoadLocalInfile = false,
                AllowUserVariables = true
            };
            builder.Services.AddMySqlDataSource(connectionString.ConnectionString);
            builder.Services.AddScoped<QueryService>();
            builder.Services.AddScoped<SchemaService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Query}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
