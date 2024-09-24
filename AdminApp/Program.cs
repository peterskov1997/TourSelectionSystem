using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

// See https://aka.ms/new-console-template for more information
var factory = new ConnectionFactory() { HostName = "localhost" };
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

// Declare the Dead Letter Exchange and the dead-letter queues
channel.ExchangeDeclare(exchange: "dead_letter_exchange", type: ExchangeType.Topic, durable: true);
channel.QueueDeclare(queue: "dead_letter_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);
channel.QueueDeclare(queue: "invalid_letter_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);

// Bind the queues to the Dead Letter Exchange
channel.QueueBind(queue: "dead_letter_queue", exchange: "dead_letter_exchange", routingKey: "dead.#");
channel.QueueBind(queue: "invalid_letter_queue", exchange: "dead_letter_exchange", routingKey: "invalid.#");

Console.WriteLine(" [*] Waiting for dead-lettered messages...");

// Create consumers for dead_letter_queue and invalid_letter_queue
var deadLetterConsumer = new EventingBasicConsumer(channel);
deadLetterConsumer.Received += (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    Console.WriteLine($" [x] Dead Letter Received: {message} (Routing Key: {ea.RoutingKey})");

    // Acknowledge the message
    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
};

var invalidLetterConsumer = new EventingBasicConsumer(channel);
invalidLetterConsumer.Received += (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    Console.WriteLine($" [x] Invalid Letter Received: {message} (Routing Key: {ea.RoutingKey})");

    // Acknowledge the message
    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
};

// Consume messages from both queues
channel.BasicConsume(queue: "dead_letter_queue", autoAck: false, consumer: deadLetterConsumer);
channel.BasicConsume(queue: "invalid_letter_queue", autoAck: false, consumer: invalidLetterConsumer);

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();
