using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class UEventBus<T> where T : IEvent
{
    static readonly HashSet<IEventBinding<T>> bindings = new HashSet<IEventBinding<T>>();

    public static void Register(EventBinding<T> binding) => bindings.Add(binding);
    public static void Deregister(EventBinding<T> binding) => bindings.Remove(binding);

    public static void Raise(T eventTarget)
    {
        var snapshot = new HashSet<IEventBinding<T>>(bindings);

        foreach (var binding in snapshot.Where(binding => bindings.Contains(binding)))
        {
            binding.OnEvent.Invoke(eventTarget);
            binding.OnEventNoArgs.Invoke();
        }
        UEventBusAny.Raise(eventTarget);
    }
}