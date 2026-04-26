#if UNITY_EDITOR && UNITY_6000_0_OR_NEWER
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Control.Tools.Required.Editor
{
    internal static class RequiredIconUtility
    {
        private const string DefaultIconPath = "Packages/com.control-tools.required-attribute/Editor/Icons/Warning.svg";
        private const string LegacyIconPath = "Packages/com.control-tools.required-attribute/Editor/Icons/Warning.svg";
        private const int IconSize = 16;

        private static readonly Dictionary<Color32, Texture2D> tintedCache = new Dictionary<Color32, Texture2D>();
        private static string cachedIconPath;

        public static Texture2D GetIcon(Color color)
        {
            Color32 key = color;
            if (tintedCache.TryGetValue(key, out Texture2D cached) && cached != null)
            {
                return cached;
            }

            Texture2D texture = CreateTintedTexture(color);
            tintedCache[key] = texture;
            return texture;
        }

        public static Texture2D RedIcon => GetIcon(new Color(0.95f, 0.18f, 0.14f, 1f));
        public static Texture2D GreenIcon => GetIcon(new Color(0.20f, 0.78f, 0.34f, 1f));

        private static Texture2D CreateTintedTexture(Color tint)
        {
            TouchSourceSvgAsset();

            Texture2D texture = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false)
            {
                name = "Control Required Icon",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color[] pixels = new Color[IconSize * IconSize];
            for (int y = 0; y < IconSize; y++)
            {
                for (int x = 0; x < IconSize; x++)
                {
                    Color pixel = tint;
                    pixel.a *= GetMaskAlpha(x, IconSize - 1 - y);
                    pixels[y * IconSize + x] = pixel;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static void TouchSourceSvgAsset()
        {
            string iconPath = ResolveIconPath();
            if (!string.IsNullOrEmpty(iconPath))
            {
                AssetDatabase.LoadAssetAtPath<Object>(iconPath);
            }
        }

        private static string ResolveIconPath()
        {
            if (!string.IsNullOrEmpty(cachedIconPath))
            {
                return cachedIconPath;
            }

            if (AssetDatabase.LoadAssetAtPath<Object>(DefaultIconPath) != null)
            {
                cachedIconPath = DefaultIconPath;
                return cachedIconPath;
            }

            if (AssetDatabase.LoadAssetAtPath<Object>(LegacyIconPath) != null)
            {
                cachedIconPath = LegacyIconPath;
                return cachedIconPath;
            }

            string[] guids = AssetDatabase.FindAssets("Warning");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.EndsWith("/Warning.svg", System.StringComparison.OrdinalIgnoreCase))
                {
                    cachedIconPath = path;
                    return cachedIconPath;
                }
            }

            return null;
        }

        private static float GetMaskAlpha(int x, int y)
        {
            Vector2 a = new Vector2(8f, 1f);
            Vector2 b = new Vector2(15f, 15f);
            Vector2 c = new Vector2(1f, 15f);
            Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
            if (!IsPointInTriangle(point, a, b, c))
            {
                return 0f;
            }

            bool inStem = x >= 7 && x <= 8 && y >= 6 && y <= 9;
            bool inDot = x >= 7 && x <= 8 && y >= 11 && y <= 12;
            return inStem || inDot ? 0f : 1f;
        }

        private static bool IsPointInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            float area = Sign(point, a, b);
            float area2 = Sign(point, b, c);
            float area3 = Sign(point, c, a);
            bool hasNegative = area < 0f || area2 < 0f || area3 < 0f;
            bool hasPositive = area > 0f || area2 > 0f || area3 > 0f;
            return !(hasNegative && hasPositive);
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }
    }
}
#endif



