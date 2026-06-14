using UnityEngine;

// Programmatically generated fallback art used when optional image assets are unavailable.
public sealed partial class ZombieStormGameController
{
    // Builds a fallback neon floor when map art is unavailable.
    private void BuildFallbackNeonFloor()
    {
        GameObject floor = CreateSpriteObject("Neon Asphalt", tileSprite, new Color(0.06f, 0.072f, 0.075f), Vector3.forward * 4f, new Vector3(110f, 110f, 1f), -8);
        floor.transform.SetParent(worldRoot, false);

        for (int i = -14; i <= 14; i++)
        {
            GameObject lineX = CreateSpriteObject("Road Line X", tileSprite, new Color(0.05f, 0.75f, 1f, 0.16f), new Vector3(i * 4f, 0f, 2f), new Vector3(0.06f, 110f, 1f), -6);
            lineX.transform.SetParent(worldRoot, false);
            GameObject lineY = CreateSpriteObject("Road Line Y", tileSprite, new Color(1f, 0.18f, 0.45f, 0.12f), new Vector3(0f, i * 4f, 2f), new Vector3(110f, 0.06f, 1f), -6);
            lineY.transform.SetParent(worldRoot, false);
        }
    }

    // Programmatically draws a fallback survivor sprite.
    private Sprite CreateSurvivorSprite()
    {
        const int width = 32;
        const int height = 32;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        ClearTexture(texture, Color.clear);

        Color outline = new Color(0.08f, 0.055f, 0.035f, 1f);
        Color hatDark = new Color(0.28f, 0.20f, 0.10f, 1f);
        Color hat = new Color(0.58f, 0.43f, 0.20f, 1f);
        Color hatLight = new Color(0.86f, 0.68f, 0.33f, 1f);
        Color skin = new Color(0.92f, 0.68f, 0.45f, 1f);
        Color skinShadow = new Color(0.62f, 0.36f, 0.22f, 1f);
        Color hair = new Color(0.18f, 0.11f, 0.07f, 1f);
        Color shirt = new Color(0.86f, 0.88f, 0.80f, 1f);
        Color vest = new Color(0.50f, 0.34f, 0.16f, 1f);
        Color scarf = new Color(0.72f, 0.08f, 0.06f, 1f);
        Color denim = new Color(0.12f, 0.22f, 0.34f, 1f);
        Color boot = new Color(0.20f, 0.12f, 0.07f, 1f);
        Color glove = new Color(0.22f, 0.14f, 0.08f, 1f);
        Color eye = new Color(0.08f, 0.18f, 0.32f, 1f);

        FillEllipse(texture, 16, 28, 8, 2, new Color(0f, 0f, 0f, 0.32f));

        FillRect(texture, 10, 20, 4, 7, outline);
        FillRect(texture, 18, 20, 4, 7, outline);
        FillRect(texture, 11, 20, 3, 6, denim);
        FillRect(texture, 18, 20, 3, 6, denim);
        FillRect(texture, 9, 26, 6, 3, outline);
        FillRect(texture, 17, 26, 6, 3, outline);
        FillRect(texture, 10, 26, 5, 2, boot);
        FillRect(texture, 17, 26, 5, 2, boot);

        FillRect(texture, 8, 13, 16, 10, outline);
        FillRect(texture, 9, 14, 14, 8, shirt);
        FillRect(texture, 10, 14, 4, 8, vest);
        FillRect(texture, 18, 14, 4, 8, vest);
        FillRect(texture, 14, 14, 4, 5, new Color(0.96f, 0.95f, 0.84f, 1f));
        FillRect(texture, 14, 15, 4, 3, scarf);
        FillRect(texture, 15, 18, 2, 4, new Color(0.95f, 0.72f, 0.28f, 1f));

        FillRect(texture, 5, 15, 5, 7, outline);
        FillRect(texture, 22, 15, 5, 7, outline);
        FillRect(texture, 6, 15, 3, 6, shirt);
        FillRect(texture, 23, 15, 3, 6, shirt);
        FillRect(texture, 5, 21, 4, 3, glove);
        FillRect(texture, 23, 21, 4, 3, glove);

        FillEllipse(texture, 16, 11, 8, 6, outline);
        FillEllipse(texture, 16, 11, 7, 5, skin);
        FillRect(texture, 9, 8, 3, 6, hair);
        FillRect(texture, 20, 8, 3, 6, hair);
        FillRect(texture, 11, 13, 10, 2, skinShadow);
        SetPixelSafe(texture, 12, 10, eye);
        SetPixelSafe(texture, 20, 10, eye);
        SetPixelSafe(texture, 13, 11, Color.white);
        SetPixelSafe(texture, 21, 11, Color.white);
        FillRect(texture, 15, 13, 4, 1, new Color(0.38f, 0.12f, 0.08f, 1f));

        FillEllipse(texture, 16, 6, 13, 3, outline);
        FillEllipse(texture, 16, 6, 12, 2, hatDark);
        FillRect(texture, 9, 1, 14, 7, outline);
        FillRect(texture, 10, 1, 12, 6, hat);
        FillRect(texture, 12, 2, 8, 2, hatLight);
        FillRect(texture, 8, 7, 16, 2, hatDark);
        FillRect(texture, 12, 6, 8, 1, hatLight);

        texture.Apply();
        return CreateSpriteFromTexture(texture, new Vector2(0.5f, 0.5f), 24f);
    }

