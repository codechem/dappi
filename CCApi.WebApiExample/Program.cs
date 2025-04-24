using CCApi.Extensions.DependencyInjection;
using CCApi.WebApiExample.Data;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization();

builder.Services.AddDappi<AppDbContext>(builder.Configuration);

builder.Services.AddDappiAuthentication<DappiUser, DappiRole, AppDbContext>(builder.Configuration);

var app = builder.Build();

app.UseDappi<AppDbContext>();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await ServiceExtensions.SeedRolesAndUsersAsync<DappiUser, DappiRole>(app.Services);

app.Run();
