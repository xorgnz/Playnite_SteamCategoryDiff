using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;

namespace SteamCategoryDiff
{
    public class SteamCategoryDiffPlugin : GenericPlugin
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        public SteamCategoryDiffPlugin(IPlayniteAPI api) : base(api)
        {
            Logger.Info("SteamCategoryDiff plugin loaded.");
        }

        public override Guid Id => Guid.Parse("6f2a4c84-9df7-4af1-9f4c-7e4f59c0c111"); // must match extension.yaml Id

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            // Desktop mode menu integration is the common pattern for tooling like this. :contentReference[oaicite:3]{index=3}
            yield return new MainMenuItem
            {
                MenuSection = "@Extensions",
                Description = "Steam Category Diff",
                Action = _ =>
                {
                    try
                    {
                        ShowDiffWindow();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Failed to show Steam Category Diff window.");
                        PlayniteApi.Dialogs.ShowErrorMessage(ex.Message, "Steam Category Diff");
                    }
                }
            };
        }

        private void ShowDiffWindow()
        {
            var vm = new DiffViewModel(PlayniteApi);
            var window = PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions
            {
                ShowCloseButton = true
            });

            window.Title = "Steam Category Diff";
            window.Width = 1100;
            window.Height = 700;
            window.Content = new DiffWindow { DataContext = vm };
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;

            vm.LoadCommand.Execute(null);

            window.ShowDialog();
        }
    }
}
