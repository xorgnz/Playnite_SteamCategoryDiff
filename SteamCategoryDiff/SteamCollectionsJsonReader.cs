using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SteamCategoryDiff
{
    public static class SteamCollectionsJsonReader
    {
        private const string CollectionPrefix = "user-collections.";

        public static Dictionary<string, HashSet<string>> ReadCollections(string path)
        {
            var json = File.ReadAllText(path);
            var root = JArray.Parse(json);

            var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in root)
            {
                if (entry is not JArray pair || pair.Count < 2)
                    continue;

                var key = pair[0]?.ToString();
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                if (!key.StartsWith(CollectionPrefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                var obj = pair[1] as JObject;
                if (obj == null)
                    continue;

                var valueJson = obj["value"]?.ToString();
                if (string.IsNullOrWhiteSpace(valueJson))
                    continue;

                JObject value;

                try
                {
                    value = JObject.Parse(valueJson);
                }
                catch
                {
                    continue;
                }

                var name = value["name"]?.ToString();
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var added = value["added"]?.ToObject<List<int>>() ?? new List<int>();
                var removed = new HashSet<int>(
                    value["removed"]?.ToObject<List<int>>() ?? new List<int>()
                );

                foreach (var appId in added)
                {
                    if (removed.Contains(appId))
                        continue;

                    var id = appId.ToString();

                    if (!result.TryGetValue(id, out var set))
                    {
                        set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        result[id] = set;
                    }

                    set.Add(name);
                }
            }

            return result;
        }

        public static string FindBestCloudStorageFile(string steamRoot)
        {
            var userdata = Path.Combine(steamRoot, "userdata");

            var candidates = Directory
                .EnumerateFiles(userdata, "cloud-storage-namespace-1.json", SearchOption.AllDirectories)
                .Select(p => new FileInfo(p))
                .OrderByDescending(fi => fi.Length)
                .ThenByDescending(fi => fi.LastWriteTimeUtc)
                .FirstOrDefault();

            return candidates?.FullName;
        }
    }
}