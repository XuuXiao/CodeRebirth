﻿
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;

namespace CodeRebirth.Configs {
    public class CodeRebirthConfig {
        public ConfigEntry<int> ConfigWalletCost { get; private set; }
        public ConfigEntry<bool> ConfigWalletEnabled { get; private set; }
        public ConfigEntry<string> ConfigMoneyRarity { get; private set; }
        public ConfigEntry<bool> ConfigMoneyScrapEnabled { get; private set; }
        public ConfigEntry<string> ConfigMeteorShowerMoonList { get; private set; }
        public CodeRebirthConfig(ConfigFile configFile) {
            ConfigMeteorShowerMoonList = configFile.Bind("Weather Options",
                                                "Meteor Shower | List",
                                                "Modded, Vanilla",
                                                "List of moons with the Meteor Shower Weather (Vanilla moons need Level at the end of their name, but modded do not).");
            ConfigWalletEnabled = configFile.Bind("Shop Options",
                                                "Wallet Item | Enabled",
                                                true,
                                                "Enables/Disables the Wallet showing up in shop");
            ConfigWalletCost = configFile.Bind("Shop Options",
                                                "Wallet Item | Cost",
                                                250,
                                                "Cost of Wallet");
            ConfigMoneyRarity = configFile.Bind("Scrap Options",
                                                "Money Scrap | Rarity",
                                                "Modded@0,ExperimentationLevel@0,AssuranceLevel@0,VowLevel@0,OffenseLevel@0,MarchLevel@0,RendLevel@0,DineLevel@0,TitanLevel@0",
                                                "Enables/Disables the Wallet showing up in shop");
            ConfigMoneyScrapEnabled = configFile.Bind("Scrap Options",
                                                "Scrap | Enabled",
                                                true,
                                                "Enables/Disables the Money showing up in the Factory");
            ClearUnusedEntries(configFile);
            Plugin.Logger.LogInfo("Setting up config for CodeRebirth plugin...");
        }
        private void ClearUnusedEntries(ConfigFile configFile) {
            // Normally, old unused config entries don't get removed, so we do it with this piece of code. Credit to Kittenji.
            PropertyInfo orphanedEntriesProp = configFile.GetType().GetProperty("OrphanedEntries", BindingFlags.NonPublic | BindingFlags.Instance);
            var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp.GetValue(configFile, null);
            orphanedEntries.Clear(); // Clear orphaned entries (Unbinded/Abandoned entries)
            configFile.Save(); // Save the config file to save these changes
        }
    }
}