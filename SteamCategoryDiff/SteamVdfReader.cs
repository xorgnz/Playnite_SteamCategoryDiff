using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using Playnite.SDK;

namespace SteamCategoryDiff
{
    public static class SteamVdfReader
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        /// <summary>
        /// Reads Steam collections/categories from sharedconfig.vdf.
        /// Steam stores collections as per-app "tags" in:
        /// UserLocalConfigStore / Software / Valve / Steam / Apps / {appid} / tags
        /// </summary>
        public static Dictionary<string, HashSet<string>> ReadAppCategories(string sharedConfigPath)
        {
            if (!File.Exists(sharedConfigPath))
            {
                throw new FileNotFoundException("sharedconfig.vdf not found", sharedConfigPath);
            }

            var text = File.ReadAllText(sharedConfigPath);
            logger.Debug(sharedConfigPath);
            logger.Debug(text);

            // Parse KeyValues1 text VDF
            var doc = VdfConvert.Deserialize(text);
            var root = doc?.Value as VObject;
            if (root == null)
            {
                throw new InvalidDataException("Failed to parse sharedconfig.vdf (root is not an object).");
            }

            // Try common casing variants
            var apps =
                GetPath(root, "UserLocalConfigStore", "Software", "Valve", "Steam", "Apps")
                ?? GetPath(root, "userlocalconfigstore", "software", "valve", "steam", "apps");

            if (apps == null)
            {
                throw new InvalidDataException("Could not locate Apps tree in sharedconfig.vdf.");
            }

            var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in apps)
            {
                var appId = kvp.Key;
                if (string.IsNullOrWhiteSpace(appId) || !appId.All(char.IsDigit))
                {
                    continue;
                }

                var appObj = kvp.Value as VObject;
                if (appObj == null)
                {
                    continue;
                }

                // "tags" contains the category names (keys are usually 0,1,2...)
                var tagsObj =
                    GetChildObject(appObj, "tags")
                    ?? GetChildObject(appObj, "Tags");

                if (tagsObj == null || tagsObj.Count == 0)
                {
                    continue;
                }

                var cats = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var tag in tagsObj)
                {
                    var val = tag.Value?.ToString();
                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        cats.Add(val.Trim());
                    }
                }

                if (cats.Count > 0)
                {
                    result[appId] = cats;
                }
            }

            return result;
        }

        private static VObject GetPath(VObject root, params string[] path)
        {
            VObject cur = root;
            foreach (var seg in path)
            {
                var next = GetChildObject(cur, seg);
                if (next == null) return null;
                cur = next;
            }
            return cur;
        }

        private static VObject GetChildObject(VObject parent, string key)
        {
            if (parent == null) return null;

            // Exact match first
            if (parent.TryGetValue(key, out var v))
            {
                return v as VObject;
            }

            // Case-insensitive fallback
            foreach (var kv in parent)
            {
                if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    return kv.Value as VObject;
                }
            }

            return null;
        }
    }
}