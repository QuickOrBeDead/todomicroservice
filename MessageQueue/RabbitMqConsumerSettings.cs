namespace MessageQueue;

public sealed class RabbitMqConsumerSettings
{
    public string? Exchange { get; set; }

    public string? Queue { get; set; }

    public bool SingleActiveConsumer { get; set; }
}