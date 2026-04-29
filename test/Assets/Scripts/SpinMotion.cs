using UnityEngine;

public class SpinMotion : MonoBehaviour
{
    [SerializeField] private Vector3 rotationAxis = Vector3.up;
    [SerializeField] private float rotationSpeed = 24f;

    public void Initialize(Vector3 axis, float speed)
    {
        rotationAxis = axis.sqrMagnitude > 0.001f ? axis.normalized : Vector3.up;
        rotationSpeed = speed;
    }

    private void Update()
    {
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);
    }
}
