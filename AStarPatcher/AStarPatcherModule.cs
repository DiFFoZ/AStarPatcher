using HarmonyLib;
using SDG.Framework.Modules;
using System;
using UnityEngine;

namespace AStarPatcher;
public class AStarPatcherModule : IModuleNexus
{
    private const string c_HarmonyId = "diffoz-astar";

    private static SDG.Unturned.CommandLineInt s_AstarDebugCommandLine = new("-LogAstar");

    private Harmony? m_Harmony;

    public void initialize()
    {
        m_Harmony = new Harmony(c_HarmonyId);
        m_Harmony.PatchAll(typeof(AStarPatcherModule).Assembly);

        AstarPath.OnAwakeSettings += OnAwakeSettings;
    }

    public void shutdown()
    {
        m_Harmony?.UnpatchAll(c_HarmonyId);
        AstarPath.OnAwakeSettings -= OnAwakeSettings;
    }

    private void OnAwakeSettings()
    {
#if DEBUG
        var pathLog = PathLog.Heavy;
#else
        var pathLog = (PathLog)Mathf.Clamp(s_AstarDebugCommandLine.value, 0, 5);
#endif

        AstarPath.active.logPathResults = pathLog;
    }
}
