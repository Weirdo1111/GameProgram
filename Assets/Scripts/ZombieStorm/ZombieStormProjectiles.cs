using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class ZombieStormProjectile : MonoBehaviour
{
    private ZombieStormGameController game;
    private Vector2 direction;
    private float damage;
    private float speed;
    private float life;
    private int pierce;

    public void Initialize(ZombieStormGameController owner, Vector2 fireDirection, float hitDamage, float moveSpeed, float seconds, int pierceCount)
    {
        game = owner;
        direction = fireDirection.sqrMagnitude > 0.01f ? fireDirection.normalized : Vector2.up;
        damage = hitDamage;
        speed = moveSpeed;
        life = seconds;
        pierce = pierceCount;
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        life -= Time.deltaTime;
        if (life <= 0f)
        {
            game.ReturnPooled("player_bullet", gameObject);
            return;
        }

        IReadOnlyList<ZombieStormEnemy> activeEnemies = game.Enemies;
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            ZombieStormEnemy enemy = activeEnemies[i];
            if (enemy != null && !enemy.IsDead && Vector2.Distance(transform.position, enemy.transform.position) <= enemy.Radius + 0.16f)
            {
                enemy.TakeDamage(damage, direction);
                game.SpawnHitSpark(transform.position, new Color(1f, 0.9f, 0.28f, 0.9f), 0.26f);
                pierce--;
                if (pierce < 0)
                {
                    game.ReturnPooled("player_bullet", gameObject);
                }

                return;
            }
        }
    }
}

public sealed class ZombieStormEnemyProjectile : MonoBehaviour
{
    private ZombieStormGameController game;
    private Vector2 direction;
    private float damage;
    private float speed;
    private float life;

    public void Initialize(ZombieStormGameController owner, Vector2 fireDirection, float hitDamage, float moveSpeed, float seconds)
    {
        game = owner;
        direction = fireDirection.sqrMagnitude > 0.01f ? fireDirection.normalized : Vector2.up;
        damage = hitDamage;
        speed = moveSpeed;
        life = seconds;
    }

    private void Update()
    {
        Vector2 startPosition = transform.position;
        Vector2 endPosition = startPosition + direction * speed * Time.deltaTime;
        transform.position = endPosition;
        life -= Time.deltaTime;
        if (life <= 0f)
        {
            game.ReturnPooled("enemy_spit", gameObject);
            return;
        }

        if (game.Player != null && DistanceToSegment(game.Player.transform.position, startPosition, endPosition) <= 0.62f)
        {
            game.SpawnHitSpark(game.Player.transform.position, new Color(0.48f, 1f, 0.3f, 0.92f), 0.44f);
            game.Player.TakeDamage(damage);
            game.ReturnPooled("enemy_spit", gameObject);
        }
    }

    private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;
        if (lengthSquared <= 0.0001f)
        {
            return Vector2.Distance(point, start);
        }

        float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
        return Vector2.Distance(point, start + segment * t);
    }
}
