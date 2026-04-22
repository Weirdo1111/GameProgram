using UnityEngine;

public sealed class Test : MonoBehaviour
{
    [Header("RuleShot 2D Vertical Slice")]
    [SerializeField] private int startZone = 1;

    private void Awake()
    {
        if (FindObjectOfType<RuleShotGameController>() != null)
        {
            return;
        }

        name = "RuleShot Bootstrap";
        Application.targetFrameRate = 120;
        Time.fixedDeltaTime = 1f / 60f;
        Physics2D.gravity = new Vector2(0f, -24f);

        RuleShotGameController controller = gameObject.AddComponent<RuleShotGameController>();
        controller.firstZone = Mathf.Clamp(startZone - 1, 0, 4);
    }
}
