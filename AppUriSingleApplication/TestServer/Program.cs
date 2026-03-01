var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.MapGet("/", () => {
    Guid uid = Guid.NewGuid();
    Console.WriteLine($"New token generated {uid.ToString()}");
    return Results.Redirect($"app-uri-single-application://callback?token={uid.ToString()}");
});

app.Run();