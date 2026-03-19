using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx.Logging;
using SubnauticaObjectives.Graph;
using SubnauticaObjectives.Models;

namespace SubnauticaObjectives.PDA;

// Registers campaign objectives into the vanilla PDA Encyclopedia (Databank)
// under Guide > Objectives, without requiring Nautilus.
//
// Lifecycle:
//   1. Initialize(log, graph) — called once during Plugin.Awake.
//   2. PreRegisterAllEntries() — called from PdaLifecyclePatches immediately
//      after PDAEncyclopedia.Initialize, so the mapping dictionary contains
//      valid EntryData for every graph node BEFORE save-file restoration runs.
//   3. Refresh(facts, evaluator, hintDepth) — called whenever facts change;
//      updates Language text for active objectives and unlocks their entries.
public static class ObjectivesPdaTab
{
    private const string CategoryPath = "Guide/Objectives";
    private const string EntryPrefix = "obj_";

    private static ManualLogSource? _log;
    private static CampaignGraph? _graph;
    private static Dictionary<string, GraphNode>? _nodesById;
    private static readonly HashSet<string> _everUnlockedIds = new();

    private static readonly FieldInfo? MappingField =
        typeof(PDAEncyclopedia).GetField("mapping", BindingFlags.Static | BindingFlags.NonPublic);

    private static readonly FieldInfo? InitializedField =
        typeof(PDAEncyclopedia).GetField("initialized", BindingFlags.Static | BindingFlags.NonPublic);

