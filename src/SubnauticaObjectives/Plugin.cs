using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using SubnauticaObjectives.Facts;
using SubnauticaObjectives.Graph;
using SubnauticaObjectives.Models;
using SubnauticaObjectives.Notifications;
using SubnauticaObjectives.PDA;

namespace SubnauticaObjectives;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public sealed class Plugin : BasePlugin
{
    // Singleton accessors used by patches and the session behaviour.
    internal static ManualLogSource? Log { get; private set; }
    internal static FactRegistry?   Registry { get; private set; }
    internal static GraphEvaluator? Evaluator { get; private set; }
    internal static CampaignGraph?  Graph { get; private set; }

    // Hint depth used for display. Could later be read from a config file.
    internal static int HintDepth { get; private set; } = 1;

    public override void Load()
    {
        Log = base.Log;
        Log.LogInfo($"Loading {PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION}");

        // Resolve the campaign graph path from the plugin directory.
        string pluginDir = System.IO.Path.GetDirectoryName(
            System.Reflection.Assembly.GetExecutingAssembly().Location)!;
        string graphPath = GraphLoader.DefaultPath(pluginDir);

        Graph = GraphLoader.Load(graphPath, Log);
        if (Graph is null)
        {
            Log.LogError("Plugin disabled — campaign graph could not be loaded.");
            return;
        }

        // Initialize subsystems.
        Registry = new FactRegistry();
        Evaluator = new GraphEvaluator(Graph);

        ToastManager.Initialize(Log);
        ObjectivesPdaTab.Initialize(Log);

        // Wire the fact-added callback to re-evaluate and show a toast when facts change.
        Registry.OnFactAdded += OnFactAdded;

        // Apply Harmony patches (StoryGoalPatches, KnownTechPatches).
        new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();

        // Attach the per-session MonoBehaviour that runs startup detection.
        AddComponent<ObjectiveSessionBehaviour>();

        Log.LogInfo($"{PluginInfo.PLUGIN_NAME} loaded successfully.");
    }

    // Called each time a new fact is added (at runtime, after startup bulk-load).
    private static void OnFactAdded(string fact)
    {
        if (Registry is null || Evaluator is null || Graph is null)
            return;

        var facts = Registry.Snapshot();
        var primary = Evaluator.GetPrimaryObjective(facts);

        if (primary is null)
        {
            ToastManager.Show("All objectives complete!");
            return;
        }

        string hint = GraphEvaluator.GetHintText(primary, HintDepth);
        ToastManager.ShowObjectiveChanged(primary.Title, HintDepth, hint);
        Log?.LogInfo($"[Objective] Active: [{primary.NodeType}] {primary.Id} — \"{hint}\"");

        // Refresh the PDA Databank entries.
        ObjectivesPdaTab.Refresh(facts, Evaluator, HintDepth);
    }
}
