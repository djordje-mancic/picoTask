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
using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace picoTask
{
    public partial class MainWindow : FluentWindow
    {
        App app = (App)Application.Current;

        public MainWindow()
        {
            InitializeComponent();

            // AlwaysOnTop setting
            Settings settings = app.Settings;
            this.Topmost = settings.AlwaysOnTop;
            settings.PropertyChanged += (object? _, PropertyChangedEventArgs e) =>
            {
                if (e.PropertyName == "AlwaysOnTop")
                {
                    this.Topmost = settings.AlwaysOnTop;
                }
            };

            // Macro events
            app.MacroLoaded += (_, _) =>
            {
                MacroCheck();

                Macro? macro = app.LoadedMacro;
                if (macro == null) return;

                PlayButton.Visibility = Visibility.Visible;
                StopButton.Visibility = Visibility.Collapsed;
                
                macro.PropertyChanged += (object? sender, PropertyChangedEventArgs e) =>
                {
                    if (e.PropertyName == "Playing")
                    {
                        if (macro.Playing)
                        {
                            PlayButton.Visibility = Visibility.Collapsed;
                            StopButton.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            PlayButton.Visibility = Visibility.Visible;
                            StopButton.Visibility = Visibility.Collapsed;
                        }
                    }
                    else if (e.PropertyName == "IsSaved" || e.PropertyName == "FilePath")
                    {
                        MacroCheck();
                    }
                };
            };

            //Recording started/ended events
            app.RecordingStarted += (_, _) =>
            {
                RecordButton.Visibility = Visibility.Collapsed;
                StopRecordingButton.Visibility = Visibility.Visible;
                MacroCheck();
            };

            app.RecordingEnded += (_, _) =>
            {
                RecordButton.Visibility = Visibility.Visible;
                StopRecordingButton.Visibility = Visibility.Collapsed;
                MacroCheck();
            };
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Fix for WPF UI library, can't be set before load (at design time)
            this.SizeToContent = SizeToContent.WidthAndHeight;

            ApplicationThemeManager.ApplySystemTheme();

            MacroCheck();
        }

        // Open button
        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            if (app.IsRecording || app.LoadedMacro?.Playing == true) return;

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Open prerecorded macro from file",
                Filter = "picoTask Macro (*.ptskm)|*.ptskm"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                app.OpenMacro(openFileDialog.FileName);
            }
        }

        // Save button
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (app.LoadedMacro == null || app.IsRecording) return;

            Macro macro = app.LoadedMacro;
            string fileName = Path.GetFileNameWithoutExtension(macro.FilePath);

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                AddExtension = true,
                Title = "Save macro to file",
                FileName = fileName,
                Filter = "picoTask Macro (*.ptskm)|*.ptskm"
            };

            string? parentDirectory = Path.GetDirectoryName(macro.FilePath);
            if (parentDirectory != null)
            {
                saveFileDialog.InitialDirectory = parentDirectory;
            }
            
            if (saveFileDialog.ShowDialog() == true)
            {
                app.SaveMacro(saveFileDialog.FileName);
            }
        }

        // Record button
        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            app.StartRecording();
        }

        // Stop recording button
        private void StopRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            app.StopRecording();
        }

        // Play macro button
        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            app.PlayMacro();
        }

        // Stop macro button
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            app.StopMacro();
        }

        // Settings button
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }

        // Performs checks on what to display on the label below buttons
        private void MacroCheck()
        {
            if (app.IsRecording)
            {
                MacroNameLabel.Text = "Recording macro...";
                MacroNameLabel.ToolTip = "Press the 'Stop recording' button to stop recording your macro.";
                PlayButton.IsEnabled = false;
                SaveButton.IsEnabled = false;
                OpenButton.IsEnabled = false;
            }
            else
            {
                OpenButton.IsEnabled = true;

                Macro? macro = app.LoadedMacro;
                if (macro == null)
                {
                    MacroNameLabel.Text = "No macro recorded";
                    MacroNameLabel.ToolTip = "Press the 'Record' button to start recording a macro, or press the 'Open' button to open a prerecorded one.";
                    PlayButton.IsEnabled = false;
                    SaveButton.IsEnabled = false;
                }
                else
                {
                    string labelText = Path.GetFileName(macro.FilePath);
                    string toolTipText = macro.FilePath;
                    if (!macro.IsSaved)
                    {
                        labelText += " (Unsaved)";
                        toolTipText += " (Press the 'Save' button to save your macro to a file)";
                    }
                    MacroNameLabel.Text = labelText;
                    MacroNameLabel.ToolTip = toolTipText;
                    PlayButton.IsEnabled = true;
                    SaveButton.IsEnabled = true;
                }
            } 
        }

        /* MouseEnter and MouseLeave events to make sure no inputs get
         * recorded while the mouse is over the picoTask window.
         */
        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            app.BlockInputRecords = true;
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            app.BlockInputRecords = false;
        }
    }
}