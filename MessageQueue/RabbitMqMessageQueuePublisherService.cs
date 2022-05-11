namespace MessageQueue
{
    using System.Diagnostics.CodeAnalysis;
    using System.Text;
    using System.Text.Json;

    using RabbitMQ.Client;

    public interface IMessageQueueService
    {
        void PublishMessage<TModel>([DisallowNull] TModel model);
    }

    public class RabbitMqMessageQueuePublisherService : IMessageQueueService
    {
        private readonly string _host;

        private readonly string _userName;

        private readonly string _password;

        private readonly string _exchange;

        public RabbitMqMessageQueuePublisherService(string host, string userName, string password, string exchange)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(userName));
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(password));
            }

            if (string.IsNullOrWhiteSpace(exchange))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(exchange));
            }

            if (string.IsNullOrWhiteSpace(host))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(host));
            }

            _host = host;
            _userName = userName;
            _password = password;
            _exchange = exchange;
        }

        public void PublishMessage<TModel>([DisallowNull] TModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var factory = new ConnectionFactory { HostName = _host, UserName = _userName, Password = _password };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare("unrouted", "fanout", true, false, null);
                channel.QueueDeclare("unrouted", true, false, false, null);
                channel.QueueBind("unrouted", "unrouted", string.Empty);

                channel.ExchangeDeclare(
                    _exchange,
                    "fanout",
                    true,
                    false,
                    new Dictionary<string, object> { { "alternate-exchange", "unrouted" } });
                channel.BasicPublish(
                    _exchange,
                    string.Empty,
                    null,
                    Encoding.UTF8.GetBytes(JsonSerializer.Serialize(model)));
            }
        }
    }
}
