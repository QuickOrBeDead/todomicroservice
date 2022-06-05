namespace MessageQueue
{
    using System.Diagnostics.CodeAnalysis;
    using System.Text;
    using System.Text.Json;

    using Polly;

    using RabbitMQ.Client;

    public interface IMessageQueuePublisherService : IDisposable
    {
        void PublishMessage<TModel>([DisallowNull] TModel model);

        void Publish(ReadOnlyMemory<byte> body, string messageType = "Text");
    }

    public class RabbitMqMessageQueuePublisherService : IMessageQueuePublisherService
    {
        private readonly string _host;

        private readonly string _userName;

        private readonly string _password;

        private readonly string _exchange;

        private IConnection? _connection;

        private IModel? _channel;

        private bool _disposed;

        public RabbitMqMessageQueuePublisherService(RabbitMqSettings settings)
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

            Connect();
        }

        public void PublishMessage<TModel>([DisallowNull] TModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            Publish(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(model)), typeof(TModel).Name);
        }

        public void Publish(ReadOnlyMemory<byte> body, string messageType = "Text")
        {
            if (_channel == null)
            {
                throw new InvalidOperationException("Channel cannot be null.");
            }

            IBasicProperties properties = _channel.CreateBasicProperties();
            properties.Headers = new Dictionary<string, object> { { "MessageType", messageType } };

            _channel.BasicPublish(
                _exchange,
                string.Empty,
                null,
                body);
        }

        private void Connect()
        {
            Policy.Handle<Exception>()
                  .WaitAndRetry(10, r => TimeSpan.FromSeconds(5))
                  .Execute(() =>
                    {
                        var factory = new ConnectionFactory { HostName = _host, UserName = _userName, Password = _password };
                        _connection = factory.CreateConnection();
                        _channel = _connection.CreateModel();
                        _channel.DeclareExchange(_exchange);
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

                _channel?.Dispose();
                _connection?.Dispose();
                _disposed = true;
            }
        }

        ~RabbitMqMessageQueuePublisherService()
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
