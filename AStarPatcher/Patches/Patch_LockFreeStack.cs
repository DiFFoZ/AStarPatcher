using HarmonyLib;
using Pathfinding;
using Pathfinding.Util;

namespace AStarPatcher.Patches;

[HarmonyPatch(typeof(LockFreeStack))]
internal class Patch_LockFreeStack
{
    private static readonly object s_Lock = new();

    [HarmonyPatch(nameof(LockFreeStack.Push))]
    [HarmonyPrefix]
    public static bool PushPatch(LockFreeStack __instance, Path p)
    {
        lock (s_Lock)
        {
            p.next = __instance.head;
            __instance.head = p;

            return false;
        }
    }

    [HarmonyPatch(nameof(LockFreeStack.PopAll))]
    [HarmonyPrefix]
    public static bool PopAllPatch(LockFreeStack __instance, out Path __result)
    {
        lock (s_Lock)
        {
            __result = __instance.head;
            __instance.head = null;

            return false;
        }
    }
}
