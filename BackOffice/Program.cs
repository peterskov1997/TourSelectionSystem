using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
// See https://aka.ms/new-console-template for more information

var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

// 1. Declare the Dead Letter Exchange and bind queues for dead-letter and invalid messages
channel.ExchangeDeclare(exchange: "dead_letter_exchange", type: ExchangeType.Topic, durable: true);

// Declare queues for dead letters and invalid letters
channel.QueueDeclare(queue: "dead_letter_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);
channel.QueueDeclare(queue: "invalid_letter_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);

// Bind queues to the dead letter exchange with appropriate routing keys
channel.QueueBind(queue: "dead_letter_queue", exchange: "dead_letter_exchange", routingKey: "dead.#");
channel.QueueBind(queue: "invalid_letter_queue", exchange: "dead_letter_exchange", routingKey: "invalid.#");

channel.ExchangeDeclare(exchange: "topic_booking", type: ExchangeType.Topic, durable: true);

// Set arguments for dead-letter routing in the main queue
var queueArguments = new Dictionary<string, object>
{
    { "x-dead-letter-exchange", "dead_letter_exchange" },  // Dead Letter Exchange
    { "x-dead-letter-routing-key", "dead." }  // Dead letter routing key
};

//Declare a server-named queue
var queueName = channel.QueueDeclare().QueueName;


if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: {0} [binding_key.....]",
                            Environment.GetCommandLineArgs()[0]);
    Console.WriteLine(" Press [enter] to exit.");
    Console.ReadLine();
    Environment.ExitCode = 1;
    return;
}

foreach (var bindingKey in args)
{
    channel.QueueBind(queue: queueName,
                      exchange: "topic_booking",
                      routingKey: bindingKey);
}

Console.WriteLine(" [*] Waiting for bookings.");

var consumer = new EventingBasicConsumer(channel);
consumer.Received += (model, ea) =>
{
    byte[] body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    var routingKey = ea.RoutingKey;

    Console.WriteLine($" [x] Received: {routingKey} : {message}");

    try
    {
        // Process the message
        throw new ArgumentException("Testing DeadLetter exchange");
        // If processed successfully, acknowledge it
        channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        Console.WriteLine(" [x] Message processed and acknowledged.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($" [x] Error processing message: {ex.Message}");

        // If an error occurs, send the message to the dead-letter queue
        channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);

        // Optionally, if this message is considered invalid, you could publish it to the invalid letter queue
        var invalidProperties = channel.CreateBasicProperties();
        invalidProperties.Persistent = true;
        var invalidMessageBody = Encoding.UTF8.GetBytes(message);

        channel.BasicPublish(
            exchange: "dead_letter_exchange",
            routingKey: "invalid." + routingKey,  // Use invalid. prefix for invalid messages
            basicProperties: invalidProperties,
            body: invalidMessageBody);
        Console.WriteLine(" [x] Sent message to invalid letter queue.");
    }
};

channel.BasicConsume(queue: queueName,
                     autoAck: false, //Set to false for guaranteed delivery
                     consumer: consumer);

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();
