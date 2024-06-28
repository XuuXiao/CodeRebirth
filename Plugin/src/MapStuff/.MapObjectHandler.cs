﻿using System;
using CodeRebirth.Misc;
using CodeRebirth.ScrapStuff;
using CodeRebirth.Util;
using CodeRebirth.Util.AssetLoading;
using LethalLib.Extras;
using LethalLib.Modules;
using Unity.Mathematics;
using UnityEngine;

namespace CodeRebirth.MapStuff;

public class MapObjectHandler : ContentHandler<MapObjectHandler> {
	public class MoneyAssets(string bundleName) : AssetBundleLoader<MoneyAssets>(bundleName) {
		[LoadFromBundle("MoneyObj.asset")]
		public Item MoneyItem { get; private set; }
	}

	public class CrateAssets(string bundleName) : AssetBundleLoader<CrateAssets>(bundleName) {
		[LoadFromBundle("Crate")]
		public GameObject ItemCratePrefab { get; private set; }
	}

	public MoneyAssets Money { get; private set; }
	public CrateAssets Crate { get; private set; }

	public MapObjectHandler() {
		Money = new MoneyAssets("coderebirthasset");
		Crate = new CrateAssets("coderebirthasset");

		if (Plugin.ModConfig.ConfigMoneyEnabled.Value) RegisterInsideMoney();
	}

	public void RegisterInsideMoney() {
		Money.MoneyItem.spawnPrefab.GetComponent<Money>().SetScrapValue(-1);
		SpawnableMapObjectDef mapObjDefBug = ScriptableObject.CreateInstance<SpawnableMapObjectDef>();
		mapObjDefBug.spawnableMapObject = new SpawnableMapObject();
		mapObjDefBug.spawnableMapObject.prefabToSpawn = Money.MoneyItem.spawnPrefab;
		if (Plugin.ModConfig.ConfigWalletEnabled.Value) {
			MapObjects.RegisterMapObject(mapObjDefBug, Levels.LevelTypes.All, (level) => 
				new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, Mathf.Clamp(Plugin.ModConfig.ConfigMoneyAbundance.Value, 0, 1000)))
			);
		}
	}
}