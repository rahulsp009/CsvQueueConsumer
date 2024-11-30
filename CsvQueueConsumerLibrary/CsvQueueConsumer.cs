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

                InsertProductIntoDatabase(message);
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
            }
            else
            {
                Console.WriteLine("Invalid message format.");
            }
        }
    }
}
