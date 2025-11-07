using HarmonyLib;
using RimWorld;
using Verse;

namespace CustomResearchView;

[StaticConstructorOnStartup]
public static class HarmonyInit
{
    static HarmonyInit()
    {
        var harmony = new Harmony("com.openai.customresearchview");
        harmony.PatchAll();
        Log.Message("[CustomResearchView] Harmony patches applied.");
    }
}
