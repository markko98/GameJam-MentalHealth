using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisposeBag
{
    private List<DelayedExecutionTicket> tickets = new List<DelayedExecutionTicket>();

    public void Dispose()
    {
        foreach (var ticket in tickets)
        {
            DelayedExecutionManager.CancelTicket(ticket);
        }
    }

    public void Dispose(DelayedExecutionTicket ticket)
    {
        if (!tickets.Contains(ticket)) return;
        DelayedExecutionManager.CancelTicket(ticket);
    }

    public void AddTicket(DelayedExecutionTicket ticket)
    {
        tickets.Add(ticket);
    }
}