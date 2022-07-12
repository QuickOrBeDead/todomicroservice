namespace MessageQueue;

public sealed class RabbitMqConsumerSettings
{
    public string? Queue { get; set; }

    public bool SingleActiveConsumer { get; set; }
}