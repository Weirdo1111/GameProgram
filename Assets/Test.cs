using UnityEngine;

public sealed class Test : MonoBehaviour
{
    [Header("Zombie Storm MVP")]
    [SerializeField] private float runDurationSeconds = 300f;

    private void Awake()
    {
        if (FindObjectOfType<ZombieStormGameController>() != null)
        {
            return;
        }

        name = "Zombie Storm Bootstrap";
        Application.targetFrameRate = 120;
        Time.fixedDeltaTime = 1f / 60f;
        Physics2D.gravity = Vector2.zero;

        ZombieStormGameController controller = gameObject.AddComponent<ZombieStormGameController>();
        controller.runDurationSeconds = Mathf.Max(60f, runDurationSeconds);
    }
}
