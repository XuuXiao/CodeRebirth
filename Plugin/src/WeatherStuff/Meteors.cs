using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using CodeRebirth.src;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.PlayerLoop;
using CodeRebirth.Collisions;
using CodeRebirth.Misc;
using UnityEngine.Serialization;
using Random = System.Random;

namespace CodeRebirth.WeatherStuff;
public class Meteors : NetworkBehaviour {
    #pragma warning disable CS0649    
    [Header("Properties")]
    [SerializeField] private float initialSpeed = 50f;
    [SerializeField] private int chanceToSpawnScrap;

    [Header("Audio")]
    [SerializeField]
    AudioSource ImpactAudio;

    [SerializeField]
    AudioSource NormalTravelAudio, InsideTravelAudio;

    [Header("Graphics")]
    [SerializeField]
    ParticleSystem FireTrail;
    [SerializeField]
    AnimationCurve animationCurve = AnimationCurve.Linear(0,0,1,1);

    [SerializeField]
    Renderer MainMeteorRenderer;
    
    Vector3 origin, target;

    float timeInAir, travelTime;
    bool isMoving, visualAndLooping;
    
    public float Progress => timeInAir / travelTime;

    [ClientRpc]
    public void SetupMeteorClientRpc(Vector3 origin, Vector3 target, bool apocalypse) {
        this.origin = origin;
        this.target = target;
        float distance = Vector3.Distance(origin, target);
        travelTime = Mathf.Sqrt(2 * distance / initialSpeed);  // Time to reach the target, adjusted for acceleration
        isMoving = true;

        transform.LookAt(target);
        UpdateAudio(); // Make sure audio works correctly on the first frame.
        FireTrail.Play();
        if (apocalypse) travelTime = 500f;
    }

    public void SetupAsLooping() {
        isMoving = false;
        visualAndLooping = true;
    }
    
    private void Awake() {
        MeteorShower.Instance.AddMeteor(this);
        NormalTravelAudio.Play();
        FireTrail.Stop();
    }

    private void Update() {
        UpdateAudio();
        if (!isMoving) return;

        timeInAir += Time.deltaTime;
        MoveMeteor();
    }

    void MoveMeteor()
    {
        float progress = timeInAir / travelTime;

        if (progress >= 1.0f)
        { // Checks if the progress is 100% or more
            transform.position = target; // Ensures the meteor position is set to the target at impact
            StartCoroutine(Impact()); // Start the impact effects
            return; // Exit to prevent further execution in this update
        }

        Vector3 nextPosition = Vector3.Lerp(origin, target, animationCurve.Evaluate(progress));
        transform.position = nextPosition;
    }

    private void UpdateAudio() {
        if (GameNetworkManager.Instance.localPlayerController.isInsideFactory) {
            NormalTravelAudio.volume = 0;
            InsideTravelAudio.volume = 0;
            ImpactAudio.volume = 0.05f; // make it still audible but not as loud
        } else {
            NormalTravelAudio.volume = 1;
            InsideTravelAudio.volume = 1;
            ImpactAudio.volume = 1;
        }
        if (((1-Progress)*travelTime) <= 4.106f && !InsideTravelAudio.isPlaying) {
            NormalTravelAudio.Stop();
            InsideTravelAudio.Play();
        }
    }

    private IEnumerator Impact() {
        Plugin.Logger.LogInfo("IMPACT!!!");
        isMoving = false;

        ImpactAudio.Play();
            
        if (IsHost && UnityEngine.Random.Range(0, 100) < chanceToSpawnScrap) {
            CodeRebirthUtils.Instance.SpawnScrapServerRpc("Meteorite", transform.position + new Vector3(0, -1f, 0));
        }
            
        GameObject craterInstance = Instantiate(WeatherHandler.Instance.Assets.CraterPrefab, transform.position, Quaternion.identity);
        CraterController craterController = craterInstance.GetComponent<CraterController>();
        if (craterController != null) {
            craterController.ShowCrater(transform.position);
        }
        
        FireTrail.Stop();
            
        Landmine.SpawnExplosion(transform.position, true, 0f, 10f, 25, 75, WeatherHandler.Instance.Assets.ExplosionPrefab);

        yield return new WaitForSeconds(10f); // allow the last particles from the fire trail to still emit. <-- Actually i think the meteor just looks cool staying on the ground for an extra 10 seconds.
        if(IsHost)
            Destroy(gameObject);
        MeteorShower.Instance.RemoveMeteor(this);
    }
}
public class CraterController : MonoBehaviour
{
    public GameObject craterMesh;
    private ColliderIdentifier fireCollider;

    private void Awake()
    {
        fireCollider = this.transform.Find("WildFire").GetComponent<ColliderIdentifier>();
        ToggleCrater(false);
        MeteorShower.Instance.AddCrater(this);
    }

    static readonly Color GRASS_IMPACT_COLOUR = new Color(0.043f, 0.141f, 0.043f);
    static readonly Color SNOW_IMPACT_COLOUR = new Color(0.925f, 0.929f, 1f);
    static readonly Color ROCK_IMPACT_COLOUR = Color.gray;
    static readonly Color SAND_IMPACT_COLOUR = new Color(0.761f, 0.576f, 0f);
    public void ShowCrater(Vector3 impactLocation)
    {
        transform.position = impactLocation; // Position the crater at the impact location
        
        // Perform a raycast downward from a position slightly above the impact location
        RaycastHit hit;
        float raycastDistance = 50f; // Max distance the raycast will check for terrain
        Vector3 raycastOrigin = impactLocation; // Start the raycast 5 units above the impact location

        ToggleCrater(true);

        // Ensure the crater has a unique material instance to modify
        Renderer craterRenderer = craterMesh.GetComponent<Renderer>();
        if (craterRenderer == null)
        {
            Debug.LogWarning("Renderer component not found on the crater object.");
            return;
        }

        craterRenderer.material = new Material(craterRenderer.material); // Create a new instance of the material
        craterRenderer.material.color = Color.grey;
        // Cast the ray to detect terrain
        if (!Physics.Raycast(raycastOrigin, Vector3.down, out hit, raycastDistance, LayerMask.GetMask("Room")))
        {
            Debug.LogWarning("Terrain not found below the impact point.");
            return;
        }
        // Check if the object hit is tagged as "Terrain"
        switch (hit.collider.gameObject.tag)
        {   
            case "Grass":
                {
                    craterRenderer.material.color = GRASS_IMPACT_COLOUR;
                    Plugin.Logger.LogInfo("Found Grass!");
                    break;
                }
            case "Snow":
                {
                    craterRenderer.material.color = SNOW_IMPACT_COLOUR;
                    Plugin.Logger.LogInfo("Found Snow!");
                    break;
                }
            case "Rock":
                {
                    craterRenderer.material.color = ROCK_IMPACT_COLOUR;
                    Plugin.Logger.LogInfo("Found Rock!");
                    break;
                }
            case "Gravel":
                {
                    craterRenderer.material.color = SAND_IMPACT_COLOUR;
                    Plugin.Logger.LogInfo("Found Sand!");
                    break;
                }
            default: Debug.LogWarning("The hit object is not tagged as 'Terrain'."); break;
        }
    }

    void ToggleCrater(bool enable)
    {
        craterMesh.SetActive(enable);
        fireCollider.enabled = enable;
    }
    public void HideCrater()
    {
        ToggleCrater(false);
    }
}