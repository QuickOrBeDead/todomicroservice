namespace MessageQueue.Events;

public class EventBase
{
    public Guid Id { get; }

    public DateTime CreationDate { get; }

    protected EventBase()
    {
        Id = Guid.NewGuid();
        CreationDate = DateTime.UtcNow;
    }

    protected EventBase(Guid id, DateTime createDate)
    {
        Id = id;
        CreationDate = createDate;
    }
}