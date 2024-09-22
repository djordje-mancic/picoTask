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
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace picoTask
{
    public class Settings : INotifyPropertyChanged
    {
        double _playbackSpeed;
        bool _repeatPlayback;
        bool _alwaysOnTop;

        public double PlaybackSpeed {
            get
            {
                return _playbackSpeed;
            }
            set
            {
                _playbackSpeed = value;
                NotifyPropertyChanged();
            }
        }

        public bool RepeatPlayback
        {
            get
            {
                return _repeatPlayback;
            }
            set
            {
                _repeatPlayback = value;
                NotifyPropertyChanged();
            }
        }

        public bool AlwaysOnTop
        {
            get
            {
                return _alwaysOnTop;
            }
            set
            {
                _alwaysOnTop = value;
                NotifyPropertyChanged();
            }
        }

        public Settings() { 
            PlaybackSpeed = 1;
            RepeatPlayback = false;
            AlwaysOnTop = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
