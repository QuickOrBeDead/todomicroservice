namespace MessageQueue;

using System.Text;
using System.Text.Json;
using MessageQueue.Events;
using Polly;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public interface IMessageQueueConsumerService<out TEvent> : IDisposable
    where TEvent : EventBase
{
    void ConsumeMessage(ConsumeMessageAction<TEvent> consumeAction);
}

public delegate bool ConsumeMessageAction<in TModel>(TModel model, string messageType);

public class RabbitMqMessageQueueConsumerService<TEvent> : IMessageQueueConsumerService<TEvent>
    where TEvent : EventBase
{
    private readonly IRabbitMqConnection _connection;

    private readonly string _queue;

    private bool _disposed;

    private IModel? _channel;

    private string? _consumerTag;

    public RabbitMqMessageQueueConsumerService(IRabbitMqConnection connection, string queueName)
    {
        if (string.IsNullOrWhiteSpace(queueName))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(queueName));
        }

        _connection = connection ?? throw new ArgumentNullException(nameof(connection));

        _queue = queueName;
    }

    public void ConsumeMessage(ConsumeMessageAction<TEvent> consumeAction)
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
                        _channel = _connection.CreateChannel();

                        var exchange = EventNameAttribute.GetEventName<TEvent>();
                        _channel.ExchangeDeclare(exchange, "fanout", true, false, null);
                        _channel.QueueDeclare(_queue, true, false, false, null);
                        _channel.QueueBind(_queue, exchange, string.Empty);

                        var consumer = new EventingBasicConsumer(_channel);
                        consumer.Received += (_, e) =>
                            {
                                var @event = JsonSerializer.Deserialize<TEvent>(Encoding.UTF8.GetString(e.Body.Span));
                                if (@event == null)
                                {
                                    _channel.BasicNack(e.DeliveryTag, false, true);
                                    return;
                                }

                                var ack = consumeAction(@event, GetEventName(e));

                                if (ack)
                                {
                                    _channel.BasicAck(e.DeliveryTag, false);
                                }
                                else
                                {
                                    _channel.BasicNack(e.DeliveryTag, false, true);
                                }
                            };
                        _channel.BasicQos(0, 1, false);
                        _consumerTag = _channel.BasicConsume(_queue, false, consumer);
                    });
    }

    private static string GetEventName(BasicDeliverEventArgs e)
    {
        string eventName;
        if (e.BasicProperties.Headers.TryGetValue("EventName", out var eventNameObject)
            && eventNameObject is byte[] eventNameBytes)
        {
            eventName = Encoding.UTF8.GetString(eventNameBytes);
        }
        else
        {
            eventName = "None";
        }

        return eventName;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // dispose managed state (managed objects)
            }

            if (_consumerTag != null)
            {
                _channel?.BasicCancel(_consumerTag);
            }

            _channel?.Close(200, "Goodbye");
            _channel?.Dispose();
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