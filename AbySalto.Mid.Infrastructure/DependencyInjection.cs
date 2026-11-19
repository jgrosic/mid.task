using AbySalto.Mid.Application.Favorites;
using AbySalto.Mid.Application.Products;
using AbySalto.Mid.Infrastructure.Favorites;
using AbySalto.Mid.Infrastructure.Identity;
using AbySalto.Mid.Infrastructure.Persistence;
using AbySalto.Mid.Infrastructure.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AbySalto.Mid.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services,
            IConfiguration configuration)
        {
            return services
                .AddDatabase(configuration)
                .AddIdentity()
                .AddServices();
        }

        private static IServiceCollection AddServices(this IServiceCollection services)
        {
            // obsolete: switched to using AddIdentityApiEndpoints<ApplicationUser>
            //services.AddScoped<IAuthService, IdentityAuthService>();

            services.AddHttpClient<IProductCatalog, DummyJsonProductClient>(client =>
            {
                client.BaseAddress = new Uri("https://dummyjson.com/");
                client.Timeout = TimeSpan.FromSeconds(10);
            });

            services.AddScoped<IFavoriteService, FavoritesService>();

            return services;
        }

        private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            string connString = configuration.GetConnectionString("DefaultConnection")
                                ?? throw new InvalidOperationException("Database connection string is required.");

            services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connString));

            return services;
        }

        private static IServiceCollection AddIdentity(this IServiceCollection services)
        {
            // AddIdentityApiEndpoints already adds AddIdentityCore

            services
                .AddIdentityApiEndpoints<ApplicationUser>(options =>
                {
                    options.User.RequireUniqueEmail = true;
                    options.Password.RequiredLength = 8;
                })
                .AddEntityFrameworkStores<AppDbContext>();

            //services.ConfigureApplicationCookie(options =>
            //{
            //    options.Events.OnRedirectToLogin = context =>
            //    {
            //        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            //        return Task.CompletedTask;
            //    };
            //});

            return services;
        }
    }
}
