/*
 * Copyright 2024 Đorđe Mančić
 * This file is part of picoTask.
 * 
 * picoTask is free software: you can redistribute it and/or modify it under the terms 
 * of the GNU General Public License as published by the Free Software Foundation, 
 * either version 3 of the License, or (at your option) any later version.
 * 
 * picoTask is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
 * See the GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License along with picoTask. 
 * If not, see <https://www.gnu.org/licenses/>. 
 */
using System;
using System.Windows;
using Wpf.Ui.Controls;

namespace picoTask
{
    public partial class SettingsWindow : FluentWindow
    {
        App app = (App)Application.Current;

        public SettingsWindow()
        {
            InitializeComponent();

            Settings settings = app.Settings;

            RepeatPlaybackSwitch.IsChecked = settings.RepeatPlayback;
            AlwaysOnTopSwitch.IsChecked = settings.AlwaysOnTop;
            switch (settings.PlaybackSpeed)
            {
                case 0.5:
                    PlaybackSpeed50Button.IsChecked = true;
                    break;
                case 1:
                    PlaybackSpeed100Button.IsChecked = true;
                    break;
                case 2:
                    PlaybackSpeed200Button.IsChecked = true;
                    break;
                case 4:
                    PlaybackSpeed400Button.IsChecked = true;
                    break;
                case 8:
                    PlaybackSpeed800Button.IsChecked = true;
                    break;
                default:
                    PlaybackSpeed100Button.IsChecked = true;
                    break;
            }

            RepeatPlaybackSwitch.Checked += (_, _) => settings.RepeatPlayback = true;
            RepeatPlaybackSwitch.Unchecked += (_, _) => settings.RepeatPlayback = false;
            AlwaysOnTopSwitch.Checked += (_, _) => settings.AlwaysOnTop = true;
            AlwaysOnTopSwitch.Unchecked += (_, _) => settings.AlwaysOnTop = false;
            PlaybackSpeed50Button.Checked += (_, _) => settings.PlaybackSpeed = 0.5;
            PlaybackSpeed100Button.Checked += (_, _) => settings.PlaybackSpeed = 1;
            PlaybackSpeed200Button.Checked += (_, _) => settings.PlaybackSpeed = 2;
            PlaybackSpeed400Button.Checked += (_, _) => settings.PlaybackSpeed = 4;
            PlaybackSpeed800Button.Checked += (_, _) => settings.PlaybackSpeed = 8;
        }
    }
}
