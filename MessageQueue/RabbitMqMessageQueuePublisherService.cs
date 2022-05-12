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

        void Publish(ReadOnlyMemory<byte> body);
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

            Connect();
        }

        public void PublishMessage<TModel>([DisallowNull] TModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            Publish(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(model)));
        }

        public void Publish(ReadOnlyMemory<byte> body)
        {
            if (_channel == null)
            {
                throw new InvalidOperationException("Channel cannot be null.");
            }

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
