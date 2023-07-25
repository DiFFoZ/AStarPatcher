using HarmonyLib;
using Pathfinding;
using SDG.Unturned;
using System.Collections.Generic;
using System.Linq;
using Path = Pathfinding.Path;

namespace AStarPatcher.Patches;

[HarmonyPatch(typeof(AIPath), nameof(AIPath.stop))]
internal static class Patch_AIPath
{
    [HarmonyPatch]
    [HarmonyPrefix]
    public static void StopPatch(AIPath __instance, Seeker ___seeker, Path ___path)
    {
        // copy from OnDisable
        if (___seeker != null && !___seeker.IsDone())
        {
            var currentPath = ___seeker.GetCurrentPath();
            if (currentPath != null)
            {
                currentPath.Error();
            }
        }

        if (___path != null)
        {
            ___path.Release(__instance);
        }

        // set to null will be handled by original method

        // dump test
#if DEBUG
        var claimedField = typeof(Path).GetField("claimed", AccessTools.all);

        UnturnedLog.info("DUMP: -----");
        var thisPathClaimed = (List<object>)claimedField.GetValue(___path);
        UnturnedLog.info($"Current path claimed: {string.Join(", ", thisPathClaimed.Select(x => x.ToString()))}");
        UnturnedLog.info($"Total created paths: {PathPool<ABPath>.GetTotalCreated()}, in pool: {PathPool<ABPath>.GetSize()}");

        var stack = (Stack<ABPath>)typeof(PathPool<ABPath>).GetField("pool", AccessTools.all).GetValue(null);
        UnturnedLog.info("Paths in pool: " + stack.Count.ToString());
        foreach (var path in stack)
        {
            var claimed = (List<object>)claimedField.GetValue(path);
            if (claimed.Count == 0)
            {
                continue;
            }

            UnturnedLog.info($"Claimed {claimed.Count} times, {string.Join(", ", claimed.Select(x => x.ToString()))}");
        }
#endif
    }
}
