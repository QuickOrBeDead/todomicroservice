namespace MessageQueue
{
    using System.Text;
    using System.Text.Json;

    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;

    public interface IMessageQueueConsumerService<out TModel> : IDisposable
    {
        void ConsumeMessage(Action<TModel> consumeAction);
    }

    public class RabbitMqMessageQueueConsumerService<TModel> : IMessageQueueConsumerService<TModel>
    {
        private readonly string _host;

        private readonly string _userName;

        private readonly string _password;

        private readonly string _exchange;

        private readonly string _queue;

        private bool _disposed;

        private IConnection? _connection;

        private IModel? _channel;

        public RabbitMqMessageQueueConsumerService(string host, string userName, string password, string exchange, string queue)
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

            if (string.IsNullOrWhiteSpace(queue))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(queue));
            }

            if (string.IsNullOrWhiteSpace(host))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(host));
            }

            _host = host;
            _userName = userName;
            _password = password;
            _exchange = exchange;
            _queue = queue;
        }

        public void ConsumeMessage(Action<TModel> consumeAction)
        {
            if (consumeAction == null)
            {
                throw new ArgumentNullException(nameof(consumeAction));
            }

            // TODO: reconnect ??
            var factory = new ConnectionFactory { HostName = _host, UserName = _userName, Password = _password };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(_exchange, "fanout", true, false, null);
            _channel.QueueDeclare(_queue, true, false, false, null);
            _channel.QueueBind(_queue, _exchange, string.Empty);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (_, e) =>
            {
                var model = JsonSerializer.Deserialize<TModel>(Encoding.UTF8.GetString(e.Body.Span));
                if (model != null)
                {
                    consumeAction(model);
                }
            };
            _channel.BasicConsume(_queue, true, consumer);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                }

                _channel?.Dispose();
                _connection?.Dispose();
                _disposed = true;
            }
        }

        ~RabbitMqMessageQueueConsumerService()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}