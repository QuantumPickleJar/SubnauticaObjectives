using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SubnauticaObjectives.Models;

// Root graph document loaded from campaign.graph.json.
public sealed class CampaignGraph
{
    [JsonPropertyName("mod_id")]
    public string ModId { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("facts")]
    public List<string> Facts { get; set; } = [];

    [JsonPropertyName("nodes")]
    public List<GraphNode> Nodes { get; set; } = [];
}

public sealed class GraphNode
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("node_type")]
    public string NodeType { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public int? Priority { get; set; }

    [JsonPropertyName("parent_major")]
    public string? ParentMajor { get; set; }

    [JsonPropertyName("predecessors")]
    public List<string> Predecessors { get; set; } = [];

    [JsonPropertyName("successors")]
    public List<string> Successors { get; set; } = [];

    [JsonPropertyName("activation_rules")]
    public List<string> ActivationRules { get; set; } = [];

    [JsonPropertyName("completion_rules")]
    public List<string> CompletionRules { get; set; } = [];

    [JsonPropertyName("already_satisfied_rules")]
    public List<string> AlreadySatisfiedRules { get; set; } = [];

    [JsonPropertyName("completion_scope")]
    public string? CompletionScope { get; set; }

    [JsonPropertyName("hint_layers")]
    public Dictionary<string, HintLayer>? HintLayers { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }
}

public sealed class HintLayer
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("visibility")]
    public string Visibility { get; set; } = string.Empty;
}
