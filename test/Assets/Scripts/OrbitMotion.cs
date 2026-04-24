using UnityEngine;

public class OrbitMotion : MonoBehaviour
{
    [SerializeField] private Vector3 orbitAxis = Vector3.up;
    [SerializeField] private float orbitSpeed = 12f;

    public void Initialize(Vector3 axis, float speed)
    {
        orbitAxis = axis.sqrMagnitude > 0.001f ? axis.normalized : Vector3.up;
        orbitSpeed = speed;
    }

    private void Update()
    {
        transform.Rotate(orbitAxis, orbitSpeed * Time.deltaTime, Space.Self);
    }
}
