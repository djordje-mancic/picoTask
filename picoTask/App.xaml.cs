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
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using KeyboardEventArgs = picoTask.WindowsInput.KeyboardEventArgs;
using MessageBox = Wpf.Ui.Controls.MessageBox;
using MouseEventArgs = picoTask.WindowsInput.MouseEventArgs;

namespace picoTask
{
    public partial class App : Application
    {
        public bool IsRecording = false;
        public Macro? LoadedMacro = null;
        public bool BlockInputRecords = false;
        public Settings Settings;

        public event EventHandler? MacroLoaded;
        public event EventHandler? RecordingStarted;
        public event EventHandler? RecordingEnded;

        private Stopwatch recordingStopwatch = new Stopwatch();

        const string SETTINGS_FILENAME = "picoTask_Settings.json";

        public App()
        {
            InitializeComponent();

            // Load settings
            Settings? loadedSettings = null;
            if (Path.Exists(SETTINGS_FILENAME))
            {
                string fileContents = File.ReadAllText("picoTask_Settings.json");
                loadedSettings = JsonSerializer.Deserialize<Settings>(fileContents);
            }

            if (loadedSettings == null)
            {
                loadedSettings = new Settings();
            }

            this.Settings = loadedSettings;

            //Show MainWindow
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();

            // Attach hooks
            WindowsInput.StartListening();

            // Hotkeys
            WindowsInput.KeyboardEvent += (object? _, KeyboardEventArgs eventArgs) =>
            {
                if (eventArgs.IsDown)
                {
                    if (eventArgs.Key == Key.F8)
                    {
                        if (!IsRecording)
                        {
                            StartRecording();
                        }
                        else
                        {
                            StopRecording();
                        }
                    }
                    else if (eventArgs.Key == Key.F9)
                    {
                        if (LoadedMacro != null)
                        {
                            if (!LoadedMacro.Playing)
                            {
                                PlayMacro();
                            }
                            else
                            {
                                StopMacro();
                            }
                        }
                    }
                }
            };
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            // Save settings to file
            var serializerOptions = new JsonSerializerOptions();
            serializerOptions.WriteIndented = true;

            string jsonText = JsonSerializer.Serialize(
                this.Settings, 
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                }
            );
            File.WriteAllText(SETTINGS_FILENAME, jsonText);

            // Unattach hooks on exit
            WindowsInput.StopListening();
        }

        void WaitStepCheck()
        {
            if (recordingStopwatch.ElapsedMilliseconds > 50)
            {
                recordingStopwatch.Stop();
                int milliseconds = (int)recordingStopwatch.ElapsedMilliseconds;
                LoadedMacro?.AddStep(milliseconds);
                Debug.WriteLine($"Recording a wait of {milliseconds}ms");
                recordingStopwatch.Restart();
            }
        }

        void OnKeyboardEvent(object? _, KeyboardEventArgs eventArgs)
        {
            if (BlockInputRecords) return;
            if (eventArgs.Key == Key.F8 || eventArgs.Key == Key.F9) return;

            Debug.WriteLine($"Recording key {eventArgs.Key} {eventArgs.IsDown}");
            WaitStepCheck();
            LoadedMacro?.AddStep(eventArgs.ScanCode, eventArgs.IsDown);
        }

        void OnMouseEvent(object? _, MouseEventArgs eventArgs)
        {
            if (BlockInputRecords) return;

            Debug.WriteLine($"Recording mouse {eventArgs.MouseClick} {eventArgs.IsDown} at {eventArgs.Location}");
            WaitStepCheck();
            LoadedMacro?.AddStep(eventArgs.MouseClick, eventArgs.IsDown, eventArgs.Location);
        }

        public void StartRecording()
        {
            if (IsRecording || LoadedMacro?.Playing == true) return;
            IsRecording = true;

            LoadedMacro = new Macro();
            MacroLoaded?.Invoke(this, EventArgs.Empty);
            recordingStopwatch.Restart();

            WindowsInput.KeyboardEvent += OnKeyboardEvent;
            WindowsInput.MouseEvent += OnMouseEvent;
            RecordingStarted?.Invoke(this, EventArgs.Empty);

            Debug.WriteLine("Recording start");
        }

        public void StopRecording()
        {
            if (!IsRecording) return;
            IsRecording = false;

            WindowsInput.KeyboardEvent -= OnKeyboardEvent;
            WindowsInput.MouseEvent -= OnMouseEvent;

            WaitStepCheck();
            recordingStopwatch.Stop();
            RecordingEnded?.Invoke(this, EventArgs.Empty);

            Debug.WriteLine("Recording stopped");
        }

        public void PlayMacro()
        {
            if (IsRecording) return;
            LoadedMacro?.Play();
        }

        public void StopMacro()
        {
            if (IsRecording) return;
            LoadedMacro?.Stop();
        }

        public void SaveMacro(string path)
        {
            if (LoadedMacro == null || IsRecording) return;

            string jsonText = JsonSerializer.Serialize(
                LoadedMacro,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }
            );

            File.WriteAllText(path, jsonText);

            LoadedMacro.FilePath = path;
            LoadedMacro.IsSaved = true;
            Debug.WriteLine($"Macro saved to {path}");
        }

        public void OpenMacro(string path)
        {
            if (IsRecording || LoadedMacro?.Playing == true) return;

            string fileContents = File.ReadAllText(path);
            Macro? openedMacro = null;
            try
            {
                openedMacro = JsonSerializer.Deserialize<Macro>(fileContents);
            }
            catch
            {
                Debug.WriteLine("Exception thrown while loading macro");
            }

            if (openedMacro == null)
            {
                MessageBox messageBox = new MessageBox
                {
                    Title = "File corrupted",
                    Content = "The macro file you have tried to open has been corrupted.",
                    CloseButtonText = "OK",
                    IsPrimaryButtonEnabled = false,
                    IsSecondaryButtonEnabled = false,
                };
                messageBox.ShowDialogAsync();
                return;
            }

            openedMacro.FilePath = path;
            openedMacro.IsSaved = true;
            LoadedMacro = openedMacro;
            MacroLoaded?.Invoke(this, EventArgs.Empty);
            Debug.WriteLine($"Opened macro from {path}");
        }
    }
}
