using System.Collections.Generic;

namespace SteamCategoryDiff
{
    public sealed class DiffItem
    {
        public string Name { get; set; } = "";
        public string AppId { get; set; } = "";

        public bool AvailableInPlaynite { get; set; }
        public bool AvailableInSteam { get; set; }

        public List<string> PlayniteCategories { get; set; } = new List<string>();
        public List<string> SteamCategories { get; set; } = new List<string>();

        public bool HasDiff => CategoriesSameText == "No" || CategoriesSameText == "--";

        public string AvailableInPlayniteText => AvailableInPlaynite ? "Yes" : "No";
        public string AvailableInSteamText => AvailableInSteam ? "Yes" : "No";

        public string PlayniteCategoriesText =>
            AvailableInPlaynite ? JoinCategories(PlayniteCategories) : "--";

        public string SteamCategoriesText =>
            AvailableInSteam ? JoinCategories(SteamCategories) : "--";

        public string CategoriesSameText
        {
            get
            {
                if (!AvailableInPlaynite || !AvailableInSteam)
                {
                    return "--";
                }

                var playniteSet = new HashSet<string>(PlayniteCategories ?? new List<string>(), System.StringComparer.OrdinalIgnoreCase);
                var steamSet = new HashSet<string>(SteamCategories ?? new List<string>(), System.StringComparer.OrdinalIgnoreCase);
                return playniteSet.SetEquals(steamSet) ? "Yes" : "No";
            }
        }

        private static string JoinCategories(List<string> categories)
        {
            return categories == null || categories.Count == 0 ? "" : string.Join(", ", categories);
        }
    }
}
