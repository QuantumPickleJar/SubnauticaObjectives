using System.Collections.Generic;
using System.Linq;
using SubnauticaObjectives.Models;

namespace SubnauticaObjectives.Graph;

// Evaluates the campaign graph against the current known-fact set.
//
// Node completion semantics:
//   A node is DONE if any already_satisfied_rules pass (drop-in support)
//   OR all completion_rules pass (completed during this session).
//
// A node is ACTIVE if:
//   - It is NOT done
//   - All predecessor nodes are done
//   - All activation_rules pass (or none are specified)
public sealed class GraphEvaluator
{
    private readonly CampaignGraph _graph;
    private readonly Dictionary<string, GraphNode> _nodesById;

    public GraphEvaluator(CampaignGraph graph)
    {
        _graph = graph;
        _nodesById = graph.Nodes.ToDictionary(n => n.Id);
    }

    // Returns all nodes currently considered done.
    public IEnumerable<GraphNode> GetDoneNodes(ISet<string> facts) =>
        _graph.Nodes.Where(n => IsDone(n, facts));

    // Returns all nodes currently active (not done, predecessors done, activation rules met).
    public IEnumerable<GraphNode> GetActiveNodes(ISet<string> facts) =>
        _graph.Nodes.Where(n =>
            !IsDone(n, facts) &&
            PredecessorsDone(n, facts) &&
            ActivationRulesMet(n, facts));

    // Returns the single highest-priority active player-facing node to display as the primary objective.
    // Prefers objective > safety_barrier > facility_interaction over milestones and bubbles.
    public GraphNode? GetPrimaryObjective(ISet<string> facts) =>
        GetActiveNodes(facts)
            .Where(n => n.NodeType is "objective" or "safety_barrier" or "facility_interaction")
            .OrderByDescending(n => n.Priority ?? 0)
            .FirstOrDefault();

    // Returns the hint text for a node at the requested depth (1-3, clamped).
    public static string GetHintText(GraphNode node, int hintDepth)
    {
        if (node.HintLayers is null)
            return node.Title;

        int depth = System.Math.Max(1, System.Math.Min(hintDepth, 3));

        // Walk down from depth to 1 until a layer is found.
        for (int d = depth; d >= 1; d--)
        {
            if (node.HintLayers.TryGetValue(d.ToString(), out var layer))
                return layer.Text;
        }

        return node.Title;
    }

    // ── Internal helpers ────────────────────────────────────────────────────

    private bool IsDone(GraphNode node, ISet<string> facts)
    {
        // already_satisfied_rules: any single rule passing means the node is done
        // (used for drop-in install onto a progressed save — rules are OR'd at the list level).
        if (node.AlreadySatisfiedRules.Count > 0 &&
            node.AlreadySatisfiedRules.Any(r => RuleParser.Evaluate(r, facts)))
            return true;

        // completion_rules: ALL rules must pass (array is AND'd).
        if (node.CompletionRules.Count > 0 &&
            RuleParser.EvaluateAll(node.CompletionRules, facts))
            return true;

        return false;
    }

    private bool PredecessorsDone(GraphNode node, ISet<string> facts)
    {
        foreach (var predId in node.Predecessors)
        {
            if (!_nodesById.TryGetValue(predId, out var pred))
                continue; // unknown predecessor — skip rather than hard-block
            if (!IsDone(pred, facts))
                return false;
        }
        return true;
    }

    private static bool ActivationRulesMet(GraphNode node, ISet<string> facts)
    {
        if (node.ActivationRules.Count == 0)
            return true;
        // All activation rules must pass.
        return RuleParser.EvaluateAll(node.ActivationRules, facts);
    }
}
