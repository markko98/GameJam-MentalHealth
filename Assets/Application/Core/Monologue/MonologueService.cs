using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

// MonologueService.Play(MonologueIds.Intro) - nothing needs to be in the scene.
// Playing while one runs queues it; Interrupt() replaces it.
public static class MonologueService
{
    private readonly struct Request
    {
        public readonly MonologueDefinition Monologue;
        public readonly Action Finished;

        public Request(MonologueDefinition monologue, Action finished)
        {
            Monologue = monologue;
            Finished = finished;
        }
    }

    private static MonologueCatalogue catalogue;
    private static readonly Dictionary<GameObject, MonologueView> views = new Dictionary<GameObject, MonologueView>();
    private static readonly Queue<Request> queue = new Queue<Request>();
    private static readonly HashSet<string> played = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> reportedIds = new HashSet<string>();
    private static readonly HashSet<GameObject> brokenPrefabs = new HashSet<GameObject>();
    private static MonologueView activeView;
    private static Transform parent;

    public static event Action<MonologueDefinition> Started;

    public static event Action<MonologueDefinition> Finished;

    public static MonologueDefinition Current { get; private set; }
    public static bool IsPlaying => Current != null;
    public static int Queued => queue.Count;

    public static bool IsWaitingForNext => activeView != null && activeView.IsWaitingForNext;

    public static MonologueCatalogue Catalogue => catalogue != null ? catalogue : catalogue = CatalogueLoader.Load<MonologueCatalogue>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        catalogue = null;
        views.Clear();
        brokenPrefabs.Clear();
        queue.Clear();
        played.Clear();
        reportedIds.Clear();
        activeView = null;
        parent = null;
        Current = null;
        Started = null;
        Finished = null;
    }

    #region Playback

    public static void Play(MonologueId id, Action onFinished = null) => Enqueue(Find(id), onFinished, false);

    public static void Play(MonologueDefinition monologue, Action onFinished = null) => Enqueue(monologue, onFinished, false);

    public static void PlayOnce(MonologueId id, Action onFinished = null) => Enqueue(Find(id), onFinished, true);

    public static void PlayOnce(MonologueDefinition monologue, Action onFinished = null) => Enqueue(monologue, onFinished, true);

    public static void Interrupt(MonologueId id, Action onFinished = null) => Interrupt(Find(id), onFinished);

    public static void Interrupt(MonologueDefinition monologue, Action onFinished = null)
    {
        Stop(true);
        Enqueue(monologue, onFinished, false);
    }

    public static void Next() => activeView?.Next();

    public static void Skip() => activeView?.Finish();

    public static void Stop(bool instant = false)
    {
        queue.Clear();
        var view = activeView;
        Current = null;
        activeView = null;
        if (view != null) view.Cancel(instant);
    }

    private static void Enqueue(MonologueDefinition monologue, Action onFinished, bool once)
    {
        if (monologue == null)
        {
            onFinished?.Invoke();
            return;
        }

        if (!monologue.HasLines)
        {
            Debug.LogWarning($"[MonologueService] \"{monologue.Id}\" has no lines.", monologue);
            onFinished?.Invoke();
            return;
        }

        if ((once || monologue.playOnce) && !played.Add(monologue.Id))
        {
            onFinished?.Invoke();
            return;
        }

        queue.Enqueue(new Request(monologue, onFinished));
        if (Current == null) PlayNextInQueue();
    }

    private static void PlayNextInQueue()
    {
        while (queue.Count > 0)
        {
            var request = queue.Dequeue();
            var view = ResolveView(request.Monologue);
            if (view == null)
            {
                request.Finished?.Invoke();
                continue;
            }

            Current = request.Monologue;
            activeView = view;
            Started?.Invoke(request.Monologue);
            view.Play(request.Monologue, () => OnViewFinished(request));
            return;
        }

        Current = null;
        activeView = null;
    }

    private static void OnViewFinished(Request request)
    {
        if (Current != request.Monologue) return;

        Current = null;
        activeView = null;

        request.Finished?.Invoke();
        Finished?.Invoke(request.Monologue);

        if (Current == null) PlayNextInQueue();
    }

    #endregion

    #region Lookup and history

    public static MonologueDefinition Find(MonologueId id)
    {
        if (!id.IsValid) return null;

        if (Catalogue.TryGet(id.Value, out var monologue) && monologue != null) return monologue;

        if (reportedIds.Add(id.Value))
            Debug.LogWarning($"[MonologueService] No monologue with id \"{id.Value}\" in the MonologueCatalogue.");
        return null;
    }

    public static bool Exists(MonologueId id) => id.IsValid && Catalogue.TryGet(id.Value, out var monologue) && monologue != null;

    public static bool HasPlayed(MonologueId id) => played.Contains(id.Value);

    public static void MarkPlayed(MonologueId id)
    {
        if (id.IsValid) played.Add(id.Value);
    }

    public static void ClearHistory() => played.Clear();

    #endregion

    #region Views

    public static void SetParent(Transform canvas)
    {
        parent = canvas;
        foreach (var view in views.Values)
        {
            if (view == null) continue;
            view.transform.SetParent(parent, false);
            if (parent == null) Object.DontDestroyOnLoad(view.gameObject);
        }
    }

    private static MonologueView ResolveView(MonologueDefinition monologue)
    {
        var prefab = monologue.viewPrefab != null ? monologue.viewPrefab : Catalogue.viewPrefab;
        if (prefab == null)
        {
            if (reportedIds.Add("<view>"))
                Debug.LogError("[MonologueService] No view prefab: set one on the MonologueCatalogue " +
                               "(Tools > Create > Monologue System builds one) or on the definition.");
            return null;
        }

        if (views.TryGetValue(prefab, out var existing) && existing != null) return existing;
        if (brokenPrefabs.Contains(prefab)) return null;

        var instance = Object.Instantiate(prefab, parent, false);
        instance.name = prefab.name;
        if (parent == null) Object.DontDestroyOnLoad(instance);

        var view = instance.GetComponent<MonologueView>();
        if (view == null)
        {
            Debug.LogError($"[MonologueService] \"{prefab.name}\" has no MonologueView on its root.", prefab);
            Object.Destroy(instance);
            brokenPrefabs.Add(prefab);
            return null;
        }

        if (instance.TryGetComponent<Canvas>(out var canvas)) canvas.sortingOrder = Catalogue.canvasSortOrder;

        views[prefab] = view;
        return view;
    }

    #endregion
}
