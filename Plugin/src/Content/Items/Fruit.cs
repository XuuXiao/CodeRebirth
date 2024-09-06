using CodeRebirth.src.Util.Extensions;
using Unity.Netcode;
using static CodeRebirth.src.Content.Unlockables.PlantPot;

namespace CodeRebirth.src.Content.Items;
public class Fruit : GrabbableObject {
    public FruitType fruitType = FruitType.None;
    public override void Start() {
        base.Start();

        if (!IsHost) return;
        NetworkObject.OnSpawn(() => {
            int value = (int)(UnityEngine.Random.Range(this.itemProperties.minValue, this.itemProperties.maxValue) * 0.4f);
            SetScrapValue(value);
            SetFruitValueClientRPC(value);
        });
    }

    [ClientRpc]
    void SetFruitValueClientRPC(int value) {
        if (IsHost) return;
        SetScrapValue(value);
    }
}