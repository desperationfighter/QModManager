﻿namespace QModManager
{
    using HarmonyLib;
    using QModManager.API;
    using QModManager.Patching;
    using QModManager.Utility;
    using System.Reflection;
    using UnityEngine.Events;
    using System.Collections.Generic;
    using System.Linq;
    using QModManager.DataStructures;
    using System;

    internal static class OptionsManager
    {
        internal static string ModsListTabName = "QMods List";
        internal static int ModsTab;
        internal static int ModListTab;
        internal static List<SimpleModDataTemplate> ModList;
        public static List<SimpleModDataTemplate> ModListPendingChanges;
        //Dedicated Indicator to do not loosing change Status on Modlist
        public static bool PendingChangesOnModList;

        [HarmonyPatch(typeof(uGUI_OptionsPanel), nameof(uGUI_OptionsPanel.AddTabs))]
        internal static class OptionsPatch
        {
            [HarmonyPostfix]
            internal static void Postfix(uGUI_OptionsPanel __instance)
            {
                #region Mod Config
                ModsTab = __instance.AddTab("Mods");
                __instance.AddHeading(ModsTab, "QModManager");

                MethodInfo AddToggleOption = null; 
                if(Patcher.CurrentlyRunningGame == QModGame.Subnautica)
                {
                    AddToggleOption = typeof(uGUI_OptionsPanel).GetMethod(nameof(AddToggleOption), new System.Type[] { typeof(int), typeof(string), typeof(bool), typeof(UnityAction<bool>) });
                    AddToggleOption.Invoke(__instance, new object[] { ModsTab, "Check for updates", Config.CheckForUpdates, new UnityAction<bool>(value => Config.CheckForUpdates = value) });

                    AddToggleOption.Invoke(__instance, new object[] { ModsTab, "Enable console", Config.EnableConsole, new UnityAction<bool>(value =>
                {
                    Config.EnableConsole = value;
#if SUBNAUTICA_STABLE
                    DevConsole.disableConsole = !value;
                    UnityEngine.PlayerPrefs.SetInt("UWE.DisableConsole", value ? 0 : 1);
#elif BELOWZERO || SUBNAUTICA_EXP
                    PlatformUtils.SetDevToolsEnabled(value);
#endif
                }) });

                    AddToggleOption.Invoke(__instance, new object[] { ModsTab, "Enable debug logs", Config.EnableDebugLogs, new UnityAction<bool>(value => Config.EnableDebugLogs = value) });
                    AddToggleOption.Invoke(__instance, new object[] { ModsTab, "Enable developer mode", Config.EnableDevMode, new UnityAction<bool>(value => Config.EnableDevMode = value) });
                }
                else
                {
                    AddToggleOption = typeof(uGUI_OptionsPanel).GetMethod(nameof(AddToggleOption), new System.Type[] { typeof(int), typeof(string), typeof(bool), typeof(UnityAction<bool>), typeof(string) });
                    AddToggleOption.Invoke(__instance, new object[] { ModsTab, "Check for updates", Config.CheckForUpdates, new UnityAction<bool>(value => Config.CheckForUpdates = value), null });

                    AddToggleOption.Invoke(__instance, new object[] { ModsTab, "Enable console", Config.EnableConsole, new UnityAction<bool>(value =>
                {
                    Config.EnableConsole = value;
#if SUBNAUTICA_STABLE
                    DevConsole.disableConsole = !value;
                    UnityEngine.PlayerPrefs.SetInt("UWE.DisableConsole", value ? 0 : 1);
#elif BELOWZERO || SUBNAUTICA_EXP
                    PlatformUtils.SetDevToolsEnabled(value);
#endif
                }), null });

                    AddToggleOption.Invoke(__instance, new object[] { ModsTab, "Enable debug logs", Config.EnableDebugLogs, new UnityAction<bool>(value => Config.EnableDebugLogs = value), null });
                    AddToggleOption.Invoke(__instance, new object[] { ModsTab, "Enable developer mode", Config.EnableDevMode, new UnityAction<bool>(value => Config.EnableDevMode = value), null });
                }
                #endregion Mod Config

                #region Mod List
                //Start creating Ingame Mod List
                Logger.Log(Logger.Level.Debug, "OptionsMenu - Start creating Modlist");
                
                //Reset ModList and Pending Changes on Relead the List Menu - We do not want to save any Data or take over Data from previous time here
                ModList = new List<SimpleModDataTemplate>();
                ModListPendingChanges = new List<SimpleModDataTemplate>();
                PendingChangesOnModList = false;
                IEnumerable<IQMod> mods = QModServices.Main.GetAllMods().OrderBy(mod => mod.DisplayName);

                //test
                Logger.Log(Logger.Level.Debug, $"NANANANANANA1 - Mods - test1");
                try
                {
                    List<QMod> testmods = new List<QMod>();
                    foreach (var iQMod in QModServices.Main.GetAllMods())
                        if (iQMod is QMod qMod)
                            testmods.Add(qMod);

                    foreach (var _mod in testmods)
                    {
                        Logger.Log(Logger.Level.Debug, $"NANANANANANA1 - Mods - {_mod.Id} , {_mod.Enable} , {_mod.SubDirectory}");
                    }
                }
                catch
                {

                }
                Logger.Log(Logger.Level.Debug, $"NANANANANANA1 - Mods - test2");
                try
                {
                    List<QMod> testmods2 = QModServices.Main.GetAllMods().OrderBy(mod => mod.DisplayName) as List<QMod>;

                    foreach (var _mod in testmods2)
                    {
                        Logger.Log(Logger.Level.Debug, $"NANANANANANA2 - Mods - {_mod.Id} , {_mod.Enable} , {_mod.SubDirectory}");
                    }

                }
                catch
                {

                }
                // ENDE TEST

                List<IQMod> activeMods = new List<IQMod>();
                List<IQMod> inactiveMods = new List<IQMod>();

                //Create new Tab in the Menu
                ModListTab = __instance.AddTab("QMods List");

                //Add QMM Informations
#if SUBNAUTICA_EXP || BELOWZERO_EXP
                __instance.AddHeading(ModListTab, $"QModManager -Experimental- running version {Assembly.GetExecutingAssembly().GetName().Version.ToStringParsed()}");
#else
                __instance.AddHeading(ModListTab, $"QModManager running version {Assembly.GetExecutingAssembly().GetName().Version.ToStringParsed()}");
#endif
                //Add SML Informations to the Top as its pretty Important
                var modprio_sml = QModServices.Main.GetMod("SMLHelper");
                if (modprio_sml != null)
                {
                    __instance.AddHeading(ModListTab, $"{modprio_sml.DisplayName} {(modprio_sml.Enable ? $"v{modprio_sml.ParsedVersion}" : string.Empty)} is {(modprio_sml.IsLoaded ? "enabled" : "disabled")}");
                }
                else
                {
                    __instance.AddHeading(ModListTab, $"SMLHelper is not installed");
                }

                //Now lets create the Mod List
                foreach (var mod in mods)
                {
                    
                    if (mod.Enable)
                    {
                        activeMods.Add(mod);

                    }
                    else
                    {
                        inactiveMods.Add(mod);
                    }

                    //Write down a Temporary reduced Modlist that is interactable (The QMM Modlist is only Readonly and has many information we do not need for now)
                    SimpleModDataTemplate _tmpmod = new SimpleModDataTemplate();
                    try
                    {
                        _tmpmod.ID = mod.Id;
                        if (mod.LoadedAssembly != null)
                        {
                            //Logger.Log(Logger.Level.Debug, $"NANANANANANA - Just checking {mod.DisplayName} ; {mod.AssemblyName} ; {mod.LoadedAssembly.Location}");
                            _tmpmod.PathToAssemblyFile = mod.LoadedAssembly.Location;
                        }
                        else
                        {
                            //Logger.Log(Logger.Level.Debug, $"NANANANANANA - Just checking {mod.DisplayName} has no Loaded Assembly");
                        }   
                        _tmpmod.Enabled = mod.Enable;
                        ModList.Add(_tmpmod);
                    }
                    catch
                    {
                        Logger.Log(Logger.Level.Debug, "NANANANANANA - Error on adding Temp Mod to Modlist");
                    }

                }

                //Now show some Statistics ahead of the List.
                __instance.AddHeading(ModListTab, $"- - Statistics - -");
                __instance.AddHeading(ModListTab, $"{mods.Count()} Mods found");
                __instance.AddHeading(ModListTab, $"{activeMods.Count} Mods enabled");
                __instance.AddHeading(ModListTab, $"{inactiveMods.Count} Mods disabled");

                //Now we write down the actull List with all Mods. Starting with the Active Mods
                __instance.AddHeading(ModListTab, $"- - List of currently running Mods - -");
                foreach (var mod in activeMods)
                {
                    //This is the Header Entry
                    __instance.AddHeading(ModListTab, $"{mod.DisplayName} v{mod.ParsedVersion.ToString()} from {mod.Author}");

                    //This is the Collapse SubMenu of the Entry
                    MethodInfo Modlist_AddToggleOption = null;
                    Modlist_AddToggleOption = typeof(uGUI_OptionsPanel).GetMethod(nameof(AddToggleOption), new System.Type[] { typeof(int), typeof(string), typeof(bool), typeof(UnityAction<bool>) });                    
                    Modlist_AddToggleOption.Invoke(__instance, new object[] { ModListTab, "Enable Mod", ModList[ModList.FindIndex(mdt => mdt.ID == mod.Id)].Enabled , new UnityAction<bool>(value => OnChangeModStatus(ModList[ModList.FindIndex(mdt => mdt.ID == mod.Id)], value, __instance)) });
                }

                //Continue with Disabled Mods
                __instance.AddHeading(ModListTab, $"- - List of Disabled Mods - -");
                foreach (var mod in inactiveMods)
                {
                    //This is the Header Entry
                    __instance.AddHeading(ModListTab, $"{mod.DisplayName} from {mod.Author}");

                    //This is the Collapse SubMenu of the Entry
                    MethodInfo Modlist_AddToggleOption = null;
                    Modlist_AddToggleOption = typeof(uGUI_OptionsPanel).GetMethod(nameof(AddToggleOption), new System.Type[] { typeof(int), typeof(string), typeof(bool), typeof(UnityAction<bool>) });
                    Modlist_AddToggleOption.Invoke(__instance, new object[] { ModListTab, "Enable Mod", ModList[ModList.FindIndex(mdt => mdt.ID == mod.Id)].Enabled, new UnityAction<bool>(value => OnChangeModStatus(ModList[ModList.FindIndex(mdt => mdt.ID == mod.Id)], value, __instance)) });
                }

                Logger.Log(Logger.Level.Debug, "OptionsMenu - ModList - Creating Modlist Ending");
                
                #endregion Mod List
            }

            static void OnChangeModStatus(SimpleModDataTemplate ChangedMod,bool status, uGUI_OptionsPanel __instance)
            {
                Logger.Log(Logger.Level.Debug, $"WOLOLOLOLOLO - the submit id is {ChangedMod.ID} and the Status is {status}");
                ChangedMod.Enabled = status;

                SimpleModDataTemplate _modexist = new SimpleModDataTemplate();
                try
                {
                    //Is there a better way than try/catch using Find while determind a null or Empty when no Mod is found ?
                    _modexist = ModListPendingChanges.Find(mdt => mdt.ID == ChangedMod.ID);
                }
                catch
                {
                    _modexist = null;
                }

                if (_modexist == null)
                {
                    try
                    {
                        ModListPendingChanges.Add(ChangedMod);
                    }
                    catch
                    {
                        Logger.Log(Logger.Level.Debug, "OptionsMenu - ModList - Error on Adding Mod to Pending Status Change List");
                    }
                }
                else if(_modexist.ID == ChangedMod.ID)
                {
                    try
                    {
                        ModListPendingChanges.Remove(ChangedMod);
                    }
                    catch
                    {
                        Logger.Log(Logger.Level.Debug, "OptionsMenu - ModList - Error on Removing Mod to Pending Status Change List");
                    }
                }
                else
                {
                    Logger.Log(Logger.Level.Debug, "OptionsMenu - ModList - Error on Adding AND Removing Mod to Pending Status Change List.");
                    //mhh that should not happen....
                }

                if (ModListPendingChanges.Count == 0)
                {
                    ////disable Apply Button
                    PendingChangesOnModList = false;
                }
                else
                {
                    //enable Apply Button
                    __instance.applyButton.gameObject.SetActive(true);
                    PendingChangesOnModList = true;
                }
            }
        }

        [HarmonyPatch(typeof(uGUI_OptionsPanel), nameof(uGUI_OptionsPanel.OnApplyButton))]
        internal static class OptionsPatch_OnApplyButton
        {
            [HarmonyPostfix]
            internal static void Postfix(uGUI_OptionsPanel __instance)
            {
                //Warning Save Button is shared over the hole Option Menu !
                if (OptionsManager.ModListPendingChanges.Count > 0)
                {
                    foreach (SimpleModDataTemplate _mod in OptionsManager.ModListPendingChanges)
                    {
                        IOUtilities.ChangeModStatustoFile(_mod);
                    }
                }
                //just in case disable to button again
                __instance.applyButton.gameObject.SetActive(false);
            }
        }
    }
}
