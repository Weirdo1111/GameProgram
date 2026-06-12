using UnityEngine;

[DisallowMultipleComponent]
// Represents one circular map obstacle. The game controller queries registered obstacles to keep
// players and enemies from walking through graves, props, and other arena blockers.
public sealed class ZombieStormObstacle : MonoBehaviour
{
    [Min(0.05f)]
    public float radius = 1f;
    public float extraPadding = 0.05f;
    public Vector2 centerOffset;

    private CircleCollider2D circleCollider;

    public Vector2 WorldCenter
    {
        get
        {
            Vector2 localCenter = centerOffset;
            if (circleCollider != null)
            {
                localCenter += circleCollider.offset;
            }

            return transform.TransformPoint(localCenter);
        }
    }

    public float WorldRadius
    {
        get
        {
            float baseRadius = circleCollider != null ? circleCollider.radius : radius;
            Vector3 scale = transform.lossyScale;
            return Mathf.Max(0.05f, baseRadius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y)) + extraPadding);
        }
    }

    // Caches the optional CircleCollider2D and makes it a trigger because collision resolution is
    // handled manually by ZombieStormGameController.
    private void Awake()
    {
        circleCollider = GetComponent<CircleCollider2D>();
        if (circleCollider != null)
        {
            circleCollider.isTrigger = true;
        }
    }

    // Registers the obstacle when Unity enables it so newly activated map props block movement.
    private void OnEnable()
    {
        Register();
    }

    // Registers again on Start to cover creation order where the controller was not ready during OnEnable.
    private void Start()
    {
        Register();
    }

    // Removes the obstacle from the controller so disabled or destroyed props are ignored.
    private void OnDisable()
    {
        if (ZombieStormGameController.Instance != null)
        {
            ZombieStormGameController.Instance.UnregisterObstacle(this);
        }
    }

    // Keeps the editable fallback radius positive when values change in the Inspector.
    private void OnValidate()
    {
        radius = Mathf.Max(0.05f, radius);
    }

    // Shows the effective world-space blocking radius in the Scene view for tuning.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.15f, 0.95f, 0.35f, 0.75f);
        Gizmos.DrawWireSphere(WorldCenter, WorldRadius);
    }

    // Adds this obstacle to the active controller if one exists.
    private void Register()
    {
        if (ZombieStormGameController.Instance != null)
        {
            ZombieStormGameController.Instance.RegisterObstacle(this);
        }
    }
}
