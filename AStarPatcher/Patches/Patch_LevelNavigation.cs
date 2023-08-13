using HarmonyLib;
using Pathfinding;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace AStarPatcher.Patches;

/// <summary>
/// This patch fixes an exception on loading graps from Navigation_*.dat
/// </summary>
/*
 * Exception: Trying to initialize a node when it is not safe to initialize any nodes. Must be done during a graph update
 * AstarPath.InitializeNode (Pathfinding.GraphNode node) (at <79f203b2e95e4ad99df7f800b9c30dd6>:0)
 * Pathfinding.GraphNode..ctor (AstarPath astar) (at <79f203b2e95e4ad99df7f800b9c30dd6>:0)
 * Pathfinding.MeshNode..ctor (AstarPath astar) (at <79f203b2e95e4ad99df7f800b9c30dd6>:0)
 * Pathfinding.TriangleMeshNode..ctor (AstarPath astar) (at <79f203b2e95e4ad99df7f800b9c30dd6>:0)
 * SDG.Unturned.LevelNavigation.buildGraph (SDG.Unturned.River river) (at <da7afc137a6d4041a2cef668152379d7>:0)
 * SDG.Unturned.LevelNavigation.load () (at <da7afc137a6d4041a2cef668152379d7>:0)
 * SDG.Unturned.Level+<init>d__136.MoveNext () (at <da7afc137a6d4041a2cef668152379d7>:0)
 * UnityEngine.SetupCoroutine.InvokeMoveNext (System.Collections.IEnumerator enumerator, System.IntPtr returnValueAddress) (at <966ecbc21bec4138a5192b5e1b2dd41f>:0)
 * UnityEngine.MonoBehaviour:StartCoroutine(IEnumerator)
 * SDG.Unturned.Level:onSceneLoaded(Scene, LoadSceneMode)
 * UnityEngine.SceneManagement.SceneManager:Internal_SceneLoaded(Scene, LoadSceneMode)
 */
[HarmonyPatch(typeof(LevelNavigation), nameof(LevelNavigation.load))]
internal static class Patch_LevelNavigation
{
    private static readonly MethodInfo s_Provider_IsServerGetMethod
        = typeof(Provider).GetProperty(nameof(Provider.isServer), AccessTools.all).GetGetMethod();

    private static readonly Func<River, RecastGraph> s_LevelNavigation_BuildGraphDelegate
        = AccessTools.MethodDelegate<Func<River, RecastGraph>>(typeof(LevelNavigation).GetMethod("buildGraph", AccessTools.all));

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> LoadTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var listInstruction = instructions.ToList();
        for (var i = 0; i < listInstruction.Count; i++)
        {
            var instruction = listInstruction[i];

            yield return instruction;

            if (instruction.Calls(s_Provider_IsServerGetMethod))
            {
                // push brfalse ins
                yield return listInstruction[i + 1];

                yield return CodeInstruction.Call(() => LoadNavigationSafely);

                var retInstruction = instructions.Last();
                Debug.Assert(retInstruction.opcode == OpCodes.Ret);

                yield return retInstruction;
                break;
            }
        }
    }

    /// <summary>
    /// Runs a code from the link when pathfinding is locked/disabled
    /// https://github.com/Unturned-Datamining/Unturned-Datamining/blob/b05516d0b7da1a7f3ad21556c48a4137c5f7c777/Assembly-CSharp/SDG.Unturned/LevelNavigation.cs#L368-L388
    /// </summary>
    public static void LoadNavigationSafely()
    {
        AstarPath.active.AddWorkItem(new AstarPath.AstarWorkItem(_ =>
        {
            var failCount = 0;
            var navigationId = 0;
            while (failCount < 5)
            {
                var path = Level.info.path + "/Environment/Navigation_" + navigationId.ToString(CultureInfo.InvariantCulture) + ".dat";
                if (ReadWrite.fileExists(path, useCloud: false, usePath: false))
                {
                    River river = new River(path, usePath: false);
                    if (river.readByte() > 0)
                    {
                        // calls buildGraph(river);
                        s_LevelNavigation_BuildGraphDelegate(river);
                    }
                    river.closeRiver();
                    failCount = 0;
                }
                else
                {
                    failCount++;
                }
                navigationId++;
            }

            // telling that we completed our job
            return true;
        }));

        // wait until our job completed
        AstarPath.active.FlushWorkItems();

#if DEBUG
        UnturnedLog.info($"Loaded {LevelNavigation.bounds.Count} graphs");
#endif

        if (LevelNavigation.bounds.Count != AstarPath.active.graphs.Length)
        {
            UnturnedLog.error("Navigation bounds count ({0}) does not match graph count ({1}) during server load",
                LevelNavigation.bounds.Count, AstarPath.active.graphs.Length);
        }
    }
}
