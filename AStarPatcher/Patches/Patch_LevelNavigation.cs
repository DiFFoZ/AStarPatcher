using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Pathfinding;
using SDG.Unturned;

namespace AStarPatcher.Patches;

[HarmonyPatch(typeof(LevelNavigation), "buildGraph")]
internal static class Patch_LevelNavigation
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> LoadTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);

        var operand = AccessTools.Constructor(typeof(TriangleMeshNode), new[] { typeof(AstarPath) });

        return matcher
            .End()
            .SearchBackwards(ci => ci.Is(OpCodes.Newobj, operand))
            .ThrowIfInvalid("Search failed")
            .Advance(-4)
            .ThrowIfInvalid("Advance failed")
            .Insert(CodeInstruction.LoadField(typeof(AstarPath), nameof(AstarPath.active)),
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(AstarPath), nameof(AstarPath.BlockUntilPathQueueBlocked))))
            .InstructionEnumeration();
    }
}
