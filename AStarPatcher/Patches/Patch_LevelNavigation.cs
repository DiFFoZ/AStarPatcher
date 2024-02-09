using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using SDG.Unturned;

namespace AStarPatcher.Patches;

[HarmonyPatch(typeof(LevelNavigation), "buildGraph")]
internal static class Patch_LevelNavigation
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> LoadTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);

        var method = AccessTools.Method(typeof(AstarPath), nameof(AstarPath.FlushWorkItems));

        return matcher
            .End()
            .SearchBackwards(ci => ci.Calls(method))
            .ThrowIfInvalid("Search failed")
            .Advance(-1)
            .ThrowIfInvalid("Advance failed")
            .SetOpcodeAndAdvance(OpCodes.Ldc_I4_1)
            .InstructionEnumeration();
    }
}
