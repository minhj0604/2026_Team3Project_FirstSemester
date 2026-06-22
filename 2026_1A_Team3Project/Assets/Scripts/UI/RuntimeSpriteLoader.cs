using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Team3Project.UI
{
    public static class RuntimeSpriteLoader
    {
        private static readonly Dictionary<string, Sprite> Cache = new();

        public static Sprite LoadFromAssetPath(params string[] assetPathParts)
        {
            var relativePath = Path.Combine(assetPathParts);
            if (Cache.TryGetValue(relativePath, out var cached))
            {
                return cached;
            }

            var fullPath = Path.Combine(Application.dataPath, relativePath);
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"Sprite file not found: {fullPath}");
                return null;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(fullPath)))
            {
                Debug.LogWarning($"Failed to load sprite file: {fullPath}");
                return null;
            }

            texture.name = Path.GetFileNameWithoutExtension(fullPath);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            Cache[relativePath] = sprite;
            return sprite;
        }
    }
}
