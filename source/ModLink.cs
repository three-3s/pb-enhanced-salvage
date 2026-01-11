using Entitas;
using HarmonyLib;
using PhantomBrigade;
using PhantomBrigade.Data;
using PhantomBrigade.Mods;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Debug = UnityEngine.Debug;

// INTRODUCTION / USAGE NOTES:
//  - The project's reference to 'System' must point to the Phantom Brigade one, not Microsoft.
//    It was necessary to add
//    C:\Program Files(x86)\Steam\steamapps\common\Phantom Brigade\PhantomBrigade_Data\Managed\
//    to the project's Reference Paths, which unfortunately isn't stored in the .csproj.
//  - Debug.Log goes to LocalLow/Brace.../.../Player.log
//  - Harmony.Debug = true + FileLog.Log (and FlushBuffer) goes to desktop harmony.log.txt
//  - You may want to read more about (or ask a chatbot about):
//     - How to use eg dnSpy to decompile & search the Phantom Brigade C# module assemblies.
//     - General info about the 'Entitas' Entity Component System.
//     - Explain what the HarmonyPatch things are.
//  - Note that modding this game via C# has some significant overlap with some other heavily
//    modded games such as RimWorld (another Unity game (+HarmonyLib)).
//  - Other basic getting-started info:
//     - https://github.com/BraceYourselfGames/PB_ModSDK/wiki/Mod-system-overview#libraries
//     - https://wiki.braceyourselfgames.com/en/PhantomBrigade/Modding/ModSystem
//

namespace ModExtensions
{
    //==================================================================================================
    // (Having a class derived from ModLink might (?) be necessary, but the overrides are probably just
    //  leftover 'hello world' stuff at this point.)
    public class ModLinkCustom : ModLink
    {
#if false
        public static ModLinkCustom ins;

        public override void OnLoadStart()
        {
            ins = this;
            //Debug.Log($"OnLoadStart");
        }

        public override void OnLoad(Harmony harmonyInstance)
        {
            base.OnLoad(harmonyInstance);
            Debug.Log($"OnLoad | Mod: {modID} | Index: {modIndexPreload} | Path: {modPath}");
        }
#endif
    }//class

    //+================================================================================================+
    //||                                                                                              ||
    //+================================================================================================+
    public class Patches
    {
        static readonly float success_probablity = 0.5f; //3todo.impl

        //-------------------------------------------------------------------------------------------
        // "Dear Harmony, please call into this GiveSalvageChanceForDestroyedPart class whenever DifficultyUtility.GetFlag() runs"
        [HarmonyPatch(typeof(DifficultyUtility), MethodType.Normal), HarmonyPatch("GetFlag")]
        public class GiveSalvageChanceForDestroyedPart
        {
            // "Dear Harmony, please call this RollForSalvage() function BEFORE that DifficultyUtility.GetFlag() runs
            // (and depending on what I say, either call the normal GetFlag() or use the result I give instead)"
            public static bool Prefix(string key, ref bool __result)
            {
                if(key == "combat_salvage_allows_destroyed")
                {
                    float roll = UnityEngine.Random.Range(0f, 1f);
                    __result = (roll < success_probablity);
                    Debug.Log($"GiveSalvageChanceForDestroyedPart: roll={roll}; allow salvage = {__result}"); //3todo.rem
                    return false; // "no need to call the original function; just use my __result"
                }
                return true; // "I don't care about this case; go ahead and run the original function"
            }//func
        }//class GiveSalvageChanceForDestroyedPart
    }//class Patches
}//namespace
