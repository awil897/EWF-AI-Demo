// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.ObjectModel;
using System.Xml.Xsl;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;

namespace StylesheetUi2.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _applicationTitle = "EWF - AI Tools Demo";

        [ObservableProperty]
        private bool _enabledMaster = Settings.Default.enabledMaster;

        [ObservableProperty]
        private ObservableCollection<object> _menuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "Stylesheet Generator",
                Name = "XSLT",
                ToolTip = "XSLT Generator",
                Height= 40,
                Icon = new SymbolIcon { Symbol = SymbolRegular.BookNumber20 },
                TargetPageType = typeof(Views.Pages.DashboardPage),
                //Margin = new Thickness(0,75,0,0),
                FontSize = 11,
            },
            new NavigationViewItem()
            {
                Content = "Variable Translation",
                Name = "Variables",
                ToolTip = "Variable Translation",
                FontSize = 11,
                //Height= 75,
                Icon = new SymbolIcon { Symbol = SymbolRegular.BracesVariable24 },
                TargetPageType = typeof(Views.Pages.DataPage)
            }
        };

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "Settings",
                Margin = new Thickness(0,0,0,30),
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                FontSize = 10,
                //Height = 75,
                //MinHeight = 75,
                TargetPageType = typeof(Views.Pages.SettingsPage)
            }
        };

        [ObservableProperty]
        private ObservableCollection<MenuItem> _trayMenuItems = new()
        {
            new MenuItem { Header = "Home", Tag = "tray_home" }
        };
    }
}
