using CCApi.WebApiExample.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection")));

builder.Services.AddTransient<IWeatherService, WeatherService>();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// worth mentioning in blogpost
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// app.AddMinimalApiControllers();
// app.MapPost("/items", async (Item item, AppDbContext dbContext) =>
// {
//     dbContext.Items.Add(item);
//     await dbContext.SaveChangesAsync();
//     return TypedResults.Created($"/items/{item.Id}", item);
// });

app.MapControllers();
app.Run();