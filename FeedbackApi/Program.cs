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

builder.Services.AddCors();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(b => b
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader()
);

app.UseHttpsRedirection();

app.MapGet("/feedbackStatus", async () =>
    {
        // https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/quickstart-dotnet?tabs=azure-portal%2Cwindows%2Cpasswordless%2Csign-in-azure-cli#query-items
        try
        {
            var client = app.Services.GetService<CosmosClient>();
            var container = client.GetContainer("feedback", "feedback");



            var count = 0;

            var query = new QueryDefinition(
                query: "SELECT * FROM feedback"
                // query: "SELECT * FROM feedback f WHERE f.categoryId = @categoryId"
            );



            using FeedIterator<Feedback> feed = container.GetItemQueryIterator<Feedback>(
                queryDefinition: query
            );

            if (feed.HasMoreResults)
            {
                FeedResponse<Feedback> response = await feed.ReadNextAsync();

                count = response.Count;
                
            }

            return new { Count = count };
        }
        catch (Exception e)
        {
            Console.WriteLine($"Application error: {e.Message}");
        }



        return new { Count = -1 };



    }).WithName("GetFeedbackStatus")
    .WithOpenApi();

app.MapPost("/feedback", async (Feedback feedback) =>
{
    if (feedback is null)
    {
        return StatusCodes.Status400BadRequest;
    }

    feedback.Created = DateTime.UtcNow;
    feedback.id = Guid.NewGuid().ToString();

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
