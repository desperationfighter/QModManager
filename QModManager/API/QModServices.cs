﻿using QModManager.Checks;

namespace QModManager.API
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Reflection;
    using QModManager.Patching;
    using QModManager.Utility;

    /// <summary>
    /// Services offered to mods.
    /// </summary>
    /// <seealso cref="IQModServices" />
    public class QModServices: IQModServices
    {
        private static readonly Dictionary<string, IQMod> knownMods = new Dictionary<string, IQMod>();

        internal static void LoadKnownMods(List<QMod> loadedMods)
        {
            foreach(QMod mod in loadedMods)
                knownMods.Add(mod.Id, mod);
        }

        private QModServices()
        {
        }

        /// <summary>
        /// Gets the main entry point into the QMod Services.
        /// </summary>
        /// <value>
        /// The main.
        /// </value>
        public static IQModServices Main { get; } = new QModServices();

        /// <summary>
        /// Finds the mod by identifier.
        /// </summary>
        /// <param name="modId">The mod identifier.</param>
        /// <returns></returns>
        public IQMod FindModById(string modId)
        {
            if(knownMods.TryGetValue(modId, out IQMod mod) && mod.Enable)
            {
                return mod;
            }

            return null;
        }

        /// <summary>
        /// Checks whether or not a mod is present based on its ID.
        /// </summary>
        /// <param name="modId">The mod ID.</param>
        /// <returns>
        ///   <c>True</c> if the mod is in the collection of mods to load; Otherwise <c>false</c>.
        /// </returns>
        public bool ModPresent(string modId)
        {
            if(knownMods.TryGetValue(modId, out IQMod mod))
            {
                return mod.Enable;
            }

            return false;
        }

        /// <summary>
        /// Finds the mod by assembly.
        /// </summary>
        /// <param name="modAssembly">The mod assembly.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public IQMod FindModByAssembly(Assembly modAssembly)
        {
            foreach(IQMod mod in knownMods.Values)
            {
                if(mod.LoadedAssembly == modAssembly)
                {
                    return mod;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets a list all mods being tracked by QModManager.
        /// </summary>
        /// <returns>
        /// A read only list of mods containing all of the loaded mods
        /// </returns>
        public ReadOnlyCollection<IQMod> GetAllMods()
        {
            return new ReadOnlyCollection<IQMod>(new List<IQMod>(knownMods.Values));
        }

        /// <summary>
        /// Returns the mod from the assembly which called this method
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public IQMod GetMyMod()
        {
            var modAssembly = Assembly.GetCallingAssembly();
            return FindModByAssembly(modAssembly);
        }

        /// <summary>
        /// Returns a mod from an <see cref="Assembly" />
        /// </summary>
        /// <param name="modAssembly"></param>
        /// <returns></returns>
        public IQMod GetMod(Assembly modAssembly)
        {
            return FindModByAssembly(modAssembly);
        }

        /// <summary>
        /// Returns a mod from an ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IQMod GetMod(string id)
        {
            return FindModById(id);
        }

        /// <summary>
        /// Adds a critical message to the main menu.
        /// Message will stay in the main menu and on the loading screen.
        /// </summary>
        /// <param name="msg">The message to add.</param>
        /// <param name="size">The size of the text.</param>
        /// <param name="color">The color of the text.</param>
        /// <param name="autoformat">Whether or not to apply formatting tags to the message, or show it as it is.</param>
        public void AddCriticalMessage(string msg, int size = MainMenuMessages.defaultSize, string color = MainMenuMessages.defaultColor, bool autoformat = true)
        {
            var callingMod = GetMod(ReflectionHelper.CallingAssemblyByStackTrace());
            MainMenuMessages.Add(msg, callingMod?.DisplayName, size, color, autoformat);
        }

        /// <summary>
        /// Gets the currently running game.
        /// </summary>
        /// <value>
        /// The currently running game.
        /// </value>
        public QModGame CurrentlyRunningGame => Patcher.CurrentlyRunningGame;


        /// <summary>
        /// Gets a value indicating whether Nitrox is being used.
        /// </summary>
        /// <value>
        ///   <c>true</c> if Nitrox is being used; otherwise, <c>false</c>.
        /// </value>
        public bool NitroxRunning => NitroxCheck.IsRunning;

        /// <summary>
        /// Gets a value indicating whether Piracy was detected.
        /// </summary>
        /// <value>
        ///   <c>true</c> if Piracy was detected; otherwise, <c>false</c>.
        /// </value>
        public bool PirateDetected => PirateCheck.PirateDetected;

        /// <summary>
        /// Gets the current Q Mod Manager Version.
        /// </summary>
        /// <value>
        ///   Return Running QMM Version.
        /// </value>
        public Version QMMrunningVersion => Assembly.GetExecutingAssembly().GetName().Version;

        /// <summary>
        /// Gets a value indicating when a Savegame was already loaded (Turn true when entering the Mainmenu again.
        /// </summary>
        /// <value>
        ///   <c>true</c> Mainmenu is entered AFTER a Savegame was loaded already; otherwise, <c>false</c>.
        /// </value>
        public bool AnySavegamewasalreadyloaded => ReturnfromSavegameWarning.AnySavegamewasloaded;
    }
}
