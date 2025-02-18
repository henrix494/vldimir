using System.Collections.Concurrent;
using System.Timers;
using vladimir;
using Timer = System.Timers.Timer;
class Program
{
    private static ConcurrentQueue<Client> Clients = new();
    private static List<Flight> Flights = new();
    private static int numOfFlight = 3;
    private static Timer DepartureTimer = new(25000);
    private static Timer? _clientCreationTimer;
    private static CancellationTokenSource Cts = new();
    private static object ConsoleLock = new();
    private static List<Worker> Workers = new();
    private static object endLock = new();
    private static bool isEnd = false;
    static void Main(string[] args)
    {
        for (int i = 0; i < numOfFlight; i++)
        {
            Flights.Add(new Flight());
        }
        DepartureTimer.Elapsed += DepartureTimerOnElapsed;
        DepartureTimer.AutoReset = false;
        DepartureTimer.Start();

        StartClientFactory();

        var workerThreads = new List<Thread>();
        for (var i = 0; i < 4; i++)
        {
            var worker = new Worker { CancellationToken = Cts.Token };
            Workers.Add(worker);
            var thread = new Thread(SellTickets);
            thread.Start(worker);
            workerThreads.Add(thread);
        }

        Console.ReadLine();
        Cts.Cancel();
        foreach (var thread in workerThreads)
        {
            thread.Join();
        }
    }

    private static void DepartureTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        EndOfSales("DEPARTURE");
    }

    private static void LockedWriteLine(string message)
    {
        lock (ConsoleLock)
        {
            Console.WriteLine(message);
        }
    }

    private static void StartClientFactory()
    {
        _clientCreationTimer = new Timer() ;
        _clientCreationTimer.Elapsed += CreateNewClient;
        _clientCreationTimer.Elapsed += ClientCreationTimerRearm;
       // ClientCreationTimerRearm(null, null!);
        _clientCreationTimer.Start();
    }

    private static void CreateNewClient(object? sender, ElapsedEventArgs e)
    {
        var client = new Client();
        Clients.Enqueue(client);
        LockedWriteLine($"[CreateNewClient] {client.Id} Client created");
    }

    private static void ClientCreationTimerRearm(object? sender, ElapsedEventArgs e)
    {
            _clientCreationTimer.Interval = Random.Shared.Next(5, 25);
    }


    private static void SellTickets(object? obj)
    {
        if (obj is not Worker worker)
        {
            return;
        }

        while (!worker.CancellationToken.IsCancellationRequested)
        {
            if (Clients.TryDequeue(out var client))
            {
                bool sold = false;
                foreach (var flight in Flights)
                {
                    if (flight.TryBookSeat(out var cost))
                    {
                        worker.ClientsServed++;
                        worker.Revenue += cost;
                        LockedWriteLine($"[SellTickets] Worker {worker.Id} sold a ticket to client {client.Id} for ${cost} on Flight {Flights.IndexOf(flight) + 1}");
                        sold = true;
                        break;
                    }
                }

                if (!sold)
                {
                    EndOfSales("ALL FLIGHTS SOLD OUT");
                    break;
                }
            }
            else
            {
                Thread.Sleep(Random.Shared.Next(5, 25));
            }
        }
    }
    private static void EndOfSales(string reason)
    {
        lock (endLock)
        {
            if (isEnd == true)
            {
                return;
            }

            _clientCreationTimer?.Stop();
            Cts.Cancel();

            int totalRevenue = 0;
            int totalClientsServed = 0;

            foreach (var worker in Workers)
            {
                totalRevenue += worker.Revenue;
                totalClientsServed += worker.ClientsServed;
            }

            LockedWriteLine($"SALES STOPPED : {reason}");
            LockedWriteLine($"Total revenue: {totalRevenue}, Clients served total: {totalClientsServed}");
        }
        isEnd = true;
    }
}