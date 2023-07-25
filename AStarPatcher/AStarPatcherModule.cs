using HarmonyLib;
using SDG.Framework.Modules;

namespace AStarPatcher;
public class AStarPatcherModule : IModuleNexus
{
    private const string c_HarmonyId = "diffoz-astar";
    private Harmony? m_Harmony;

    public void initialize()
    {
        m_Harmony = new Harmony(c_HarmonyId);
        m_Harmony.PatchAll(typeof(AStarPatcherModule).Assembly);
    }

    public void shutdown()
    {
        m_Harmony?.UnpatchAll(c_HarmonyId);
    }
}
