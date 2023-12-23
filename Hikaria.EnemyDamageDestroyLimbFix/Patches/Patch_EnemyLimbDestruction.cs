using Enemies;
using HarmonyLib;
using SNetwork;

namespace Hikaria.EnemyDamageDestroyLimbFix.Patches;

[HarmonyPatch]
public static class Patch_EnemyLimbDestruction
{
    public static void DestroyLimb(Dam_EnemyDamageLimb limb)
    {
        var enemy = limb.m_base.Owner;
        if (!enemy.Alive)
        {
            return;
        }
        int globalID = enemy.GlobalID;
        EnemyLimbDestroyedLookup.TryAdd(globalID, new());
        EnemyLimbDestroyedLookup[globalID].Add(limb.m_limbID);
    }

    public static bool IsLimbDestroyedAfterReceivedDamage(Dam_EnemyDamageLimb limb)
    {
        int globalID = limb.m_base.Owner.GlobalID;
        if (!EnemyLimbDestroyedLookup.TryGetValue(globalID, out var limbSet) || !limbSet.Contains(limb.m_limbID))
        {
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(EnemyAgent), nameof(EnemyAgent.Setup))]
    [HarmonyPostfix]
    private static void EnemyAgent__Setup__Postfix(EnemyAgent __instance)
    {
        if (!SNet.IsMaster)
        {
            return;
        }
        __instance.add_OnDeadCallback(new Action(() => EnemyLimbDestroyedLookup.Remove(__instance.GlobalID)));
    }

    [HarmonyPatch(typeof(EnemyAgent), nameof(EnemyAgent.OnDestroy))]
    [HarmonyPrefix]
    private static void EnemyAgent__OnDestroy__Prefix(EnemyAgent __instance)
    {
        if (!SNet.IsMaster)
        {
            return;
        }
        EnemyLimbDestroyedLookup.Remove(__instance.GlobalID);
    }

    [HarmonyPatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ProcessReceivedDamage))]
    [HarmonyPrefix]
    private static void Dam_EnemyDamageBase__ProcessReceivedDamage__Prefix(Dam_EnemyDamageBase __instance, ref float damage, int limbID)
    {
        if (!SNet.IsMaster)
        {
            return;
        }
        var limb = __instance.DamageLimbs[limbID];
        if (IsLimbDestroyedAfterReceivedDamage(limb))
        {
            if (limb.m_type == eLimbDamageType.Weakspot)
            {
                damage = 0f;
            }
        }
    }

    [HarmonyPatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ProcessReceivedDamage))]
    [HarmonyPostfix]
    private static void Dam_EnemyDamageBase__ProcessReceivedDamage__Postfix(Dam_EnemyDamageBase __instance, int limbID)
    {
        if (!SNet.IsMaster)
        {
            return;
        }
        var limb = __instance.DamageLimbs[limbID];
        if (!IsLimbDestroyedAfterReceivedDamage(limb) && limb.IsDestroyed)
        {
            DestroyLimb(limb);
        }
    }

    private static Dictionary<int, HashSet<int>> EnemyLimbDestroyedLookup = new();
}
