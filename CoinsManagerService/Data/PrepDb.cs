using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace CoinsManagerService.Data
{
    public static class PrepDb
    {
        public static void PrepareDatabase(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetService<AppDbContext>();
                try
                {
                    context.Database.Migrate();
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"--> Couldn't apply db migrations: {ex.Message}");
                }
            }
        }
    }
}
