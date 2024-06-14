using CodeRebirth.Misc;
using UnityEngine;

namespace CodeRebirth.ScrapStuff;
public class MeteoriteShard : GrabbableObject {
    public override void Start() {
        base.Start();
        GetComponent<ScrapValueSyncer>().SetScrapValue(scrapValue);
    }
}