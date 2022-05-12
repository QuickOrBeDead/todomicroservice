namespace MessageQueue
{
    using System.Text;
    using System.Text.Json;

    using Polly;

    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;

    public interface IMessageQueueConsumerService : IDisposable
    {
        void ConsumeMessage(Action<ReadOnlyMemory<byte>> consumeAction);
    }

    public interface IMessageQueueConsumerService<out TModel> : IMessageQueueConsumerService
    {
        void ConsumeMessage(Action<TModel> consumeAction);
    }

    public class RabbitMqMessageQueueConsumerService : IMessageQueueConsumerService
    {
        private readonly string _host;

        private readonly string _userName;

        private readonly string _password;

        private readonly string _exchange;

        private readonly string _queue;

        private readonly bool _declareQueue;

        private bool _disposed;

        private IConnection? _connection;

        private IModel? _channel;

        private string? _consumerTag;

        public RabbitMqMessageQueueConsumerService(string host, string userName, string password, string exchange, string queue, bool declareQueue = true)
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
            _declareQueue = declareQueue;
        }

        public void ConsumeMessage(Action<ReadOnlyMemory<byte>> consumeAction)
        {
            if (consumeAction == null)
            {
                throw new ArgumentNullException(nameof(consumeAction));
            }

            Policy
                .Handle<Exception>()
                .WaitAndRetry(10, r => TimeSpan.FromSeconds(5))
                .Execute(
                    () =>
                    {
                        var factory = new ConnectionFactory { HostName = _host, UserName = _userName, Password = _password };
                        _connection = factory.CreateConnection();
                        _channel = _connection.CreateModel();

                        _channel.DeclareExchange(_exchange);

                        if (_declareQueue)
                        {
                            _channel.QueueDeclare(_queue, true, false, false, null);
                            _channel.QueueBind(_queue, _exchange, string.Empty);
                        }

                        var consumer = new EventingBasicConsumer(_channel);
                        consumer.Received += (_, e) =>
                        {
                            consumeAction(e.Body);

                            _channel.BasicAck(e.DeliveryTag, false);
                        };
                        _consumerTag = _channel.BasicConsume(_queue, false, consumer);
                    });
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                }

                _channel?.BasicCancel(_consumerTag);
                _channel?.Close(200, "Goodbye");
                _connection?.Close();
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

    public class RabbitMqMessageQueueGenericConsumerService<TModel> : RabbitMqMessageQueueConsumerService, IMessageQueueConsumerService<TModel>
    {
        public RabbitMqMessageQueueGenericConsumerService(string host, string userName, string password, string exchange, string queue, bool declareQueue = true) 
            : base(host, userName, password, exchange, queue, declareQueue)
        {
        }

        public void ConsumeMessage(Action<TModel> consumeAction)
        {
            if (consumeAction == null)
            {
                throw new ArgumentNullException(nameof(consumeAction));
            }

            ConsumeMessage(
                m =>
                    {
                        var model = JsonSerializer.Deserialize<TModel>(Encoding.UTF8.GetString(m.Span));
                        if (model != null)
                        {
                            consumeAction(model);
                        }
                    });
        }
    }
}