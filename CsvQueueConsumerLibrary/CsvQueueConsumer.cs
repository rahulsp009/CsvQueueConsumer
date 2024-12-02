using Microsoft.Extensions.Options;
using ModelsLibrary.Models;
using ProductServiceLibrary.Interfaces;
using ProductServiceLibrary.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvQueueConsumerLibrary
{
    public class CsvQueueConsumer
    {
        private readonly IProductService _productService;
        private readonly IConnection _connection;
        private readonly HashSet<int> _processedFileIds = new HashSet<int>();
        private readonly Dictionary<int, int> _messageCounts = new Dictionary<int, int>();
        private int _currentFileId;

        public CsvQueueConsumer(IProductService productService, IOptions<RabbitMqConfiguration> rabbitMqConfig)
        {
            _productService = productService;
            var factory = new ConnectionFactory()
            {
                HostName = rabbitMqConfig.Value.HostName,
                UserName = rabbitMqConfig.Value.Username,
                Password = rabbitMqConfig.Value.Password
            };

            _connection = factory.CreateConnection();
        }

        public void StartConsuming()
        {
            using var channel = _connection.CreateModel();

            channel.QueueDeclare(queue: "product_queue", durable: true, exclusive: false, autoDelete: false);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($"Received message: {message}");
                try
                {
                    InsertProductIntoDatabase(message);
                }
                catch (Exception ex)
                {
                    throw;
                }
            };

            channel.BasicConsume(queue: "product_queue", autoAck: true, consumer: consumer);
            Console.WriteLine("Press [enter] to exit.");
            Console.ReadLine();
        }

        private void InsertProductIntoDatabase(string message)
        {
            var parts = message.Split(',');
            if (parts.Length == 5)
            {
                var product = new Product
                {
                    FileId = int.Parse(parts[0]),
                    Name = parts[1],
                    Category = parts[2],
                    Price = decimal.Parse(parts[3]),
                    StockQuantity = int.Parse(parts[4])
                };

                _productService.AddProduct(product);
                Console.WriteLine($"Inserted product: {product.Name}");

                // Count the number of messages for each fileId
                if (!_messageCounts.ContainsKey(product.FileId))
                {
                    _messageCounts[product.FileId] = 0; // Initialize count for new fileId
                }
                _messageCounts[product.FileId]++; // Increment count for current fileId

                // If we are processing a new fileId
                if (_currentFileId != product.FileId)
                {
                    // Check if the previous fileId has been fully processed
                    if ((_currentFileId != 0 && !_processedFileIds.Contains(_currentFileId)) || IsQueueEmpty())
                    {
                        Console.WriteLine($"All messages for fileId {_currentFileId} have been processed.");
                        _processedFileIds.Add(_currentFileId); // Mark as processed
                        _productService.UpdateFileProcessingStatus(_currentFileId);
                    }

                    // Update current fileId
                    _currentFileId = product.FileId;
                    Console.WriteLine($"Processing messages for new fileId: {_currentFileId}");
                }
            }
            else
            {
                Console.WriteLine("Invalid message format.");
            }
        }

        private bool IsQueueEmpty()
        {
            try
            {
                using (var channel = _connection.CreateModel())
                {
                    // Declare the queue passively to check its state
                    var result = channel.QueueDeclarePassive("product_queue");
                    return result.MessageCount == 0; // Returns true if there are no messages in the queue
                }
            }
            catch (RabbitMQ.Client.Exceptions.OperationInterruptedException ex)
            {
                // Handle exception if the queue does not exist or other issues occur
                Console.WriteLine($"Error checking queue: {ex.Message}");
                return false; // Treat as not empty if there's an error
            }
        }
    }
}
