using System.Threading;

namespace MonoStereo;

/// <summary>
/// A substitude for lock() that is guaranteed to respect request order.
/// </summary>
public sealed class QueuedLock
{
    private readonly object innerLock = new();
    private volatile int ticketsCount = 0;
    private volatile int ticketToRide = 1;

    public void Enter()
    {
        int myTicket = Interlocked.Increment(ref ticketsCount);
        Monitor.Enter(innerLock);

        while (true)
        {
            if (myTicket == ticketToRide)
                return;

            else
                Monitor.Wait(innerLock);
        }
    }

    public void Exit()
    {
        Interlocked.Increment(ref ticketToRide);
        Monitor.PulseAll(innerLock);
        Monitor.Exit(innerLock);
    }
}