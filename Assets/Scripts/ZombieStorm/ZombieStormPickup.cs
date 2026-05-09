using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class ZombieStormPickup : MonoBehaviour
{
    private ZombieStormGameController game;
    private string poolKey;
    private int xp;
    private int coins;
    private float bobOffset;

    public void Initialize(ZombieStormGameController owner, string key, int xpAmount, int coinAmount)
    {
        game = owner;
        poolKey = key;
        xp = xpAmount;
        coins = coinAmount;
        bobOffset = UnityEngine.Random.value * 10f;
    }

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

        transform.localScale = Vector3.one * (0.32f + Mathf.Sin(Time.time * 6f + bobOffset) * 0.035f);

        if (distance < 0.34f)
        {
            if (xp > 0)
            {
                game.Player.AddExperience(xp);
            }

            if (coins > 0)
            {
                game.Player.AddCoins(coins);
            }

            game.ReturnPooled(poolKey, gameObject);
        }
    }
}
