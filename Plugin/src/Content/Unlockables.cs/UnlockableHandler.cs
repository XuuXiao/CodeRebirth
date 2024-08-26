using CodeRebirth.src.Util.AssetLoading;
using CodeRebirth.src.Util;
using UnityEngine;
using LethalLib.Modules;

namespace CodeRebirth.src.Content.Maps;
public class UnlockableHandler : ContentHandler<UnlockableHandler> {
	public class ShockwaveBotAssets(string bundleName) : AssetBundleLoader<ShockwaveBotAssets>(bundleName) {
		[LoadFromBundle("ShockwaveBotUnlockable.asset")]
		public UnlockableItem ShockWaveBotUnlockable { get; private set; } = null!;
	}

	public ShockwaveBotAssets ShockwaveBot { get; private set; } = null!;

    public UnlockableHandler() {
		if (true) RegisterShockWaveGal();
	}

    private void RegisterShockWaveGal() {
        ShockwaveBot = new ShockwaveBotAssets("ShockwaveBot");
        Unlockables.RegisterUnlockable(ShockwaveBot.ShockWaveBotUnlockable, 999, StoreType.ShipUpgrade);
    }
}