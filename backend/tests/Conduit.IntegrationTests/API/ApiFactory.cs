using System;
using Conduit.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Conduit.IntegrationTests.Api;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName =
        $"conduit-api-contract-tests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<DbContextOptions<ConduitContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ConduitContext>>();
            services.RemoveAll<ConduitContext>();

            services.AddDbContext<ConduitContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });
        });
    }
}
