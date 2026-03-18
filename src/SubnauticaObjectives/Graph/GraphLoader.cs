using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BepInEx.Logging;
using SubnauticaObjectives.Models;

namespace SubnauticaObjectives.Graph;

// Loads campaign.graph.json from the plugin's data directory.
// Expected location: BepInEx/plugins/SubnauticaObjectives/data/campaign.graph.json
public static class GraphLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static CampaignGraph? Load(string path, ManualLogSource log)
    {
        if (!File.Exists(path))
        {
            log.LogError($"[GraphLoader] Campaign graph not found at: {path}");
            return null;
        }

        try
        {
            string json = File.ReadAllText(path);
            var graph = JsonSerializer.Deserialize<CampaignGraph>(json, SerializerOptions);
            if (graph is null)
            {
                log.LogError("[GraphLoader] Deserialization returned null.");
                return null;
            }

            log.LogInfo($"[GraphLoader] Loaded {graph.ModId} v{graph.Version} — {graph.Nodes.Count} nodes, {graph.Facts.Count} facts.");
            return graph;
        }
        catch (JsonException ex)
        {
            log.LogError($"[GraphLoader] JSON parse error: {ex.Message}");
            return null;
        }
    }

    // Resolves the expected path for campaign.graph.json relative to a given plugin directory.
    public static string DefaultPath(string pluginDirectory) =>
        Path.Combine(pluginDirectory, "data", "campaign.graph.json");
}