    // Creates a simple pixel-art fallback sprite.
    private Sprite CreatePixelSprite(Color baseColor, Color accentColor, int size, bool character)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.46f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                Color pixel = Color.clear;

                if (character)
                {
                    if (distance <= radius)
                    {
                        pixel = baseColor;
                    }

                    if (distance <= radius * 0.55f && y > size * 0.45f)
                    {
                        pixel = Color.Lerp(baseColor, accentColor, 0.72f);
                    }

                    if (x < 2 || x > size - 3 || y < 2 || y > size - 3)
                    {
                        pixel.a *= 0.4f;
                    }
                }
                else
                {
                    pixel = baseColor;
                    if ((x + y) % 5 == 0)
                    {
                        pixel = Color.Lerp(baseColor, accentColor, 0.55f);
                    }
                }

                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply(true, false);
        return CreateSpriteFromTexture(texture, new Vector2(0.5f, 0.5f), size);
    }

    // Programmatically draws the red orbiting blade sprite.
    private Sprite CreateOrbitingBladeSprite()
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        ClearTexture(texture, Color.clear);

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        Color glow = new Color(1f, 0.12f, 0.06f, 0.34f);
        Color edge = new Color(0.34f, 0.02f, 0.02f, 1f);
        Color steel = new Color(1f, 0.46f, 0.36f, 1f);
        Color highlight = Color.white;
        Color hilt = new Color(0.82f, 0.12f, 0.08f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x - center.x;
                float py = y - center.y;
                Color pixel = Color.clear;
                float bodyT = Mathf.InverseLerp(-24f, 22f, px);
                float bladeHalfWidth = Mathf.Lerp(6.4f, 2.1f, bodyT);
                bool bladeBody = px >= -24f && px <= 22f && Mathf.Abs(py) <= bladeHalfWidth;
                bool bladeTip = px > 22f && px <= 30f && Mathf.Abs(py) <= (30f - px) * 0.48f;
                bool bladeGlow = px >= -28f && px <= 31f && Mathf.Abs(py) <= bladeHalfWidth + 4.4f;
                bool grip = px >= -31f && px < -23f && Mathf.Abs(py) <= 8.2f;
                bool guard = px >= -24f && px <= -19f && Mathf.Abs(py) <= 12.5f;

                if (bladeGlow || (px > 22f && px <= 31f && Mathf.Abs(py) <= (31f - px) * 0.58f + 3f))
                {
                    pixel = glow;
                }

                if (bladeBody || bladeTip)
                {
                    pixel = Mathf.Abs(py) > bladeHalfWidth - 1.3f ? edge : Color.Lerp(steel, highlight, Mathf.Clamp01((py + bladeHalfWidth) / Mathf.Max(0.01f, bladeHalfWidth * 2f)));
                }

                if (grip)
                {
                    pixel = Mathf.FloorToInt(Mathf.Abs(py)) % 4 == 0 ? edge : hilt;
                }

                if (guard)
                {
                    pixel = Mathf.Abs(py) > 9.5f ? edge : new Color(1f, 0.3f, 0.18f, 1f);
                }

                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply(true, false);
        return CreateSpriteFromTexture(texture, new Vector2(0.5f, 0.5f), 64f);
    }

    // Programmatically draws the red energy ring around the blades.
    private Sprite CreateOrbitingRingSprite()
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                float ring = Mathf.Clamp01(1f - Mathf.Abs(d - 0.82f) / 0.055f);
                float inner = Mathf.Clamp01(1f - Mathf.Abs(d - 0.62f) / 0.025f) * 0.38f;
                float sparkle = (x + y) % 23 == 0 && d > 0.7f && d < 0.93f ? 0.2f : 0f;
                float alpha = Mathf.Clamp01(ring * 0.7f + inner + sparkle);
                texture.SetPixel(x, y, new Color(1f, 0.18f, 0.08f, alpha));
            }
        }

        texture.Apply(true, false);
        return CreateSpriteFromTexture(texture, new Vector2(0.5f, 0.5f), 64f);
    }

    // Creates a soft circular sprite used for glows, shadows, and range markers.
    private Sprite CreateSoftDiscSprite(Color color, int size, float radiusScale, float centerFade)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f * radiusScale;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center) / radius;
                float alpha = Mathf.Clamp01(1f - d);
                alpha = Mathf.Pow(alpha, 1.7f);
                Color pixel = color;
                pixel.a *= Mathf.Lerp(centerFade, 1f, alpha) * alpha;
                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply(true, false);
        return CreateSpriteFromTexture(texture, new Vector2(0.5f, 0.5f), size);
    }

    // Programmatically draws the ground blood splat sprite.
    private Sprite CreateBloodSplatSprite()
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        ClearTexture(texture, Color.clear);
        Color blood = new Color(0.5f, 0.02f, 0.025f, 0.9f);
        FillEllipse(texture, 32, 32, 18, 9, blood);
        FillEllipse(texture, 22, 27, 9, 5, blood);
        FillEllipse(texture, 44, 36, 11, 6, blood);
        FillEllipse(texture, 34, 22, 5, 3, blood);
        FillEllipse(texture, 18, 39, 4, 3, blood);
        FillEllipse(texture, 51, 25, 3, 2, blood);
        texture.Apply(true, false);
        return CreateSpriteFromTexture(texture, new Vector2(0.5f, 0.5f), size);
    }

    // Programmatically draws a neon sign sprite.
    private Sprite CreateNeonSignSprite()
    {
        const int width = 64;
        const int height = 24;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, true);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        ClearTexture(texture, Color.clear);
        FillRect(texture, 2, 4, 60, 16, new Color(0f, 0f, 0f, 0.7f));
        FillRect(texture, 4, 6, 56, 2, Color.white);
        FillRect(texture, 4, 16, 56, 2, Color.white);
        FillRect(texture, 8, 10, 8, 4, Color.white);
        FillRect(texture, 21, 10, 14, 4, Color.white);
        FillRect(texture, 41, 10, 12, 4, Color.white);
        texture.Apply(true, false);
        return CreateSpriteFromTexture(texture, new Vector2(0.5f, 0.5f), width);
    }

    // Fills a rectangle area on a texture.
    private static void FillRect(Texture2D texture, int x, int y, int width, int height, Color color)
    {
        for (int yy = y; yy < y + height; yy++)
        {
            for (int xx = x; xx < x + width; xx++)
            {
                SetPixelSafe(texture, xx, yy, color);
            }
        }
    }

    // Fills an ellipse area on a texture.
    private static void FillEllipse(Texture2D texture, int centerX, int centerY, int radiusX, int radiusY, Color color)
    {
        float rx = Mathf.Max(1f, radiusX);
        float ry = Mathf.Max(1f, radiusY);
        for (int y = centerY - radiusY; y <= centerY + radiusY; y++)
        {
            for (int x = centerX - radiusX; x <= centerX + radiusX; x++)
            {
                float dx = (x - centerX) / rx;
                float dy = (y - centerY) / ry;
                if (dx * dx + dy * dy <= 1f)
                {
                    SetPixelSafe(texture, x, y, color);
                }
            }
        }
    }

    // Writes one texture pixel only when the coordinates are inside bounds.
    private static void SetPixelSafe(Texture2D texture, int x, int y, Color color)
    {
        if (x < 0 || x >= texture.width || y < 0 || y >= texture.height)
        {
            return;
        }

        texture.SetPixel(x, y, color);
    }
}

public sealed partial class ZombieStormMainMenuUI
{
    // Creates and caches a simple fallback cover when the menu art is missing.
    private Sprite GetBackgroundFallbackSprite()
    {
        if (backgroundFallbackSprite != null)
        {
            return backgroundFallbackSprite;
        }

        Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color32[] pixels = new Color32[16];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color32(40, 45, 38, 255);
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        backgroundFallbackSprite = ZombieStormTextureUtility.CreateSprite(
            texture,
            new Rect(0f, 0f, 4f, 4f),
            new Vector2(0.5f, 0.5f),
            4f);
        return backgroundFallbackSprite;
    }

    // Creates a one-pixel solid sprite for UI images that only need flat color.
    private Sprite CreateSolidSprite(string name)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = name;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixel(0, 0, Color.white);
        texture.Apply(false, false);
        return ZombieStormTextureUtility.CreateSprite(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
    }
}
