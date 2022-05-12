namespace MessageQueue
{
    using RabbitMQ.Client;

    internal static class ChannelExtensions
    {
        public static void DeclareExchange(this IModel channel, string exchange)
        {
            if (channel == null)
            {
                throw new ArgumentNullException(nameof(channel));
            }

            channel.ExchangeDeclare("unrouted", "fanout", true, false, null);
            channel.QueueDeclare("unrouted", true, false, false, null);
            channel.QueueBind("unrouted", "unrouted", string.Empty);

            channel.ExchangeDeclare(
                exchange,
                "fanout",
                true,
                false,
                new Dictionary<string, object> { { "alternate-exchange", "unrouted" } });
        }
    }
}
