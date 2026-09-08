using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UserManagement.Data;
using Westwind.AspNetCore.Markdown;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddDataAccess(builder.Configuration)
    .AddDomainServices()
    .AddMarkdown()
    .AddControllersWithViews();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<DataContext>().Database.Migrate();
}

app.UseMarkdown();

app.UseHsts();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

app.MapDefaultControllerRoute();

app.Run();
