using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace Hikaria.EnemyDamageDestroyLimbFix;

[BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
public class EntryPoint : BasePlugin
{
    public override void Load()
    {
        Instance = this;

        m_Harmony = new(PluginInfo.GUID);
        m_Harmony.PatchAll();

        Logs.LogMessage("OK");
    }

    public static EntryPoint Instance { get; private set; }

    private Harmony m_Harmony;
}
