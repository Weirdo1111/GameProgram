using System;
using System.Collections.Generic;
using UnityEngine;

// Shared texture processing and Sprite construction used by runtime art loading.
public sealed partial class ZombieStormGameController
{
    // Creates a sprite from an entire texture.
    private static Sprite CreateSpriteFromTexture(Texture2D texture, Vector2 pivot, float pixelsPerUnit)
    {
        return CreateSpriteFromTexture(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            pivot,
            pixelsPerUnit);
    }

    // Creates a sprite from a selected texture region.
    private static Sprite CreateSpriteFromTexture(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit)
    {
        return ZombieStormTextureUtility.CreateSprite(texture, rect, pivot, pixelsPerUnit);
    }

    // Calculates a sprite pivot from the center of its visible pixels.
    private static Vector2 CalculateOpaqueCenterPivot(Texture2D texture)
    {
        Color32[] pixels = texture.GetPixels32();
        long sumX = 0;
        long sumY = 0;
        long count = 0;

        for (int y = 0; y < texture.height; y++)
        {
            int row = y * texture.width;
            for (int x = 0; x < texture.width; x++)
            {
                if (pixels[row + x].a <= 20)
                {
                    continue;
                }

                sumX += x;
                sumY += y;
                count++;
            }
        }

        if (count == 0)
        {
            return new Vector2(0.5f, 0.5f);
        }

        return new Vector2(
            Mathf.Clamp01(sumX / (float)count / Mathf.Max(1, texture.width - 1)),
            Mathf.Clamp01(sumY / (float)count / Mathf.Max(1, texture.height - 1)));
    }

    // Makes near-black sprite-sheet backgrounds transparent while preserving fire colors.
    private static void RemoveBlackBackground(Texture2D texture)
    {
        Color32[] pixels = texture.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 color = pixels[i];
            if (color.a < 10)
            {
                continue;
            }

            int brightness = color.r + color.g + color.b;
            if (brightness <= 34 && color.r <= 18 && color.g <= 18 && color.b <= 18)
            {
                color.a = 0;
                pixels[i] = color;
            }
        }

