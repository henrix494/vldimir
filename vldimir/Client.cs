namespace vladimir;

public class Client
{
    private static int _id = 1;
    private static readonly object IdLock = new();
    public int Id { get; }

    public Client()
    {
        lock (IdLock)
        {
            Id = _id++;
        }
    }
}