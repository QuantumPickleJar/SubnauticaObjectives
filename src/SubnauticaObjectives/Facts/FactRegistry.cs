using System;
using System.Collections.Generic;
using SubnauticaObjectives.Models;

namespace SubnauticaObjectives.Facts;

// Thread-safe in-memory store for known facts.
// Fires OnFactAdded whenever a new fact is registered.
public sealed class FactRegistry
{
    private readonly HashSet<string> _facts = new(StringComparer.Ordinal);
    private readonly object _lock = new();

    public event Action<string>? OnFactAdded;

    // Returns an immutable snapshot of all currently known facts.
    public ISet<string> Snapshot()
    {
        lock (_lock)
        {
            return new HashSet<string>(_facts, StringComparer.Ordinal);
        }
    }

    // Registers a fact. Returns true if it was newly added (false if already known).
    public bool Add(string fact)
    {
        bool added;
        lock (_lock)
        {
            added = _facts.Add(fact);
        }
        if (added)
            OnFactAdded?.Invoke(fact);
        return added;
    }

    // Registers multiple facts at once without firing an event per fact;
    // fires a single bulk-completion callback instead.
    public void AddBulk(IEnumerable<string> facts, Action? onComplete = null)
    {
        lock (_lock)
        {
            foreach (var f in facts)
                _facts.Add(f);
        }
        onComplete?.Invoke();
    }

    public bool Contains(string fact)
    {
        lock (_lock)
        {
            return _facts.Contains(fact);
        }
    }

    public int Count
    {
        get { lock (_lock) { return _facts.Count; } }
    }

    // Validates that all facts referenced in the graph are known to the registry's declared list.
    // Used only at startup for diagnostic logging — not a hard block.
    public static void LogUnknownFacts(ISet<string> knownFacts, CampaignGraph graph, BepInEx.Logging.ManualLogSource log)
    {
        foreach (var fact in knownFacts)
        {
            if (!graph.Facts.Contains(fact))
                log.LogWarning($"[FactRegistry] Detected fact '{fact}' is not declared in campaign graph facts list.");
        }
    }
}
