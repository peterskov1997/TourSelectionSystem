using RabbitMQ.Client;
using System.Text;

namespace TourSelectionFront.Services
{
    public class RabbitMqService
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public RabbitMqService()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            //Declare the Dead Letter Exchange (DLX)
            _channel.ExchangeDeclare(exchange: "dead_letter_exchange", type: ExchangeType.Topic, durable: true);

            //Declare queues for dead letters and invalid letters
            _channel.QueueDeclare(queue: "dead_letter_queue", 
                                  durable: true, 
                                  exclusive: false, 
                                  autoDelete: false, 
                                  arguments: null);

            _channel.QueueDeclare(queue: "invalid_letter_queue", 
                                  durable: true, 
                                  exclusive: false, 
                                  autoDelete: false, 
                                  arguments: null);

            //Bind queues to the dead letter exchange with appropriate routing keys
            _channel.QueueBind(queue: "dead_letter_queue", exchange: "dead_letter_exchange", routingKey: "dead.#");
            _channel.QueueBind(queue: "invalid_letter_queue", exchange: "dead_letter_exchange", routingKey: "invalid.#");
        }

        public void SendMessage(string message, string routingKey)
        {
            _channel.ExchangeDeclare(exchange: "topic_booking", ExchangeType.Topic, durable:true);

            // Declare a main queue with dead-letter exchange configuration
            var queueArguments = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", "dead_letter_exchange" },  // Dead Letter Exchange
                { "x-dead-letter-routing-key", "dead." }  // Dead letter routing key
            };

            _channel.QueueDeclare(queue: "main_queue", 
                                  durable: true, 
                                  exclusive: false, 
                                  autoDelete: false, 
                                  arguments: queueArguments);

            var body = Encoding.UTF8.GetBytes(message);

            //Set messages itself to be persistent
            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;

            _channel.BasicPublish(exchange: "topic_booking",
                                  routingKey: routingKey,
                                  basicProperties: properties,
                                  body: body);
        }

        public void Dispose()
        {
            _channel.Close();
            _connection.Close();
        }
    }
}
