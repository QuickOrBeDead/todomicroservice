namespace MessageQueue
{
    public sealed class RabbitMqSettings
    {
        public string Host { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public string Exchange { get; set; }

        public string Queue { get; set; }

        public bool DeclareQueue { get; set; }
    }
}