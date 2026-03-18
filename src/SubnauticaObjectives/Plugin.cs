using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SubnauticaObjectives.Facts;
using SubnauticaObjectives.Graph;
using SubnauticaObjectives.Models;
using SubnauticaObjectives.Notifications;
using SubnauticaObjectives.PDA;
using UnityEngine;

namespace SubnauticaObjectives;

/// <summary>
/// BepInEx v5 Mono plugin for Subnautica.
///
/// v5 plugins must derive from BaseUnityPlugin so Chainloader can instantiate them.
/// </summary>
[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public sealed class Plugin : BaseUnityPlugin
{
    // Singleton accessors used by patches and the session behaviour.
    public static ManualLogSource? Log { get; private set; }
    public static FactRegistry?   Registry { get; private set; }
    public static GraphEvaluator? Evaluator { get; private set; }
    public static CampaignGraph?  Graph { get; private set; }

    // Hint depth used for display. Could later be read from a config file.
    public static int HintDepth { get; private set; } = 1;

    private static bool _initialized;

    private void Awake()
    {
        if (_initialized)
            return;

        _initialized = true;

        try
        {
            // Create a manual log source for this plugin
            Log = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);
            Log.LogInfo($"===== PLUGIN INIT START =====");
            Log.LogInfo($"Loading {PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION}");

            // Resolve the campaign graph path from the plugin directory.
            string pluginDir = System.IO.Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location)!;
            Log.LogInfo($"Plugin directory: {pluginDir}");
            
            string graphPath = GraphLoader.DefaultPath(pluginDir);
            Log.LogInfo($"Graph path: {graphPath}");
            Log.LogInfo($"Graph file exists: {System.IO.File.Exists(graphPath)}");

            Graph = GraphLoader.Load(graphPath, Log);
            if (Graph is null)
            {
                Log.LogError("Plugin disabled — campaign graph could not be loaded.");
                return;
            }
            Log.LogInfo($"Graph loaded: {Graph.Nodes.Count} nodes");

            // Initialize subsystems.
            Registry = new FactRegistry();
            Log.LogInfo($"FactRegistry initialized");
            
            Evaluator = new GraphEvaluator(Graph);
            Log.LogInfo($"GraphEvaluator initialized");

            ToastManager.Initialize(Log);
            Log.LogInfo($"ToastManager initialized");
            
            ObjectivesPdaTab.Initialize(Log);
            Log.LogInfo($"ObjectivesPdaTab initialized");

            // Wire the fact-added callback to re-evaluate and show a toast when facts change.
            Registry.OnFactAdded += OnFactAdded;
            Log.LogInfo($"OnFactAdded callback wired");

            // Apply Harmony patches (StoryGoalPatches, KnownTechPatches).
            new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();
            Log.LogInfo($"Harmony patches applied");

            // Attach the per-session MonoBehaviour that runs startup fact detection.
            gameObject.AddComponent<ObjectiveSessionBehaviour>();
            Log.LogInfo($"Session behaviour attached");

            Log.LogInfo($"===== PLUGIN INIT SUCCESS =====");
        }
        catch (System.Exception ex)
        {
            Log?.LogError($"===== PLUGIN INIT FAILED =====");
            Log?.LogError($"Error initializing plugin: {ex}");
            if (ex.InnerException != null)
                Log?.LogError($"Inner: {ex.InnerException}");
        }
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
