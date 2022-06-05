namespace MessageQueue
{
    using System.Text;
    using System.Text.Json;

    using Polly;

    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;

    public interface IMessageQueueConsumerService : IDisposable
    {
        void ConsumeMessage(ConsumeAction consumeAction);
    }

    public interface IMessageQueueConsumerService<out TModel> : IMessageQueueConsumerService
    {
        void ConsumeMessage(ConsumeMessageAction<TModel> consumeAction);
    }

    public delegate void ConsumeAction(ReadOnlyMemory<byte> messageBytes, string messageType);

    public delegate void ConsumeMessageAction<in TModel>(TModel model, string messageType);

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

        public RabbitMqMessageQueueConsumerService(RabbitMqSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (string.IsNullOrWhiteSpace(settings.UserName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(settings.UserName));
            }

            if (string.IsNullOrWhiteSpace(settings.Password))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(settings.Password));
            }

            if (string.IsNullOrWhiteSpace(settings.Exchange))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(settings.Exchange));
            }

            if (string.IsNullOrWhiteSpace(settings.Queue))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(settings.Queue));
            }

            if (string.IsNullOrWhiteSpace(settings.Host))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(settings.Host));
            }

            _host = settings.Host;
            _userName = settings.UserName;
            _password = settings.Password;
            _exchange = settings.Exchange;
            _queue = settings.Queue;
            _declareQueue = settings.DeclareQueue;
        }

        public void ConsumeMessage(ConsumeAction consumeAction)
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
                                consumeAction(e.Body, GetMessageType(e));

                                _channel.BasicAck(e.DeliveryTag, false);
                            };
                        _channel.BasicQos(0, 1, false);
                        _consumerTag = _channel.BasicConsume(_queue, false, consumer);
                    });
        }

        private static string GetMessageType(BasicDeliverEventArgs e)
        {
            string messageType;
            if (e.BasicProperties.Headers.TryGetValue("MessageType", out var messageTypeObject)
                && messageTypeObject is byte[] messageTypeBytes)
            {
                messageType = Encoding.UTF8.GetString(messageTypeBytes);
            }
            else
            {
                messageType = "None";
            }

            return messageType;
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
        public RabbitMqMessageQueueGenericConsumerService(RabbitMqSettings settings) 
            : base(settings)
        {
        }

        public void ConsumeMessage(ConsumeMessageAction<TModel> consumeAction)
        {
            if (consumeAction == null)
            {
                throw new ArgumentNullException(nameof(consumeAction));
            }

            ConsumeMessage(
                (m, t) =>
                    {
                        var model = JsonSerializer.Deserialize<TModel>(Encoding.UTF8.GetString(m.Span));
                        if (model != null)
                        {
                            consumeAction(model, t);
                        }
                    });
        }
    }
}