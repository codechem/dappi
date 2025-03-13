using CCApi.WebApiExample.Data;
using CCApi.WebApiExample.Interfaces;
using CCApi.WebApiExample.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection")));

builder.Services.AddTransient<IWeatherService, WeatherService>();

// Add controllers and other services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

#if DEBUG
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("Toolkit", new OpenApiInfo { Title = "Toolkit API", Version = "v1" });
    c.SwaggerDoc("Default", new OpenApiInfo { Title = "Default API", Version = "v1" });

    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        if (apiDesc.GroupName == null) return docName == "Default";
        return docName == apiDesc.GroupName;
    });

    c.TagActionsBy(api => api.GroupName ?? api.ActionDescriptor.RouteValues["controller"]);
});
#endif

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy => policy.WithOrigins("http://localhost:4200")
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors("AllowAngularApp");

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/Toolkit/swagger.json", "Toolkit API v1");
        c.SwaggerEndpoint("/swagger/Default/swagger.json", "Default API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
