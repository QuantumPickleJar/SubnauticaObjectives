using System.Text.Json;
using GraphInspector;

var graphPath = ResolveGraphPath(args);

if (!File.Exists(graphPath))
{
    Console.Error.WriteLine($"Graph file not found: {graphPath}");
    Environment.Exit(1);
}

var json = await File.ReadAllTextAsync(graphPath);

var options = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true
};

CampaignGraph? graph = JsonSerializer.Deserialize<CampaignGraph>(json, options);

if (graph is null)
{
    Console.Error.WriteLine("Failed to deserialize graph JSON.");
    Environment.Exit(1);
}

Console.WriteLine($"Loaded graph: {graph.ModId} ({graph.Version})");
Console.WriteLine($"Facts: {graph.Facts.Count}");
Console.WriteLine($"Nodes: {graph.Nodes.Count}");
Console.WriteLine();

PrintNodeTypeCounts(graph);
Console.WriteLine();

var issues = ValidateGraph(graph);

if (issues.Count == 0)
{
    Console.WriteLine("No structural issues detected.");
}
else
{
    Console.WriteLine($"Validation issues: {issues.Count}");
    foreach (var issue in issues)
    {
        Console.WriteLine($"- {issue}");
    }
}

static string ResolveGraphPath(string[] args)
{
    if (args.Length > 0)
    {
        return Path.GetFullPath(args[0]);
    }

    // Walk up from the current directory until we find the repo root (marked by .git),
    // then resolve data/campaign.graph.json relative to it.
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir != null)
    {
        if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
        {
            return Path.Combine(dir.FullName, "data", "campaign.graph.json");
        }
        dir = dir.Parent;
    }

    // Fall back to the current directory if no .git root was found.
    return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "data", "campaign.graph.json"));
}

static void PrintNodeTypeCounts(CampaignGraph graph)
{
    Console.WriteLine("Node counts by type:");
    foreach (var group in graph.Nodes.GroupBy(n => n.NodeType).OrderBy(g => g.Key))
    {
        Console.WriteLine($"- {group.Key}: {group.Count()}");
    }
}

static List<string> ValidateGraph(CampaignGraph graph)
{
    var issues = new List<string>();
    var allIds = new HashSet<string>(StringComparer.Ordinal);
    var duplicateIds = new HashSet<string>(StringComparer.Ordinal);

    foreach (var node in graph.Nodes)
    {
        if (!allIds.Add(node.Id))
        {
            duplicateIds.Add(node.Id);
        }
    }

    foreach (var duplicateId in duplicateIds.OrderBy(x => x))
    {
        issues.Add($"Duplicate node id: {duplicateId}");
    }

    var knownIds = graph.Nodes.Select(n => n.Id).ToHashSet(StringComparer.Ordinal);

    foreach (var node in graph.Nodes)
    {
        if (string.IsNullOrWhiteSpace(node.Id))
        {
            issues.Add("A node is missing an id.");
            // Skip further checks that rely on node.Id being meaningful.
            continue;
        }

        foreach (var predecessor in node.Predecessors)
        {
            if (!knownIds.Contains(predecessor))
            {
                issues.Add($"Node '{node.Id}' references missing predecessor '{predecessor}'.");
            }
        }

        foreach (var successor in node.Successors)
        {
            if (!knownIds.Contains(successor))
            {
                issues.Add($"Node '{node.Id}' references missing successor '{successor}'.");
            }
        }

        if (string.IsNullOrWhiteSpace(node.NodeType))
        {
            issues.Add($"Node '{node.Id}' is missing node_type.");
        }

        if (node.CompletionRules.Count == 0)
        {
            issues.Add($"Node '{node.Id}' has no completion_rules.");
        }

        var needsHints = node.NodeType is "objective" or "safety_barrier" or "facility_interaction";
        if (needsHints)
        {
            if (node.HintLayers is null)
            {
                issues.Add($"Node '{node.Id}' is missing hint_layers.");
            }
            else
            {
                foreach (var requiredDepth in new[] { "1", "2", "3" })
                {
                    if (!node.HintLayers.ContainsKey(requiredDepth))
                    {
                        issues.Add($"Node '{node.Id}' is missing hint layer {requiredDepth}.");
                    }
                }
            }
        }
    }

    return issues;
}
