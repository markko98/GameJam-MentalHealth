using System;
using System.Collections.Generic;

public static class UEventBusAny
{
    private static readonly HashSet<Action<IEvent>> handlers = new HashSet<Action<IEvent>>();

    public static void Register(Action<IEvent> handler) => handlers.Add(handler);
    public static void Deregister(Action<IEvent> handler) => handlers.Remove(handler);

    internal static void Raise(IEvent e)
    {
        var snapshot = new List<Action<IEvent>>(handlers);
        foreach (var h in snapshot)
        {
            if (handlers.Contains(h))
                h.Invoke(e);
        }
    }
}