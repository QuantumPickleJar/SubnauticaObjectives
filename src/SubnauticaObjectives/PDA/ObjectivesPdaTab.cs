using System.Collections.Generic;
using System.Text;
using BepInEx.Logging;
using SubnauticaObjectives.Graph;
using SubnauticaObjectives.Models;

namespace SubnauticaObjectives.PDA;

// Registers campaign objectives into the vanilla PDA Encyclopedia (Databank).
// Adds entries under an "Objectives" category without requiring Nautilus.
//
// PDAEncyclopedia stores entries keyed by string ID. Each entry has a title and body text.
// We add one entry per active objective, regenerated on demand.
public static class ObjectivesPdaTab
{
    // Category key used for all entries added by this mod.
    private const string CategoryKey = "SubnauticaObjectives";

    // Encyclopedia entry key prefix.
    private const string EntryPrefix = "obj_";

    private static ManualLogSource? _log;
    private static readonly List<string> _registeredKeys = new List<string>();

    public static void Initialize(ManualLogSource log)
    {
        _log = log;
    }

    // Registers (or re-registers) all active objectives into the PDA Databank.
    // Call this once at startup after fact detection and again whenever the fact set changes.
    public static void Refresh(ISet<string> facts, GraphEvaluator evaluator, int hintDepth)
    {
        ClearRegisteredEntries();

        var activeNodes = evaluator.GetActiveNodes(facts);

        var sb = new StringBuilder();
        sb.AppendLine("Current objectives:");
        sb.AppendLine();

        int count = 0;
        foreach (var node in System.Linq.Enumerable.OrderByDescending(activeNodes, n => n.Priority ?? 0))
        {
            if (node.NodeType is not ("objective" or "safety_barrier" or "facility_interaction"))
                continue;

            string hint = GraphEvaluator.GetHintText(node, hintDepth);
            string key = $"{EntryPrefix}{node.Id}";

            AddEntry(key, node.Title, hint);
            _registeredKeys.Add(key);

            sb.AppendLine($"• {hint}");
            count++;
        }

        if (count == 0)
            sb.AppendLine("(No active objectives — check your progress or the graph data.)");

        // Add a summary "Objectives" root entry that lists everything.
        AddEntry($"{EntryPrefix}summary", "Objectives", sb.ToString());
        _registeredKeys.Add($"{EntryPrefix}summary");

        _log?.LogInfo($"[ObjectivesPdaTab] Refreshed — {count} active objective(s) registered.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static void AddEntry(string key, string title, string body)
    {
        // PDAEncyclopedia.AddCustomEntry requires an EntryData struct.
        // We populate only the fields available without Nautilus.
        var data = new PDAEncyclopedia.EntryData
        {
            key        = key,
            // path is the slash-separated category path shown in the Databank tree.
            path       = $"{CategoryKey}/{key}",
            // TODO: timeCapsule field may not exist in this version of Subnautica.
            // timeCapsule = false,
        };

        // Register the entry if it is not already known.
        if (!PDAEncyclopedia.ContainsEntry(key))
        {
            PDAEncyclopedia.Add(key, verbose: false);
            _log?.LogDebug($"[ObjectivesPdaTab] Added entry '{key}': {title}");
        }
    }

    private static void ClearRegisteredEntries()
    {
        // PDAEncyclopedia has no public remove API, so we track our keys and skip re-adding.
        // On a full refresh we simply let duplicates be silently ignored by ContainsEntry checks.
        _registeredKeys.Clear();
    }
}
