using System;
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

    /// <summary>
    /// Manually enters this <see cref="QueuedLock"/> state.<br/>
    /// Only use this if you know what you're doing - otherwise use <see cref="Execute(Action)"/>
    /// </summary>
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

    /// <summary>
    /// Manually exits this <see cref="QueuedLock"/> state.<br/>
    /// Only use this if you know what you're doing - otherwise use <see cref="Execute(Action)"/>
    /// </summary>
    public void Exit()
    {
        Interlocked.Increment(ref ticketToRide);
        Monitor.PulseAll(innerLock);
        Monitor.Exit(innerLock);
    }

    /// <summary>
    /// Locks this <see cref="QueuedLock"/>, executes the specified action, and then exits the lock.
    /// </summary>
    public void Execute(Action action)
    {
        Enter();
        try { action(); }
        finally { Exit(); }
    }
}