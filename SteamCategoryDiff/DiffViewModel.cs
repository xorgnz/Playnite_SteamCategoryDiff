using Microsoft.Win32;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SteamCategoryDiff
{
    public sealed class DiffViewModel : ObservableObject
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        // Steam library plugin id from Playnite's Steam extension source.
        private static readonly Guid SteamPluginId = Guid.Parse("CB91DFC9-B977-43BF-8E70-55F46E410FAB");

        private readonly IPlayniteAPI api;
        private readonly List<DiffItem> allItems = new List<DiffItem>();

        public ObservableCollection<DiffItem> Items { get; } = new ObservableCollection<DiffItem>();

        private bool showSteamOnly = true;
        public bool ShowSteamOnly
        {
            get => showSteamOnly;
            set
            {
                if (showSteamOnly != value)
                {
                    SetValue(ref showSteamOnly, value);
                    ApplyFilters();
                }
            }
        }

        private bool showPlayniteOnly = true;
        public bool ShowPlayniteOnly
        {
            get => showPlayniteOnly;
            set
            {
                if (showPlayniteOnly != value)
                {
                    SetValue(ref showPlayniteOnly, value);
                    ApplyFilters();
                }
            }
        }

        private bool showBoth = true;
        public bool ShowBoth
        {
            get => showBoth;
            set
            {
                if (showBoth != value)
                {
                    SetValue(ref showBoth, value);
                    ApplyFilters();
                }
            }
        }

        private bool showIdenticals = false;
        public bool ShowIdenticals
        {
            get => showIdenticals;
            set
            {
                if (showIdenticals != value)
                {
                    SetValue(ref showIdenticals, value);
                    ApplyFilters();
                }
            }
        }

        private bool isLoading;
        public bool IsLoading
        {
            get => isLoading;
            private set => SetValue(ref isLoading, value);
        }

        public ICommand LoadCommand { get; }
        public ICommand CopyReportCommand { get; }

        private readonly RelayCommand copyReportRelay;
        private readonly RelayCommand loadRelay;

        public DiffViewModel(IPlayniteAPI api)
        {
            this.api = api;

            loadRelay = new RelayCommand(async () => await LoadAsync(), () => !IsLoading);
            LoadCommand = loadRelay;

            copyReportRelay = new RelayCommand(() => CopyReport(), () => Items.Count > 0);
            CopyReportCommand = copyReportRelay;
        }

        private async Task LoadAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            loadRelay.RaiseCanExecuteChanged();
            copyReportRelay.RaiseCanExecuteChanged();

            Items.Clear();
            allItems.Clear();

            try
            {
                // 1) Find Steam root
                var steamPath = TryGetSteamPathFromRegistry();
                if (string.IsNullOrWhiteSpace(steamPath) || !Directory.Exists(steamPath))
                {
                    var picked = api.Dialogs.SelectFolder();
                    if (string.IsNullOrWhiteSpace(picked) || !Directory.Exists(picked))
                    {
                        return;
                    }
                    steamPath = picked;
                }

                // 2) Pick the modern Steam collections file (cloud-storage-namespace-1.json)
                var collectionsFile = SteamCollectionsJsonReader.FindBestCloudStorageFile(steamPath);
                if (string.IsNullOrWhiteSpace(collectionsFile) || !File.Exists(collectionsFile))
                {
                    api.Dialogs.ShowErrorMessage(
                        "Could not find Steam collections JSON:\n" +
                        "userdata/*/config/cloudstorage/cloud-storage-namespace-1.json\n\n" +
                        $"Steam folder: {steamPath}",
                        "Steam Category Diff");

                    return;
                }

                Logger.Info($"Using Steam collections JSON: {collectionsFile} ({new FileInfo(collectionsFile).Length} bytes)");

                // 3) Read Steam categories off-thread (file can be large)
                Dictionary<string, HashSet<string>> steamCats;
                try
                {
                    steamCats = await Task.Run(() => SteamCollectionsJsonReader.ReadCollections(collectionsFile));
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed reading Steam collections JSON.");
                    api.Dialogs.ShowErrorMessage("Failed to read Steam collections JSON:\n" + ex.Message, "Steam Category Diff");
                    return;
                }

                // 4) Read Playnite categories on UI thread (API access is safest here)
                var playniteCats = ReadPlayniteSteamGameCategories();
                var playniteNameByAppId = ReadPlayniteSteamGameNames();

                // 5) Diff (can be done off-thread; it only uses dictionaries now)
                var diffItems = await Task.Run(() =>
                {
                    var allAppIds = new HashSet<string>(steamCats.Keys, StringComparer.OrdinalIgnoreCase);
                    foreach (var id in playniteCats.Keys) allAppIds.Add(id);

                    var list = new List<DiffItem>(capacity: allAppIds.Count);

                    foreach (var appId in allAppIds.OrderBy(x => x))
                    {
                        var availableInSteam = steamCats.TryGetValue(appId, out var s);
                        var availableInPlaynite = playniteCats.TryGetValue(appId, out var p);

                        s ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        p ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        var name = playniteNameByAppId.TryGetValue(appId, out var n) ? n : $"Steam App {appId}";

                        list.Add(new DiffItem
                        {
                            Name = name,
                            AppId = appId,
                            AvailableInPlaynite = availableInPlaynite,
                            AvailableInSteam = availableInSteam,
                            PlayniteCategories = p.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
                            SteamCategories = s.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList()
                        });
                    }

                    return list;
                });

                allItems.AddRange(diffItems.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase).ThenBy(i => i.AppId));
                ApplyFilters();
            }
            finally
            {
                IsLoading = false;
                loadRelay.RaiseCanExecuteChanged();
                copyReportRelay.RaiseCanExecuteChanged();
            }
        }

        private Dictionary<string, HashSet<string>> ReadPlayniteSteamGameCategories()
        {
            var categoriesById = api.Database.Categories.ToDictionary(c => c.Id, c => c.Name);

            // appid -> categories
            var map = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var game in api.Database.Games)
            {
                if (game == null) continue;
                if (game.PluginId != SteamPluginId) continue;

                var appId = NormalizeSteamAppId(game);
                if (appId == null) continue;

                var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (game.CategoryIds != null)
                {
                    foreach (var cid in game.CategoryIds)
                    {
                        if (categoriesById.TryGetValue(cid, out var name) && !string.IsNullOrWhiteSpace(name))
                        {
                            set.Add(name.Trim());
                        }
                    }
                }

                map[appId] = set;
            }

            return map;
        }

        private Dictionary<string, string> ReadPlayniteSteamGameNames()
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var game in api.Database.Games)
            {
                if (game == null) continue;
                if (game.PluginId != SteamPluginId) continue;

                var appId = NormalizeSteamAppId(game);
                if (appId == null) continue;

                if (!map.ContainsKey(appId))
                {
                    map[appId] = game.Name ?? $"Steam App {appId}";
                }
            }

            return map;
        }

        private static string? NormalizeSteamAppId(Game game)
        {
            var id = game.GameId;
            if (string.IsNullOrWhiteSpace(id)) return null;

            id = id.Trim();
            return id.All(char.IsDigit) ? id : null;
        }

        private static string? TryGetSteamPathFromRegistry()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                {
                    var val = key?.GetValue("SteamPath") as string;
                    if (!string.IsNullOrWhiteSpace(val)) return val;
                }
            }
            catch { /* ignore */ }

            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"Software\Valve\Steam"))
                {
                    var val = key?.GetValue("InstallPath") as string;
                    if (!string.IsNullOrWhiteSpace(val)) return val;
                }
            }
            catch { /* ignore */ }

            return null;
        }

        private void CopyReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Steam Category Diff Report");
            sb.AppendLine("========================================");
            sb.AppendLine();

            foreach (var item in allItems.Where(i => i.HasDiff))
            {
                sb.AppendLine($"{item.Name} (AppID {item.AppId})");
                sb.AppendLine($"  Available in Playnite: {item.AvailableInPlayniteText}");
                sb.AppendLine($"  Available in Steam: {item.AvailableInSteamText}");
                sb.AppendLine($"  Playnite categories: {item.PlayniteCategoriesText}");
                sb.AppendLine($"  Steam categories: {item.SteamCategoriesText}");
                sb.AppendLine($"  Categories same: {item.CategoriesSameText}");
                sb.AppendLine();
            }

            Clipboard.SetText(sb.ToString());
        }

        private void ApplyFilters()
        {
            Items.Clear();

            var filtered = allItems.Where(i => PassesLibraryFilter(i) && PassesDiffFilter(i));

            foreach (var item in filtered)
            {
                Items.Add(item);
            }

            copyReportRelay.RaiseCanExecuteChanged();
        }

        private bool PassesLibraryFilter(DiffItem item)
        {
            var steamOnly = item.AvailableInSteam && !item.AvailableInPlaynite;
            var playniteOnly = item.AvailableInPlaynite && !item.AvailableInSteam;
            var both = item.AvailableInSteam && item.AvailableInPlaynite;

            return (steamOnly && ShowSteamOnly) ||
                   (playniteOnly && ShowPlayniteOnly) ||
                   (both && ShowBoth);
        }

        private bool PassesDiffFilter(DiffItem item)
        {
            return ShowIdenticals || item.HasDiff;
        }
    }

    internal sealed class RelayCommand : ICommand
    {
        private readonly Func<bool>? canExecute;
        private readonly Action execute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this.canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object parameter) => canExecute?.Invoke() ?? true;

        public void Execute(object parameter) => execute();

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
