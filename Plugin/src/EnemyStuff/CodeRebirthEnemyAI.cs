﻿using Unity.Netcode;
using System.Diagnostics;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.src.EnemyStuff;
public abstract class CodeRebirthEnemyAI : EnemyAI
{
    public override void Start()
    {
        base.Start();
        LogIfDebugBuild(enemyType.enemyName + " Spawned.");
    }
    
    [Conditional("DEBUG")]
    public void LogIfDebugBuild(object text)
    {
        Plugin.Logger.LogInfo(text);
    }

    [ClientRpc]
    public void DoAnimationClientRpc(string triggerName)
    {
        DoAnimationOnLocalClient(triggerName);
    }

    public void DoAnimationOnLocalClient(string triggerName)
    {
        LogIfDebugBuild(triggerName);
        creatureAnimator.SetTrigger(triggerName);
    }

    public void ToggleEnemySounds(bool toggle)
    {
        creatureSFX.enabled = toggle;
        creatureVoice.enabled = toggle;
    }
    [ClientRpc]
    public void ChangeSpeedClientRpc(float speed)
    {
        ChangeSpeedOnLocalClient(speed);
    }

    public void ChangeSpeedOnLocalClient(float speed)
    {
        agent.speed = speed;
    }
    public bool FindClosestPlayerInRange(float range) {
        PlayerControllerB closestPlayer = null;
        float minDistance = float.MaxValue;

        foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts) {
            bool onSight = player.IsSpawned && player.isPlayerControlled && !player.isPlayerDead && !player.isInHangarShipRoom && EnemyHasLineOfSightToPosition(player.transform.position, 60f, range);
            if (!onSight) continue;

            float distance = Vector3.Distance(transform.position, player.transform.position);
            bool closer = distance < minDistance;
            if (!closer) continue;

            minDistance = distance;
            closestPlayer = player;
        }
        if (closestPlayer == null) return false;

        targetPlayer = closestPlayer;
        return true;
    }

    public bool EnemyHasLineOfSightToPosition(Vector3 pos, float width = 60f, float range = 20f, float proximityAwareness = 5f) {
        if (eye == null) {
            _ = transform;
        } else {
            _ = eye;
        }

        if (Vector3.Distance(eye.position, pos) >= range || Physics.Linecast(eye.position, pos, StartOfRound.Instance.collidersAndRoomMaskAndDefault)) return false;

        Vector3 to = pos - eye.position;
        return Vector3.Angle(eye.forward, to) < width || Vector3.Distance(transform.position, pos) < proximityAwareness;
    }
    public bool IsPlayerReachable(PlayerControllerB PlayerToCheck) {
        Vector3 Position = RoundManager.Instance.GetNavMeshPosition(targetPlayer.transform.position, RoundManager.Instance.navHit, 2.7f);
        if (!RoundManager.Instance.GotNavMeshPositionResult) {
            LogIfDebugBuild("Player Reach Test: No Navmesh position");
            return false; 
        }
        agent.CalculatePath(Position, agent.path);
        bool HasPath = (agent.path.status == NavMeshPathStatus.PathComplete);
        LogIfDebugBuild($"Player Reach Test: {HasPath}");
        return HasPath;
    }
    public float PlayerDistanceFromShip(PlayerControllerB PlayerToCheck) {
        if(PlayerToCheck == null) return -1;
        float DistanceFromShip = Vector3.Distance(targetPlayer.transform.position, StartOfRound.Instance.shipBounds.transform.position);
        LogIfDebugBuild($"PlayerNearShip check: {DistanceFromShip}");
        return DistanceFromShip;
    }
    private float DistanceFromTargetPlayer(PlayerControllerB targetPlayer, bool IncludeYAxis) {
        if (targetPlayer == null) return -1f;
        if (IncludeYAxis) {
            return Vector3.Distance(targetPlayer.transform.position, this.transform.position);
        }
        Vector2 PlayerFlatLocation = new Vector2(targetPlayer.transform.position.x, targetPlayer.transform.position.z);
        Vector2 EnemyFlatLocation = new Vector2(transform.position.x, transform.position.z);
        return Vector2.Distance(PlayerFlatLocation, EnemyFlatLocation);
    }
    public bool AnimationIsFinished(string AnimName) {
        if (!creatureAnimator.GetCurrentAnimatorStateInfo(0).IsName(AnimName)) {
            LogIfDebugBuild(__getTypeName() + ": Checking for animation " + AnimName + ", but current animation is " + creatureAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name);
            return true;
        }
        return (creatureAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f);
    }
    [ServerRpc]
    public void SetTargetServerRpc(int PlayerID) {
        SetTargetClientRpc(PlayerID);
    }
    [ClientRpc]
    public void SetTargetClientRpc(int PlayerID) {
        if(PlayerID == -1) {
            targetPlayer = null;
            LogIfDebugBuild($"Clearing target on {this}");
            return;
        }
        if (StartOfRound.Instance.allPlayerScripts[PlayerID] == null) {
            LogIfDebugBuild($"Index invalid! {this}");
            return;
        }
        targetPlayer = StartOfRound.Instance.allPlayerScripts[PlayerID];
        LogIfDebugBuild($"{this} setting target to: {targetPlayer.playerUsername}");
    }
}
