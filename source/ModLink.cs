using Entitas;
using HarmonyLib;
using PhantomBrigade;
using PhantomBrigade.Data;
using PhantomBrigade.Mods;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System;
using UnityEngine;
using Debug = UnityEngine.Debug;

// INTRODUCTION / USAGE NOTES:
//  - The project's reference to 'System' must point to the Phantom Brigade one, not Microsoft.
//    It was necessary to add
//    C:\Program Files(x86)\Steam\steamapps\common\Phantom Brigade\PhantomBrigade_Data\Managed\
//    to the project's Reference Paths, which unfortunately isn't stored in the .csproj.
//     - (and add reference to UnityEngine.JSONSerializeModule to the project)
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

// POSSIBLE IMPROVEMENTS:
//  - I'd the thought that equipment might suffer varying degrees of damage. This could affect
//    how many salvage-points are required. (Other things are conceivable, such as reducing
//    item level/quality/mods, or requiring 'further repair', which might require expending
//    some materials to salvage the part in-tact, e.g., maybe salvaging a damaged blue costs
//    5..50 common gems and 1..3 rare gems.)

namespace ModExtensions
{
    //==================================================================================================
    // (Having a class derived from ModLink might (?) be necessary, but the overrides are probably just
    //  leftover 'hello world' stuff at this point.)
    public class ModLinkCustom : ModLink
    {
#if true
        public static ModLinkCustom ins;

        public override void OnLoadStart()
        {
            ins = this;
            //Debug.Log($"OnLoadStart");
        }

        public override void OnLoad(Harmony harmonyInstance)
        {
            base.OnLoad(harmonyInstance);
            MyModConfigManager.Load();
            //Debug.Log($"OnLoad | Mod: {modID} | Index: {modIndexPreload} | Path: {modPath}");
        }
#endif
    }//class

    [Serializable]
    public class MyModConfig
    {
        public float salvageable_destroyed_part_success_chance = 0.5f; // (default; 0..1)
    }//class

    public class MyModConfigManager {
        private static readonly string ConfigPath = Path.Combine(Application.persistentDataPath, "enhanced_salvage_of_destroyed_parts_config.json");
        public static MyModConfig Config { get; private set; }
        public static void Load()
        {
            Debug.Log($"[EnhancedSalvageOfDestroyedParts] Loading: {ConfigPath}");
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    Config = JsonUtility.FromJson<MyModConfig>(json);
                    if (Config == null)
                        throw new Exception("Parsed config was null");
                }
                else
                {
                    CreateDefault();
                }
            }
            catch (Exception e)
            {
                Debug.Log($"[EnhancedSalvageOfDestroyedParts] Failed to create/load config: {e}");
            }
            Debug.Log($"[EnhancedSalvageOfDestroyedParts] loaded salvageable_destroyed_part_success_chance={Config.salvageable_destroyed_part_success_chance}");
        }
        private static void CreateDefault()
        {
            Config = new MyModConfig();
            Save();
        }
        public static void Save()
        {
            string json = JsonUtility.ToJson(Config, true);
            File.WriteAllText(ConfigPath, json);
        }
    }//class

    //+================================================================================================+
    //||                                                                                              ||
    //+================================================================================================+
    public class Patches
    {
        //-------------------------------------------------------------------------------------------
        // The game's PreparePartForSalvage() function has a bunch of special considerations and looks
        // like it's subject to significant change. So for simplicity and to reduce chance of this mod
        // breaking something terribly for a future Phantom Brigade version, we'll just intercept the
        // query of the difficulty setting that controls whether destroyed parts are salvageable. (This
        // could conceivably stop working in some future PB version, but that's a safer mode of failure.)
        //
        // "Dear Harmony, please call into this GiveSalvageChanceForDestroyedPart class whenever DifficultyUtility.GetFlag() runs"
        [HarmonyPatch(typeof(DifficultyUtility), MethodType.Normal), HarmonyPatch("GetFlag")]
        public class GiveSalvageChanceForDestroyedPart
        {
            // "Dear Harmony, due to this function being named Prefix(), please call this RollForSalvage() function
            //  BEFORE that DifficultyUtility.GetFlag() runs (and depending on what I say, either call the normal
            //  GetFlag() or use the result I give instead)"
            public static bool Prefix(string key, ref bool __result)
            {
                if(key == "combat_salvage_allows_destroyed")
                {
                    float roll = UnityEngine.Random.Range(0f, 1f);
                    float chance = MyModConfigManager.Config.salvageable_destroyed_part_success_chance;
                    __result = (roll < chance);
                    Debug.Log($"[EnhancedSalvageOfDestroyedParts] GiveSalvageChanceForDestroyedPart: roll={roll}, chance={chance}; salvageable={__result}");
                    return false; // "no need to call the original function; just use my __result"
                }
                return true; // "I don't care about this case; go ahead and run the original function like normal"
            }//func
        }//class GiveSalvageChanceForDestroyedPart
    }//class Patches
}//namespace
