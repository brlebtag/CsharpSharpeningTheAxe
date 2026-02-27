var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.MapGet("/", () => Results.Redirect($"app-uri-single-application://callback?token={Guid.NewGuid().ToString()}"));

app.Run();