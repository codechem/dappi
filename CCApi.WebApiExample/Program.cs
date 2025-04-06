using CCApi.WebApiExample.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDappi<ApplicationDbContext>(builder.Configuration);

var app = builder.Build();

app.UseDappi<ApplicationDbContext>();

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