    private static readonly FieldInfo? LanguageStringsField =
        typeof(Language).GetField("strings", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo? EntriesField =
        typeof(PDAEncyclopedia).GetField("entries", BindingFlags.Static | BindingFlags.NonPublic);

    public static void Initialize(ManualLogSource log, CampaignGraph graph)
    {
        _log = log;
        _graph = graph;
        _nodesById = graph.Nodes.ToDictionary(n => n.Id);
    }

    // ── Phase 2: Pre-register all possible entries in the mapping ───────

    /// <summary>
    /// Populates the PDAEncyclopedia mapping dictionary with EntryData for
    /// every graph node (and a summary entry) so that:
    ///   • save-file restoration never sees "Entry not found" errors, and
    ///   • later Add() calls in Refresh always succeed.
    /// Called from PdaLifecyclePatches right after PDAEncyclopedia.Initialize.
    /// </summary>
    public static void PreRegisterAllEntries()
    {
        if (_graph is null)
        {
            _log?.LogWarning("[ObjectivesPdaTab] Cannot pre-register: graph not loaded.");
            return;
        }

        if (MappingField?.GetValue(null) is not IDictionary mapping)
        {
            _log?.LogWarning("[ObjectivesPdaTab] Cannot pre-register: mapping field not accessible.");
            return;
        }

        RegisterPathDisplayNames();

        int count = 0;
        foreach (var node in _graph.Nodes)
        {
            string key = EntryPrefix + node.Id;
            mapping[key] = MakeEntryData(key);
            RegisterLanguageLine("Ency_" + key, node.Title);
            RegisterLanguageLine("EncyDesc_" + key, node.Title);
            count++;
        }

        // Summary entry.
        string summaryKey = EntryPrefix + "summary";
        mapping[summaryKey] = MakeEntryData(summaryKey);
        RegisterLanguageLine("Ency_" + summaryKey, "Objectives");
        RegisterLanguageLine("EncyDesc_" + summaryKey, "Objectives will appear once the session is evaluated.");

        _log?.LogInfo("[ObjectivesPdaTab] Pre-registered " + (count + 1) + " entries in mapping.");
    }

    // ── Phase 3: Refresh — unlock active entries and update text ─────────

    public static void Refresh(ISet<string> facts, GraphEvaluator evaluator, int hintDepth)
    {
        if (!IsPdaReady())
        {
            _log?.LogDebug("[ObjectivesPdaTab] PDA not ready; skipping refresh.");
            return;
        }

        var activeNodes = evaluator.GetActiveNodes(facts).ToList();
        var actionableNodes = activeNodes
            .Where(n => n.NodeType != "major_milestone" && n.NodeType != "bubble")
            .OrderByDescending(n => n.Priority ?? 0)
            .ToList();
        var trackerNodes = activeNodes
            .Where(n => n.NodeType == "major_milestone" || n.NodeType == "bubble")
            .OrderByDescending(n => n.Priority ?? 0)
            .ToList();

        var summaryBuilder = new StringBuilder();
        summaryBuilder.AppendLine("Pending Objectives");
        summaryBuilder.AppendLine();

        int count = 0;
        foreach (var node in activeNodes.OrderByDescending(n => n.Priority ?? 0))
        {
            string key = EntryPrefix + node.Id;
            string body = BuildEntryBody(node, hintDepth, facts, evaluator);

            RegisterLanguageLine("Ency_" + key, node.Title);
            RegisterLanguageLine("EncyDesc_" + key, body);
            UnlockEntry(key);
            _everUnlockedIds.Add(node.Id);

            count++;
        }

        int summaryCount = 0;
        foreach (var node in actionableNodes)
        {
            string summaryHint = GraphEvaluator.GetHintText(node, 1);
            if (string.IsNullOrWhiteSpace(summaryHint) || summaryHint == node.Title)
                summaryHint = node.Title;

            summaryBuilder.AppendLine("- " + EnsureSentence(summaryHint));
            summaryCount++;
        }

        if (summaryCount == 0)
            summaryBuilder.AppendLine("- No pending actionable objectives.");

        if (trackerNodes.Count > 0)
        {
            summaryBuilder.AppendLine();
            summaryBuilder.AppendLine("Active Phase Trackers");
            foreach (var node in trackerNodes)
                summaryBuilder.AppendLine("- " + EnsureSentence(node.Title));
        }

        // Remove PDA entries for previously-unlocked nodes that are now done.
        var doneIds = new List<string>();
        foreach (var nodeId in _everUnlockedIds)
        {
            if (!_nodesById!.TryGetValue(nodeId, out var doneNode))
                continue;
            if (!evaluator.IsNodeDone(doneNode, facts))
                continue;

            RemoveEntry(EntryPrefix + nodeId);
            doneIds.Add(nodeId);
        }
        foreach (var id in doneIds)
            _everUnlockedIds.Remove(id);

        // Unlock the summary entry so Guide > Objectives is always visible.
        string summaryKey = EntryPrefix + "summary";
        RegisterLanguageLine("Ency_" + summaryKey, "Objectives");
        RegisterLanguageLine("EncyDesc_" + summaryKey, summaryBuilder.ToString());
        UnlockEntry(summaryKey);

        _log?.LogInfo("[ObjectivesPdaTab] Refreshed - " + count + " active objective(s) registered.");
    }

    // Delegates to hint-layer body or overview body depending on the node.
    private static string BuildEntryBody(GraphNode node, int hintDepth, ISet<string> facts, GraphEvaluator evaluator)
    {
        if (node.HintLayers is not null && node.HintLayers.Count > 0)
            return BuildHintBody(node, hintDepth);

        return BuildOverviewBody(node, facts, evaluator);
    }

    // Builds the Databank page body for a node that has hint layers.
    private static string BuildHintBody(GraphNode node, int hintDepth)
    {
        var layers = node.HintLayers;
        if (layers is null || layers.Count == 0)
            return node.Title;

        int maxDepth = System.Math.Max(1, System.Math.Min(hintDepth, 3));
        var sb = new StringBuilder();

        sb.AppendLine("Assessment");
        sb.AppendLine(EnsureSentence(node.Title + " is currently active."));
        sb.AppendLine();
        sb.AppendLine("Guidance");

        for (int d = 1; d <= maxDepth; d++)
        {
            if (!layers.TryGetValue(d.ToString(), out var layer))
                continue;

            if (layer.Visibility == "spoiler_masked")
                sb.AppendLine("Classified: Increase hint depth to reveal this guidance layer.");
            else
                sb.AppendLine(EnsureSentence(layer.Text));

            if (d < maxDepth)
                sb.AppendLine();
        }

        return sb.Length > 0 ? sb.ToString().TrimEnd() : node.Title;
    }

    // Builds an overview page for milestone/bubble nodes by listing successors.
    private static string BuildOverviewBody(GraphNode node, ISet<string> facts, GraphEvaluator evaluator)
    {
        if (_nodesById is null || node.Successors is null || node.Successors.Count == 0)
            return node.Title;

        var sb = new StringBuilder();
        sb.AppendLine("Campaign Phase");
        sb.AppendLine(EnsureSentence(node.Title + " is being tracked by this entry."));
        sb.AppendLine();
        sb.AppendLine("Status");
        sb.AppendLine("Only pending directives are shown below.");
        sb.AppendLine();
        sb.AppendLine("Directive Checklist");
        sb.AppendLine();

        int pending = 0;
        int completed = 0;

        foreach (string successorId in node.Successors)
        {
            if (!_nodesById.TryGetValue(successorId, out var successor))
                continue;

            bool done = evaluator.IsNodeDone(successor, facts);
            if (done)
            {
                completed++;
                continue;
            }

            sb.AppendLine("[ ]  " + successor.Title);
            pending++;
        }

        if (pending == 0)
            sb.AppendLine("- No pending directives remain in this phase.");

        if (completed > 0)
        {
            sb.AppendLine();
            sb.AppendLine(EnsureSentence(completed + " directive(s) already complete and hidden from this checklist"));
        }

        return sb.ToString().TrimEnd();
    }

    private static string EnsureSentence(string text)
    {
        string cleaned = text.Trim();
        if (cleaned.Length == 0)
            return cleaned;

        char last = cleaned[cleaned.Length - 1];
        if (last != '.' && last != '!' && last != '?')
            return cleaned + ".";

        return cleaned;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static PDAEncyclopedia.EntryData MakeEntryData(string key)
    {
        return new PDAEncyclopedia.EntryData
        {
            key = key,
            path = CategoryPath,
            nodes = CategoryPath.Split('/'),
            unlocked = false,
        };
    }

    private static void RegisterPathDisplayNames()
    {
        RegisterLanguageLine("EncyPath_Guide", "Guide");
        RegisterLanguageLine("EncyPath_Guide/Objectives", "Objectives");
    }

    private static void RegisterLanguageLine(string key, string value)
    {
        if (Language.main == null)
            return;

        if (LanguageStringsField?.GetValue(Language.main) is not IDictionary strings)
            return;

        strings[key] = value;
    }

    private static void UnlockEntry(string key)
    {
        try
        {
            if (!PDAEncyclopedia.HasEntryData(key))
            {
                _log?.LogWarning("[ObjectivesPdaTab] No mapping for '" + key + "'; skipping unlock.");
                return;
            }

            if (!PDAEncyclopedia.ContainsEntry(key))
                PDAEncyclopedia.Add(key, false);
        }
        catch (Exception ex)
        {
            _log?.LogWarning("[ObjectivesPdaTab] Failed to unlock '" + key + "': " + ex.Message);
        }
    }

    private static void RemoveEntry(string key)
    {
        try
        {
            if (EntriesField?.GetValue(null) is not IDictionary entries)
                return;

            if (entries.Contains(key))
                entries.Remove(key);
        }
        catch (Exception ex)
        {
            _log?.LogWarning("[ObjectivesPdaTab] Failed to remove '" + key + "': " + ex.Message);
        }
    }

    private static bool IsPdaReady()
    {
        if (Language.main == null)
            return false;

        if (InitializedField?.GetValue(null) is not bool initialized)
            return false;

        return initialized;
    }
}