        texture.SetPixels32(pixels);
    }

    // Removes checkerboard transparency background connected to image edges.
    private void RemoveEdgeCheckerBackground(Texture2D texture)
    {
        int width = texture.width;
        int height = texture.height;
        Color32[] pixels = texture.GetPixels32();
        bool[] visited = new bool[pixels.Length];
        Queue<int> queue = new Queue<int>();

        for (int x = 0; x < width; x++)
        {
            TryQueueBackgroundPixel(x, 0, width, pixels, visited, queue);
            TryQueueBackgroundPixel(x, height - 1, width, pixels, visited, queue);
        }

        for (int y = 0; y < height; y++)
        {
            TryQueueBackgroundPixel(0, y, width, pixels, visited, queue);
            TryQueueBackgroundPixel(width - 1, y, width, pixels, visited, queue);
        }

        while (queue.Count > 0)
        {
            int index = queue.Dequeue();
            pixels[index].a = 0;
            int x = index % width;
            int y = index / width;
            TryQueueBackgroundPixel(x + 1, y, width, pixels, visited, queue);
            TryQueueBackgroundPixel(x - 1, y, width, pixels, visited, queue);
            TryQueueBackgroundPixel(x, y + 1, width, pixels, visited, queue);
            TryQueueBackgroundPixel(x, y - 1, width, pixels, visited, queue);
        }

        texture.SetPixels32(pixels);
        texture.Apply(true, false);
    }

    // Cleans leftover edge colors to reduce white or gray outlines.
    private void CleanBackgroundFringe(Texture2D texture)
    {
        int width = texture.width;
        int height = texture.height;
        Color32[] pixels = texture.GetPixels32();
        Color32[] cleaned = new Color32[pixels.Length];
        Array.Copy(pixels, cleaned, pixels.Length);

        for (int pass = 0; pass < 4; pass++)
        {
            bool changed = false;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    Color32 color = pixels[index];
                    if (color.a == 0 || !TouchesTransparentPixel(x, y, width, height, pixels))
                    {
                        continue;
                    }

                    int max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
                    int min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
                    int average = (color.r + color.g + color.b) / 3;
                    int saturation = max - min;
                    if (average >= 188 && saturation <= 82)
                    {
                        cleaned[index].a = 0;
                        changed = true;
                    }
                    else if (average >= 172 && saturation <= 96)
                    {
                        cleaned[index].a = (byte)Mathf.Min(color.a, 92);
                        changed = true;
                    }
                }
            }

            if (!changed)
            {
                break;
            }

            Color32[] swap = pixels;
            pixels = cleaned;
            cleaned = swap;
            Array.Copy(pixels, cleaned, pixels.Length);
        }

        DilateTransparentPixels(texture, pixels);
    }

    // Checks whether a pixel touches transparency during edge cleanup.
    private static bool TouchesTransparentPixel(int x, int y, int width, int height, Color32[] pixels)
    {
        for (int yy = y - 1; yy <= y + 1; yy++)
        {
            for (int xx = x - 1; xx <= x + 1; xx++)
            {
                if (xx == x && yy == y)
                {
                    continue;
                }

                if (xx < 0 || xx >= width || yy < 0 || yy >= height)
                {
                    return true;
                }

                if (pixels[yy * width + xx].a < 20)
                {
                    return true;
                }
            }
        }

        return false;
    }

    // Fills transparent pixels with neighbor colors to prevent scaled texture borders.
    private void DilateTransparentPixels(Texture2D texture, Color32[] pixels)
    {
        int width = texture.width;
        int height = texture.height;
        Color32[] dilated = new Color32[pixels.Length];
        Array.Copy(pixels, dilated, pixels.Length);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                if (pixels[index].a >= 20)
                {
                    continue;
                }

                Color32 replacement;
                if (TryFindOpaqueNeighborColor(x, y, width, height, pixels, out replacement))
                {
                    replacement.a = 0;
                    dilated[index] = replacement;
                }
            }
        }

        texture.SetPixels32(dilated);
        texture.Apply(false, false);
    }

    // Finds a nearby opaque pixel color for transparent edge filling.
    private static bool TryFindOpaqueNeighborColor(int x, int y, int width, int height, Color32[] pixels, out Color32 color)
    {
        for (int radius = 1; radius <= 3; radius++)
        {
            int r = 0;
            int g = 0;
            int b = 0;
            int count = 0;
            for (int yy = y - radius; yy <= y + radius; yy++)
            {
                for (int xx = x - radius; xx <= x + radius; xx++)
                {
                    if (xx < 0 || xx >= width || yy < 0 || yy >= height)
                    {
                        continue;
                    }

                    Color32 sample = pixels[yy * width + xx];
                    if (sample.a < 210)
                    {
                        continue;
                    }

                    r += sample.r;
                    g += sample.g;
                    b += sample.b;
                    count++;
                }
            }

            if (count > 0)
            {
                color = new Color32((byte)(r / count), (byte)(g / count), (byte)(b / count), 0);
                return true;
            }
        }

        color = new Color32(0, 0, 0, 0);
        return false;
    }

    // Adds a likely background pixel to the flood-fill queue.
    private static void TryQueueBackgroundPixel(int x, int y, int width, Color32[] pixels, bool[] visited, Queue<int> queue)
    {
        int height = pixels.Length / width;
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return;
        }

        int index = y * width + x;
        if (visited[index] || !IsCheckerBackground(pixels[index]))
        {
            return;
        }

        visited[index] = true;
        queue.Enqueue(index);
    }

    // Checks whether a pixel color looks like checkerboard background.
    private static bool IsCheckerBackground(Color32 color)
    {
        if (color.a < 10)
        {
            return true;
        }

        int max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
        int min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
        int average = (color.r + color.g + color.b) / 3;
        int saturation = max - min;
        if (average >= 232)
        {
            return true;
        }

        return saturation <= 48 && average >= 168;
    }

    // Places player frames onto a consistent texture canvas size.
    private Texture2D NormalizePlayerFrame(Texture2D source, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, true);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        ClearTexture(texture, Color.clear);

        int offsetX = Mathf.RoundToInt((width - source.width) * 0.5f);
        int offsetY = 0;
        Color[] pixels = source.GetPixels();
        texture.SetPixels(offsetX, offsetY, source.width, source.height, pixels);
        texture.Apply(true, false);
        return texture;
    }

    // Fills an entire texture with one color.
    private static void ClearTexture(Texture2D texture, Color color)
    {
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, color);
            }
        }
    }
}

internal static class ZombieStormTextureUtility
{
    // Centralizes Unity Sprite creation for loaded and generated runtime textures.
    internal static Sprite CreateSprite(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit)
    {
        return Sprite.Create(texture, rect, pivot, pixelsPerUnit);
    }
}
