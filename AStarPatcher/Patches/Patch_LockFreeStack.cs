using HarmonyLib;
using Pathfinding.Util;
using SDG.Unturned;
using Path = Pathfinding.Path;

namespace AStarPatcher.Patches;

[HarmonyPatch(typeof(LockFreeStack))]
internal static class Patch_LockFreeStack
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

#if DEBUG
            Path? next = __instance.head;
            var counter = next == null ? 0 : 1;
            while ((next = next?.next) != null)
            {
                counter++;
            }
            UnturnedLog.info($"[Push] Amount path refs: {counter}");
#endif

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

#if DEBUG
            Path? next = __result;
            var counter = next == null ? 0 : 1;
            while ((next = next?.next) != null)
            {
                counter++;
            }

            if (counter != 0)
            {
                UnturnedLog.info($"[Pop] Amount path refs: {counter}");
            }
#endif

            return false;
        }
    }
}
