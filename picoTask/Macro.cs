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
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using System.Text.Json.Serialization;

namespace picoTask
{
    public class Macro : INotifyPropertyChanged
    {
        #region MacroStep class and derived classes
        [JsonDerivedType(typeof(MouseStep), typeDiscriminator: "M")]
        [JsonDerivedType(typeof(KeyboardStep), typeDiscriminator: "K")]
        [JsonDerivedType(typeof(WaitStep), typeDiscriminator: "W")]
        public abstract class MacroStep
        {
            public abstract void PlayStep();
        }

        public class MouseStep : MacroStep
        {
            public MouseAction Action { get; set; }
            public bool IsDown { get; set; }
            public Point Location { get; set; }

            public MouseStep()
            {
                Action = MouseAction.None;
                IsDown = false;
                Location = new Point();
            }

            public MouseStep(MouseAction mouseAction, bool isDown, Point location)
            {
                Action = mouseAction;
                IsDown = isDown;
                Location = location;
            }

            public override void PlayStep()
            {
                Debug.WriteLine($"Setting {Action} mouse click to {IsDown} at location {Location}");
                WindowsInput.SimulateInput(Action, IsDown, Location);
            }
        }

        public class KeyboardStep : MacroStep
        {
            public Key Key { get; set; }
            public int ScanCode { get; set; }
            public bool IsDown { get; set; }

            public KeyboardStep()
            {
                Key = Key.None;
                ScanCode = 0;
                IsDown = false;
            }

            public KeyboardStep(Key key, bool isDown)
            {
                Key = key;
                ScanCode = 0;
                IsDown = isDown;
            }

            public KeyboardStep(int scanCode, bool isDown)
            {
                Key = Key.None;
                ScanCode = scanCode;
                IsDown = isDown;
            }

            public override void PlayStep()
            {
                Debug.WriteLine($"Setting {Key} key to {IsDown}");
                WindowsInput.SimulateInput(ScanCode, IsDown);
            }
        }

        public class WaitStep : MacroStep
        {
            public int WaitTime { get; set; }

            public WaitStep()
            {
                WaitTime = 0;
            }

            public WaitStep(int waitTime)
            {
                WaitTime = waitTime;
            }

            public override void PlayStep()
            {
                PlayStep(1);
            }

            public void PlayStep(double playbackSpeed)
            {
                Debug.WriteLine($"Waiting for {WaitTime}ms at {playbackSpeed}x playback speed");
                Thread.Sleep((int)(this.WaitTime / playbackSpeed));
            }
        }
        #endregion

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        CancellationTokenSource? playbackCTS = null;
        bool _playing = false;
        string _filepath = "Untitled.ptskm";
        bool _issaved = false;

        public List<MacroStep> Steps { get; set; }

        [JsonIgnore]
        public bool Playing
        {
            get { return _playing; }
            set
            {
                _playing = value;
                NotifyPropertyChanged();
            }
        }

        [JsonIgnore]
        public string FilePath
        {
            get { return _filepath; }
            set
            {
                _filepath = value;
                NotifyPropertyChanged();
            }
        }

        [JsonIgnore]
        public bool IsSaved
        {
            get { return _issaved; }
            set
            {
                _issaved = value;
                NotifyPropertyChanged();
            }
        }

        #region Constructors
        public Macro()
        {
            Steps = new();
        }
        #endregion

        #region Methods
        public void AddStep(int waitTime)
        {
            Steps.Add(new WaitStep(waitTime));
        }

        public void AddStep(MouseAction mouseAction, bool isDown, Point location)
        {
            Steps.Add(new MouseStep(mouseAction, isDown, location));
        }

        public void AddStep(Key key, bool isDown)
        {
            Steps.Add(new KeyboardStep(key, isDown));
        }

        public void AddStep(int scanCode, bool isDown)
        {
            Steps.Add(new KeyboardStep(scanCode, isDown));
        }

        public void Play()
        {
            if (Playing) return;
            Playing = true;

            Settings settings = ((App) Application.Current).Settings;

            CancellationTokenSource cts = new CancellationTokenSource();
            playbackCTS = cts;

            Dispatcher outsideDispatcher = Dispatcher.CurrentDispatcher;

            Task.Run(() =>
            {
                try
                {
                    do
                    {
                        foreach (MacroStep step in Steps)
                        {
                            cts.Token.ThrowIfCancellationRequested();
                            if (step is WaitStep)
                            {
                                ((WaitStep)step).PlayStep(settings.PlaybackSpeed);
                            }
                            else
                            {
                                step.PlayStep();
                            }
                        }
                        cts.Token.ThrowIfCancellationRequested();
                    }
                    while (settings.RepeatPlayback);

                    outsideDispatcher.Invoke(() =>
                    {
                        Playing = false;
                    });
                }
                catch { }
            }, cts.Token);
        }

        public void Stop()
        {
            if (!Playing) return;
            Playing = false;
            if (playbackCTS != null) 
            { 
                playbackCTS.Cancel(); 
            }
        }
        #endregion
    }
}
