namespace vladimir;

public class Flight
{
    public const int FirstClassCost = 800;
    public const int EconomyClassCost = 300;
    private readonly object _firstClassSeatsLock = new();
    private readonly object _economyClassSeatsLock = new();
    private int _nFirstClassSeats = 12;
    private int _nEconomyClassSeats = 120;
    private readonly object _bookingLock = new();
    private readonly object _soldOutLock = new();
    private bool _soldOut;

    private int NFirstClassSeats
    {
        get
        {
            lock (_firstClassSeatsLock)
            {
                return _nFirstClassSeats;
            }
        }
        set
        {
            lock (_firstClassSeatsLock)
            {
                _nFirstClassSeats = value;
            }
        }
    }

    private int NEconomyClassSeats
    {
        get
        {
            lock (_economyClassSeatsLock)
            {
                return _nEconomyClassSeats;
            }
        }
        set
        {
            lock (_economyClassSeatsLock)
            {
                _nEconomyClassSeats = value;
            }
        }
    }


    private bool SoldOut
    {
        get
        {
            lock (_soldOutLock)
            {
                return _soldOut;
            }
        }
        set
        {
            lock (_soldOutLock)
            {
                _soldOut = value;
            }
        }
    }

    public bool TryBookSeat(out int cost)
    {
        lock (_bookingLock)
        {
            cost = 0;
            if (SoldOut)
            {
                return false;
            }

            cost = NFirstClassSeats > 0 ? FirstClassCost : NEconomyClassSeats > 0 ? EconomyClassCost : 0;
            SoldOut = cost == 0;
            NFirstClassSeats -= cost == FirstClassCost ? 1 : 0;
            NEconomyClassSeats -= cost == EconomyClassCost ? 1 : 0;
            return !SoldOut;
        }
    }
}