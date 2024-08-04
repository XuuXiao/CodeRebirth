﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using CodeRebirth.MapStuff;
using CodeRebirth.Util.Extensions;
using CodeRebirth.Util.Spawning;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using CodeRebirth.Util;
using System.Text.RegularExpressions;
using UnityEngine.AI;

namespace CodeRebirth.Patches;

[HarmonyPatch(typeof(RoundManager))]
static class RoundManagerPatch {
	internal static List<SpawnableFlora> spawnableFlora = [];
    
	[HarmonyPatch(nameof(RoundManager.SpawnOutsideHazards)), HarmonyPostfix]
	static void SpawnOutsideMapObjects() {
		if (Plugin.ModConfig.ConfigFloraEnabled.Value) SpawnFlora();
        
		if (!RoundManager.Instance.IsHost) return;
		if (Plugin.ModConfig.ConfigItemCrateEnabled.Value) SpawnCrates();
		
	}

	static void SpawnFlora() {
		Plugin.Logger.LogInfo("Spawning flora!!!");
		System.Random random = new(StartOfRound.Instance.randomMapSeed + 2358);

		// Create a dictionary mapping FloraTag to the corresponding moonsWhiteList
		var tagToMoonLists = spawnableFlora
			.GroupBy(flora => flora.floraTag)
			.ToDictionary(
				g => g.Key,
				g => new
				{
					MoonsWhiteList = g.First().moonsWhiteList,
					MoonsBlackList = g.First().moonsBlackList
				}
			);

		// Cache the valid tags based on the current moon configuration
		Dictionary<FloraTag, bool> validTags = new Dictionary<FloraTag, bool>();
		foreach (var tag in tagToMoonLists.Keys) {
			if (tagToMoonLists.TryGetValue(tag, out var moonLists))
			{
				bool isLevelValid = IsCurrentMoonInConfig(moonLists.MoonsWhiteList, moonLists.MoonsBlackList);
				validTags[tag] = isLevelValid;
			}
		}

		foreach (var tagGroup in spawnableFlora.GroupBy(flora => flora.floraTag)) {
			if (!validTags.TryGetValue(tagGroup.Key, out bool isLevelValid) || !isLevelValid) {
				continue;
			}
			foreach (SpawnableFlora flora in tagGroup) {
				var targetSpawns = flora.spawnCurve.Evaluate(random.NextFloat(0, 1));
				for (int i = 0; i < targetSpawns; i++)
				{
					Vector3 basePosition = GetRandomPointNearPointsOfInterest(random);
					Vector3 offset = new Vector3(random.NextFloat(-5, 5), 0, random.NextFloat(-5, 5));
					Vector3 randomPosition = basePosition + offset;
					Vector3 vector = GetRandomNavMeshPosition(randomPosition, 20f, random) + (Vector3.up * 2);

					if (!Physics.Raycast(vector, Vector3.down, out RaycastHit hit, 100, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
						continue;

					bool isValid = true;
					foreach (string floorTag in flora.blacklistedTags)
					{
						if (hit.transform.gameObject.CompareTag(floorTag))
						{
							isValid = false;
							break;
						}
					}
					if (!isValid) continue;

					// Use NavMesh.SamplePosition to get the nearest point on the NavMesh
					if (NavMesh.SamplePosition(hit.point, out NavMeshHit navMeshHit, 20f, NavMesh.AllAreas))
					{
						Vector3 navMeshPosition = navMeshHit.position;

						// Adjust the position to align with the terrain normal
						Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);

						GameObject spawnedFlora = GameObject.Instantiate(flora.prefab, navMeshPosition, rotation, RoundManager.Instance.mapPropsContainer.transform);
						spawnedFlora.transform.up = hit.normal;
					}
				}
			}
		}
	}

	public static Vector3 GetRandomPointNearPointsOfInterest(System.Random random) {
		// Get all points of interest
		Vector3[] pointsOfInterest = RoundManager.Instance.outsideAINodes.Select(node => node.transform.position).ToArray();
		
		// Choose a random point of interest
		Vector3 chosenPoint = pointsOfInterest[random.Next(0, pointsOfInterest.Length)];
		
		// Calculate an offset to avoid too much bunching up
		Vector3 offset = new Vector3(random.NextFloat(-20, 20), 0, random.NextFloat(-20, 20));
		return chosenPoint + offset;
	}

	public static Vector3 GetRandomNavMeshPosition(Vector3 center, float range, System.Random random) {
		for (int i = 0; i < 30; i++) { // Try up to 30 times to find a valid position
			Vector3 randomPos = center + new Vector3(random.NextFloat(-range, range), 0, random.NextFloat(-range, range));
			if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, range, NavMesh.AllAreas)) {
				return hit.position;
			}
		}
		return center; // Fallback to the center if no valid position found
	}

	public static bool IsCurrentMoonInConfig(string[] moonsWhiteList, string[] moonsBlackList) {
		// Prepare the current level name
		string currentLevelName = Regex.Replace(StartOfRound.Instance.currentLevel.PlanetName, "^(?:\\d+ )*(.*)", "$1Level").ToLowerInvariant();
		string currentLLLLevelName = LethalLevelLoader.LevelManager.CurrentExtendedLevel.NumberlessPlanetName.ToLower();
		// Convert whitelist and blacklist to lowercase and sort them
		var whiteList = moonsWhiteList.Select(levelType => levelType.ToLowerInvariant()).ToArray();
		var blackList = moonsBlackList.Select(levelType => levelType.ToLowerInvariant()).ToArray();
		Array.Sort(whiteList);
		Array.Sort(blackList);

		// Function to check if an item exists in the sorted list using binary search
		bool IsInList(string item, string[] list) {
			return Array.BinarySearch(list, item) >= 0;
		}

		// Check if "all" is in the whitelist
		if (IsInList("all", whiteList)) return true;
		
		bool isVanillaMoon = LethalLevelLoader.PatchedContent.VanillaExtendedLevels.Any(level => level.Equals(LethalLevelLoader.LevelManager.CurrentExtendedLevel));

		// Check blacklist first
		if (IsInList(currentLevelName, blackList) || IsInList(currentLLLLevelName, blackList)) return false;

		// Check for vanilla moon conditions
		if (isVanillaMoon) {
			if (IsInList("vanilla", whiteList)) return true;
			if (IsInList(currentLevelName, whiteList)) return true;
			return false;
		}

		// Check for custom moon conditions
		if (IsInList("custom", whiteList)) return true;

		// Check for custom level name
		return IsInList(currentLLLLevelName, whiteList);
	}

	static void SpawnCrates() {
		Plugin.Logger.LogDebug("Spawning crates!!!");
		System.Random random = new();
		int minValue = 0;
		for (int i = 0; i < random.Next(minValue, Mathf.Clamp(Plugin.ModConfig.ConfigCrateAbundance.Value, minValue, 1000)); i++) {
			Vector3 position = RoundManager.Instance.outsideAINodes[random.Next(0, RoundManager.Instance.outsideAINodes.Length)].transform.position;
			Vector3 vector = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(position, 10f, default, random, -1) + (Vector3.up * 2);

			Physics.Raycast(vector, Vector3.down, out RaycastHit hit, 100, StartOfRound.Instance.collidersAndRoomMaskAndDefault);

			GameObject crate = random.NextBool() ? MapObjectHandler.Instance.Crate.MetalCratePrefab : MapObjectHandler.Instance.Crate.ItemCratePrefab;
			
			GameObject spawnedCrate = GameObject.Instantiate(crate, hit.point, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
			Plugin.Logger.LogDebug($"Spawning {crate.name} at {hit.point}");
			spawnedCrate.transform.up = hit.normal;
			spawnedCrate.GetComponent<NetworkObject>().Spawn();
		}
	}
	
	[HarmonyPatch(nameof(RoundManager.UnloadSceneObjectsEarly)), HarmonyPostfix]
	static void PatchFix_DespawnOldCrates() {
		foreach (ItemCrate crate in GameObject.FindObjectsOfType<ItemCrate>()) {
			crate.NetworkObject.Despawn();
		}
	}

	/*[HarmonyPatch("LoadNewLevelWait")]
	[HarmonyPrefix]
	public static void LoadNewLevelWaitPatch(RoundManager __instance)
	{
		if (__instance.currentLevel.levelID == 3 && TimeOfDay.Instance.daysUntilDeadline == 0)
		{
			Plugin.Logger.LogInfo("Spawning Devil deal objects");
			if (RoundManager.Instance.IsServer) CodeRebirthUtils.Instance.SpawnDevilPropsServerRpc();
		}
	}

	[HarmonyPatch("DespawnPropsAtEndOfRound")]
	[HarmonyPostfix]
	public static void DespawnPropsAtEndOfRoundPatch(RoundManager __instance)
	{
		if (__instance.currentLevel.levelID == 3 && TimeOfDay.Instance.daysUntilDeadline == 0)
		{
			Plugin.Logger.LogInfo("Despawning Devil deal objects");
			if (RoundManager.Instance.IsServer) CodeRebirthUtils.Instance.DespawnDevilPropsServerRpc();
		}
	}*/
}