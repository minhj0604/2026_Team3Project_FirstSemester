using System.Collections.Generic;
using Team3Project.GameSystems;
using UnityEngine;

namespace Team3Project.UI
{
    public static class MergeResourceVisuals
    {
        private static readonly Dictionary<string, Sprite> Sprites = new();

        public static void Register(MergeResource resource, Sprite sprite)
        {
            if (!resource.CanUse || sprite == null)
            {
                return;
            }

            Sprites[Key(resource.Family, resource.Stage)] = sprite;
            var familyKey = Key(resource.Family, -1);
            if (!Sprites.ContainsKey(familyKey))
            {
                Sprites[familyKey] = sprite;
            }
        }

        public static bool TryGetSprite(MergeResource resource, out Sprite sprite)
        {
            return Sprites.TryGetValue(Key(resource.Family, resource.Stage), out sprite)
                || Sprites.TryGetValue(Key(resource.Family, -1), out sprite);
        }

        public static Color GetTint(MergeResource resource)
        {
            return resource.Family == ResourceFamily.Egg && resource.Stage >= 3
                ? new Color(1f, 0.88f, 0.36f, 1f)
                : Color.white;
        }

        private static string Key(ResourceFamily family, int stage)
        {
            return $"{(int)family}:{stage}";
        }
    }
}
