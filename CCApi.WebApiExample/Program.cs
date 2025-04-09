using System.Text.Json.Serialization;
using CCApi.WebApiExample.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDappi<AppDbContext>(builder.Configuration);

var app = builder.Build();

app.UseDappi<AppDbContext>();

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
