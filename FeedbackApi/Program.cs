using FeedbackApi;
using Microsoft.Azure.Cosmos;
using System.Configuration;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<CosmosClient>(new CosmosClient(
    accountEndpoint: builder.Configuration.GetConnectionString("CosmosEndpoint"),
    authKeyOrResourceToken: builder.Configuration.GetConnectionString("CosmosKey")!, 
    clientOptions: new CosmosClientOptions(){ ConnectionMode = ConnectionMode.Gateway }
    ));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
}).WithName("GetWeatherForecast")
    .WithOpenApi();

app.MapPost("/feedback", async (Feedback feedback) =>
{
    if (feedback is null)
    {
        return StatusCodes.Status400BadRequest;
    }

    feedback.Created = DateTime.UtcNow;

    // TODO: use enums
    if ((feedback.Score < 1) || (feedback.Score > 5))
    {
        return StatusCodes.Status406NotAcceptable;
    }

    try
    {
        var client = app.Services.GetService<CosmosClient>();
        var container = client.GetContainer("feedback", "feedback");
        await container.CreateItemAsync(feedback, new PartitionKey(feedback.Score));
    }
    catch (Exception e)
    {
        Console.WriteLine($"Application error: {e.Message}");
        return StatusCodes.Status500InternalServerError;
    }

    Console.WriteLine(JsonSerializer.Serialize(feedback));



    return StatusCodes.Status202Accepted;
}).WithName("SendFeedback")
    .Produces(StatusCodes.Status202Accepted)
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status500InternalServerError)
    .ProducesValidationProblem(StatusCodes.Status406NotAcceptable)
    .WithOpenApi();

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
