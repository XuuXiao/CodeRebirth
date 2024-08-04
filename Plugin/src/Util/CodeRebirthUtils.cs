using System;
using CodeRebirth.MapStuff;
using CodeRebirth.EnemyStuff;
using Unity.Netcode;
using UnityEngine;
using Random = System.Random;
using System.Collections.Generic;

namespace CodeRebirth.Util.Spawning;
internal class CodeRebirthUtils : NetworkBehaviour
{
    private static Random random = null!;
    internal static CodeRebirthUtils Instance { get; private set; } = null!;
    public static List<GrabbableObject> goldenEggs = new List<GrabbableObject>();
    public static Dictionary<string, GameObject> Objects = new Dictionary<string, GameObject>();

    void Awake()
    {
        Instance = this;
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void SpawnScrapServerRpc(string itemName, Vector3 position, bool isQuest = false, bool defaultRotation = true, int valueIncrease = 0) {
        if (StartOfRound.Instance == null) {
            return;
        }
        
        if (random == null) {
            random = new Random(StartOfRound.Instance.randomMapSeed + 85);
        }

        if (itemName == string.Empty) {
            return;
        }
        Plugin.samplePrefabs.TryGetValue(itemName, out Item item);
        if (item == null) {
            // throw for stacktrace
            throw new NullReferenceException($"'{itemName}' either isn't a CodeRebirth scrap or not registered! This method only handles CodeRebirth scrap!");
        }
        Transform? parent = null;
        if (parent == null) {
            parent = StartOfRound.Instance.propsContainer;
        }
        GameObject go = Instantiate(item.spawnPrefab, position + Vector3.up * 0.2f, defaultRotation == true ? Quaternion.Euler(item.restingRotation) : Quaternion.identity, parent);
        go.GetComponent<NetworkObject>().Spawn();
        int value = random.Next(minValue: item.minValue + valueIncrease, maxValue: item.maxValue + valueIncrease);
        var scanNode = go.GetComponentInChildren<ScanNodeProperties>();
        scanNode.scrapValue = value;
        scanNode.subText = $"Value: ${value}";
        go.GetComponent<GrabbableObject>().scrapValue = value;
        UpdateScanNodeClientRpc(new NetworkObjectReference(go), value);
        if (isQuest) go.AddComponent<QuestItem>();
    }

    [ClientRpc]
    public void UpdateScanNodeClientRpc(NetworkObjectReference go, int value) {
        go.TryGet(out NetworkObject netObj);
        if(netObj != null)
        {
            if (netObj.TryGetComponent(out GrabbableObject grabbableObject)) {
                grabbableObject.SetScrapValue(value);
                Plugin.Logger.LogInfo($"Scrap Value: {value}");
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnDevilPropsServerRpc() {
        Objects.Add("Devil", Spawn("Devil", new Vector3(-19.355f, -1.473f, -0.243f), Quaternion.Euler(-81.746f, 152.088f, -53.711f)));
        Objects.Add("DevilChair", Spawn("DevilChair", new Vector3(-21.16f, -2.686f, 0), Quaternion.Euler(0, 180, 0)));
        Objects.Add("DevilTable", Spawn("DevilTable", new Vector3(-19.518f, -2.686f, 0), Quaternion.identity));
        Objects.Add("PlayerChair", Spawn("PlayerChair", new Vector3(-17.832f, -2.686f, 0), Quaternion.Euler(-90, -90, 0)));
    }

    [ServerRpc(RequireOwnership = false)]
    public void DespawnDevilPropsServerRpc() {
        foreach (KeyValuePair<string, GameObject> @object in Objects)
        {
            @object.Value.GetComponent<NetworkObject>().Despawn(true);
        }
        Objects.Clear();
    }
    
    public static GameObject Spawn(string objectName, Vector3 location, Quaternion rotation)
    {
        GameObject obj = Instantiate<GameObject>(MapObjectHandler.DevilDealPrefabs[objectName], location, rotation);
        NetworkObject component = obj.GetComponent<NetworkObject>();
        Plugin.Logger.LogInfo(obj.name + " NetworkObject spawned");
        component.Spawn(false);
        return obj;
    }
}