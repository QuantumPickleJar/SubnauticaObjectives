using System.Collections.Generic;
using Newtonsoft.Json;

namespace SubnauticaObjectives.Models;

// Root graph document loaded from campaign.graph.json.
public sealed class CampaignGraph
{
    [JsonProperty("mod_id")]
    public string ModId { get; set; } = string.Empty;

    [JsonProperty("version")]
    public string Version { get; set; } = string.Empty;

    [JsonProperty("facts")]
    public List<string> Facts { get; set; } = new List<string>();

    [JsonProperty("nodes")]
    public List<GraphNode> Nodes { get; set; } = new List<GraphNode>();
}

public sealed class GraphNode
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("node_type")]
    public string NodeType { get; set; } = string.Empty;

    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("priority")]
    public int? Priority { get; set; }

    [JsonProperty("parent_major")]
    public string? ParentMajor { get; set; }

    [JsonProperty("predecessors")]
    public List<string> Predecessors { get; set; } = new List<string>();

    [JsonProperty("successors")]
    public List<string> Successors { get; set; } = new List<string>();

    [JsonProperty("activation_rules")]
    public List<string> ActivationRules { get; set; } = new List<string>();

    [JsonProperty("completion_rules")]
    public List<string> CompletionRules { get; set; } = new List<string>();

    [JsonProperty("already_satisfied_rules")]
    public List<string> AlreadySatisfiedRules { get; set; } = new List<string>();

    [JsonProperty("completion_scope")]
    public string? CompletionScope { get; set; }

    [JsonProperty("hint_layers")]
    public Dictionary<string, HintLayer>? HintLayers { get; set; }

    [JsonProperty("tags")]
    public List<string>? Tags { get; set; }
}

public sealed class HintLayer
{
    [JsonProperty("text")]
    public string Text { get; set; } = string.Empty;

    [JsonProperty("visibility")]
    public string Visibility { get; set; } = string.Empty;
}
