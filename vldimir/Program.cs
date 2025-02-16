using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;
using vladimir;
class Program
{
    private static  ConcurrentQueue<Client> Clients = new();
    private static  Flight Flight = new();
    private static  Timer DepartureTimer = new(1000); 
    private static Timer? _clientCreationTimer;
    private static  CancellationTokenSource Cts = new();
    private static  object ConsoleLock = new();
    private static  List<Worker> Workers = new();

    static void Main(string[] args)
    {
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
        _clientCreationTimer = new Timer();
        _clientCreationTimer.Elapsed += CreateNewClient;
        _clientCreationTimer.Elapsed += ClientCreationTimerRearm;
        ClientCreationTimerRearm(null, null!);
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
        if (_clientCreationTimer != null)
        {
            _clientCreationTimer.Interval = new Random().Next(5, 25);
        }
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
                if (Flight.TryBookSeat(out var cost))
                {
                    worker.ClientsServed++;
                    worker.Revenue += cost;
                    LockedWriteLine($"[SellTickets] Worker {worker.Id} sold a ticket to client {client.Id} for ${cost}");
                }
                else
                {
                    EndOfSales("ALL FLIGHTS SOLD OUT");
                    break;
                }
            }
            else
            {
                Thread.Sleep(10); // Avoid busy waiting
            }
        }

    }
    private static void EndOfSales(string reason)
    {
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
}