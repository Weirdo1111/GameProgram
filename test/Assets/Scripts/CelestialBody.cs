using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class CelestialBody : MonoBehaviour
{
    private const string EmissionColorProperty = "_EmissionColor";

    private Renderer cachedRenderer;
    private Vector3 baseScale;
    private Color baseEmission;

    public string BodyName { get; private set; }
    public string FactText { get; private set; }
    public bool IsMoon { get; private set; }
    public Color AccentColor { get; private set; }
    public float FocusDistance { get; private set; }

    public void Initialize(string bodyName, string factText, Color accentColor, bool isMoon)
    {
        BodyName = bodyName;
        FactText = factText;
        AccentColor = accentColor;
        IsMoon = isMoon;

        cachedRenderer = GetComponent<Renderer>();
        baseScale = transform.localScale;
        FocusDistance = Mathf.Clamp(baseScale.x * (isMoon ? 7.5f : 5.5f), 4.5f, 12f);

        if (cachedRenderer.material.HasProperty(EmissionColorProperty))
        {
            baseEmission = cachedRenderer.material.GetColor(EmissionColorProperty);
        }
        else
        {
            baseEmission = accentColor * 0.35f;
        }
    }

    public Vector3 GetSuggestedCameraPosition()
    {
        Vector3 awayFromCenter = transform.position - Vector3.zero;

        if (awayFromCenter.sqrMagnitude < 0.01f)
        {
            awayFromCenter = new Vector3(-1f, 0.35f, -1f);
        }

        awayFromCenter.Normalize();
        return transform.position + awayFromCenter * FocusDistance + Vector3.up * (FocusDistance * 0.22f);
    }

    public void PlaySelectionEffect()
    {
        StopAllCoroutines();
        StartCoroutine(AnimateSelection());
    }

    private IEnumerator AnimateSelection()
    {
        const float duration = 0.35f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float pulse = Mathf.Sin((elapsed / duration) * Mathf.PI);
            transform.localScale = baseScale * (1f + pulse * 0.18f);

            if (cachedRenderer.material.HasProperty(EmissionColorProperty))
            {
                cachedRenderer.material.SetColor(
                    EmissionColorProperty,
                    Color.Lerp(baseEmission, AccentColor * 2.5f, pulse));
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = baseScale;

        if (cachedRenderer.material.HasProperty(EmissionColorProperty))
        {
            cachedRenderer.material.SetColor(EmissionColorProperty, baseEmission);
        }
    }
}
