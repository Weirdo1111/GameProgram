using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Moves XP or health pickups toward the player and applies the reward.
public sealed class ZombieStormPickup : MonoBehaviour
{
    private ZombieStormGameController game;
    private string poolKey;
    private int xp;
    private float heal;
    private float baseScale;
    private float bobOffset;

    // Initializes the references and values this object needs at runtime.
    public void Initialize(ZombieStormGameController owner, string key, int xpAmount, float healAmount = 0f)
    {
        game = owner;
        poolKey = key;
        xp = xpAmount;
        heal = healAmount;
        baseScale = heal > 0f ? 0.42f : 0.32f;
        bobOffset = UnityEngine.Random.value * 10f;
    }

    // Advances movement, combat, animation, timers, and state changes each frame.
    private void Update()
    {
        if (game == null || game.Player == null)
        {
            return;
        }

        Vector2 toPlayer = (Vector2)game.Player.transform.position - (Vector2)transform.position;
        float distance = toPlayer.magnitude;
        float pickupRange = game.Player.PickupRange;
        if (distance < pickupRange)
        {
            float pullSpeed = Mathf.Lerp(2f, 12f, 1f - distance / pickupRange);
            transform.position += (Vector3)(toPlayer.normalized * pullSpeed * Time.deltaTime);
        }

        transform.localScale = Vector3.one * (baseScale + Mathf.Sin(Time.time * 6f + bobOffset) * 0.035f);

        if (distance < 0.34f)
        {
            if (xp > 0)
            {
                game.Player.AddExperience(xp);
            }

            if (heal > 0f)
            {
                game.Player.Heal(heal);
                game.SpawnHitSpark(transform.position, new Color(1f, 0.22f, 0.28f, 0.78f), 0.26f);
                game.PlaySfx("pickup", 0.6f, 0.04f);
            }

            game.ReturnPooled(poolKey, gameObject);
        }
    }
}
