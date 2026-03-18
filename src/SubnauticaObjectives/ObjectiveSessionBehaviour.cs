using SubnauticaObjectives.Facts;
using SubnauticaObjectives.Graph;
using SubnauticaObjectives.Notifications;
using SubnauticaObjectives.PDA;
using UnityEngine;

namespace SubnauticaObjectives;

// MonoBehaviour that waits until the game session is ready (Player.main is valid),
// then runs the one-time startup fact detection pass and seeds the PDA tab.
//
// Attached to the BepInEx plugin GameObject in Plugin.Load().
internal sealed class ObjectiveSessionBehaviour : MonoBehaviour
{
    private bool _initialised;
    private bool _loggedWaitingForPlayer;

    private void Awake()
    {
        Plugin.Log?.LogInfo($"[ObjectiveSessionBehaviour] Awake() called");
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Plugin.Log?.LogInfo($"[ObjectiveSessionBehaviour] Start() called");
    }

    private void Update()
    {
        if (_initialised)
            return;

        // Wait until player singleton is available in an active save.
        if (Player.main == null)
        {
            if (!_loggedWaitingForPlayer)
            {
                Plugin.Log?.LogInfo("[ObjectiveSessionBehaviour] Waiting for Player.main...");
                _loggedWaitingForPlayer = true;
            }
            return;
        }

        _initialised = true;
        Plugin.Log?.LogInfo("[ObjectiveSessionBehaviour] Player.main detected, running startup.");
        RunStartup();
    }

    private static void RunStartup()
    {
        if (Plugin.Registry is null || Plugin.Evaluator is null)
            return;

        Plugin.Log?.LogInfo("[ObjectiveSessionBehaviour] Running startup fact detection.");

        // Populate facts from the current save state.
        StartupFactDetector.Detect(Plugin.Registry, Plugin.Log!);

        var facts = Plugin.Registry.Snapshot();

        // Show the first active objective as a toast.
        var primary = Plugin.Evaluator.GetPrimaryObjective(facts);
        if (primary is not null)
        {
            string hint = GraphEvaluator.GetHintText(primary, Plugin.HintDepth);
            ToastManager.ShowObjectiveChanged(primary.Title, Plugin.HintDepth, hint);
            Plugin.Log?.LogInfo($"[Startup Objective] [{primary.NodeType}] {primary.Id} — \"{hint}\"");
        }
        else
        {
            Plugin.Log?.LogInfo("[Startup Objective] No active objectives found — save may be complete.");
        }

        // Seed the PDA Databank with active objectives.
        ObjectivesPdaTab.Refresh(facts, Plugin.Evaluator, Plugin.HintDepth);
    }
}
