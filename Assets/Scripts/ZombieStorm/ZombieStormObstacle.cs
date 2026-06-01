using UnityEngine;

[DisallowMultipleComponent]
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

    private void Awake()
    {
        circleCollider = GetComponent<CircleCollider2D>();
        if (circleCollider != null)
        {
            circleCollider.isTrigger = true;
        }
    }

    private void OnEnable()
    {
        Register();
    }

    private void Start()
    {
        Register();
    }

    private void OnDisable()
    {
        if (ZombieStormGameController.Instance != null)
        {
            ZombieStormGameController.Instance.UnregisterObstacle(this);
        }
    }

    private void OnValidate()
    {
        radius = Mathf.Max(0.05f, radius);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.15f, 0.95f, 0.35f, 0.75f);
        Gizmos.DrawWireSphere(WorldCenter, WorldRadius);
    }

    private void Register()
    {
        if (ZombieStormGameController.Instance != null)
        {
            ZombieStormGameController.Instance.RegisterObstacle(this);
        }
    }
}
