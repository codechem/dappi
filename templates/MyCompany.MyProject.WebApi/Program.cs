using MyCompany.MyProject.WebApi.Data;

internal partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddOpenApi();
        builder.Services.AddDappi<AppDbContext>(builder.Configuration);

        var app = builder.Build();

        app.UseDappi<AppDbContext>();
        app.UseHttpsRedirection();
        app.MapControllers();

        app.Run();
    }
}