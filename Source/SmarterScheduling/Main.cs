using System.Reflection;
using HarmonyLib;
using Verse;

namespace SmarterScheduling;

[StaticConstructorOnStartup]
internal class Main
{
    static Main()
    {
        var harmony = new Harmony("com.github.harmony.rimworld.maarx.smarterscheduling");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
}