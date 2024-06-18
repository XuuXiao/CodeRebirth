﻿﻿using System;
using System.Collections;
using System.Linq;
using CodeRebirth.ItemStuff;
using CodeRebirth.Misc;
using CodeRebirth.Util.Extensions;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using Random = System.Random;

namespace CodeRebirth.MapStuff;

public class ItemCrate : CRHittable {

	[SerializeField]
	public SkinnedMeshRenderer mainRenderer;

	[SerializeField]
	[Header("Hover Tooltips")]
	public string regularHoverTip = "Hold : [E]";
	[SerializeField]
	public string keyHoverTip = "Open : [LMB]";

	[Header("Audio")]
	[SerializeField]
	public AudioSource slowlyOpeningSFX;

	[SerializeField]
	public AudioSource openSFX;
	
	public InteractTrigger trigger;
	public Collider[] colliders;
	
	public Pickable pickable;

	Animator animator;
	public AnimationClip openClip;

	public bool opened;
	public float digProgress;

	public Vector3 originalPosition;
	public Random random = new();
	public enum CrateType {
		Wooden,
		Metal,
		Golden,
	}
	public CrateType crateType;
	
	public void Awake() {
		if (crateType == CrateType.Wooden) {
			// mainRenderer.GetComponent<SkinnedMeshRenderer>().materials[0] = Assets.WoodenCrateMaterial;
			// mainRenderer.GetComponent<SkinnedMeshRenderer>().Mesh = Assets.WoodenCrateMesh;
		} else if (crateType == CrateType.Metal) {
			// mainRenderer.GetComponent<SkinnedMeshRenderer>().materials[0] = Assets.MetalCrateMaterial;
			// mainRenderer.GetComponent<SkinnedMeshRenderer>().Mesh = Assets.MetalCrateMesh;
		}
		trigger = GetComponent<InteractTrigger>();
		pickable = GetComponent<Pickable>();
		animator = GetComponent<Animator>();
		trigger.onInteractEarly.AddListener(OnInteractEarly);
		trigger.onInteract.AddListener(OnInteract);
		trigger.onStopInteract.AddListener(OnInteractCancel);

		digProgress = random.NextFloat(0.01f, 0.3f);
		originalPosition = transform.position;
		transform.position = originalPosition + (transform.up * digProgress * .5f);
	}

	public void Update() {
		trigger.interactable = digProgress == 1;
		if (GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer != null && GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer.itemProperties.itemName == "Key") {
			trigger.hoverTip = keyHoverTip;
		} else {
			trigger.hoverTip = regularHoverTip;
		}
	}

	public void OnInteractEarly(PlayerControllerB playerController) {
		slowlyOpeningSFX.Play();
	}
	
	public void OnInteract(PlayerControllerB playerController) {
		if (GameNetworkManager.Instance.localPlayerController != playerController) return;
		Open();
		slowlyOpeningSFX.Stop();
	}

	public void OnInteractCancel(PlayerControllerB playerController) {
		slowlyOpeningSFX.Stop();
	}

	public void Open() {
		if (!IsHost) OpenCrateServerRPC();
		else OpenCrate();
	}

	[ServerRpc(RequireOwnership = false)]
	public void OpenCrateServerRPC() {
		OpenCrate();
	}

	public void OpenCrate() {
		SpawnableItemWithRarity chosenItemWithRarity = random.NextItem(RoundManager.Instance.currentLevel.spawnableScrap);
		Item item = chosenItemWithRarity.spawnableItem;
		GameObject spawned = Instantiate(item.spawnPrefab, transform.position + transform.up*0.6f, Quaternion.Euler(item.restingRotation), RoundManager.Instance.spawnedScrapContainer);

		spawned.GetComponent<GrabbableObject>().SetScrapValue((int)(random.Next(item.minValue, item.maxValue) * RoundManager.Instance.scrapValueMultiplier));
		spawned.GetComponent<NetworkObject>().Spawn(false);

		OpenCrateClientRPC();
	}

	[ClientRpc]
	public void OpenCrateClientRPC() {
		OpenCrateLocally();
	}

	public void OpenCrateLocally() {
		pickable.IsLocked = false;
		openSFX.Play();
		trigger.enabled = false;
		GetComponent<Collider>().enabled = false;
		opened = true;
		animator.SetTrigger("opened");
	}
	
	public override bool Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1) {
		if (digProgress < 1) {
			float progressChange = random.NextFloat(0.15f, 0.25f);
			digProgress = Mathf.Min(digProgress + progressChange, 1);
			Plugin.Logger.LogDebug($"ItemCrate was hit! New digProgress: {digProgress}");
			transform.position = originalPosition + (transform.up * digProgress * .5f);
		}
		return true; // this bool literally doesn't get used. i have no clue.
	}
}