using CsvQueueConsumerLibrary;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductServiceLibrary.Interfaces;
using ProductServiceLibrary.Services;


var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build();

//Set up Dependency Injection
var serviceProvider = new ServiceCollection()
       .Configure<RabbitMqConfiguration>(configuration.GetSection("RabbitMq")) // Bind RabbitMq settings
   .AddScoped<IProductService>(provider =>
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        return new ProductService(connectionString);
    })
    .AddSingleton<CsvQueueConsumer>()
    .BuildServiceProvider();

// Resolve services and start consuming messages
var consumer = serviceProvider.GetService<CsvQueueConsumer>();
try
{
    consumer.StartConsuming();
}
catch (Exception ex)
{
    Console.WriteLine($"An error occurred: {ex.Message}");
}
