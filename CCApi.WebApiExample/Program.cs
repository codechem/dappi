using System.Text.Json.Serialization;
using CCApi.WebApiExample.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDappi<ApplicationDbContext>(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
    });

var app = builder.Build();

app.UseDappi<ApplicationDbContext>();

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
