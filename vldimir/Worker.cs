namespace vladimir;

public class Worker
{
    private static int _id = 1;
    private static readonly object IdLock = new();
    private static readonly object ClientsServedLock = new();
    private static readonly object RevenueLock = new();
    public int Id { get; }

    private int _clientsServed;
    private int _revenue;

    public int ClientsServed
    {
        get
        {
            lock (ClientsServedLock)
            {
                return _clientsServed;
            }
        }
        set
        {
            lock (ClientsServedLock)
            {
                _clientsServed = value;
            }
        }
    }

    public int Revenue
    {
        get
        {
            lock (RevenueLock)
            {
                return _revenue;
            }
        }
        set
        {
            lock (RevenueLock)
            {
                _revenue = value;
            }
        }
    }

    public CancellationToken CancellationToken { get; init; }

    public Worker()
    {
        lock (IdLock)
        {
            Id = _id++;
        }
    }
}