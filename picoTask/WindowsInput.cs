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
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace picoTask
{
    /// <summary>
    /// WindowsInput class handles keyboard/mouse input listening and invoking through
    /// the native Windows API and provides more traditional C# style events and methods
    /// for other classes to use.
    /// </summary>
    public class WindowsInput
    {
        #region Win32 native structs and methods
        [StructLayout(LayoutKind.Sequential)]
        struct WIN32_POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public ushort vkCode;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MSLLHOOKSTRUCT
        {
            public WIN32_POINT pt;
            public int mouseData;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int xPos;
            public int yPos;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct INPUT_DATA
        {
            [FieldOffset(0)]
            public MOUSEINPUT mouseInput;
            [FieldOffset(0)]
            public KEYBDINPUT keyboardInput;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public INPUT_DATA data;
        }

        [DllImport("User32.dll")]
        static extern IntPtr SetWindowsHookEx(int idHook, HookDelegate hookFunction, IntPtr moduleHandle, uint _);

        delegate IntPtr HookDelegate(int code, int wParam, IntPtr lParam);

        [DllImport("User32.dll")]
        static extern bool UnhookWindowsHookEx(IntPtr hookHandle);

        [DllImport("User32.dll")]
        static extern IntPtr CallNextHookEx(IntPtr _, int code, int wParam, IntPtr lParam);

        [DllImport("User32.dll")]
        static extern uint SendInput(uint arraySize, INPUT[] inputArray, int inputSize);

        [DllImport("User32.dll")]
        static extern int GetSystemMetrics(int nIndex);

        [DllImport("Kernel32.dll")]
        static extern IntPtr GetModuleHandle(string moduleName);

        [DllImport("Kernel32.dll")]
        static extern int GetLastError();
        #endregion

        public class KeyboardEventArgs : EventArgs
        {
            public Key Key { get; internal set; }
            public int ScanCode { get; internal set; }
            public bool IsDown { get; internal set; }

            public KeyboardEventArgs()
            {
                Key = Key.None;
                IsDown = false;
            }
        }

        public class MouseEventArgs : EventArgs
        {
            public MouseAction MouseClick { get; internal set; }
            public bool IsDown { get; internal set; }
            public Point Location { get; internal set; }

            public MouseEventArgs()
            {
                MouseClick = MouseAction.None;
                Location = new Point();
                IsDown = false;
            }
        }

        public static event EventHandler<KeyboardEventArgs>? KeyboardEvent;
        public static event EventHandler<MouseEventArgs>? MouseEvent;

        static IntPtr KeyboardHookCallback(int code, int keyAction, IntPtr lParam)
        {
            if (keyAction == 0x100 || keyAction == 0x101) // 0x100 - Key down, 0x101 - Key up
            {
                var eventArgs = new KeyboardEventArgs();
                KBDLLHOOKSTRUCT keyboardLLInfo = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                eventArgs.Key = KeyInterop.KeyFromVirtualKey(keyboardLLInfo.vkCode);
                eventArgs.ScanCode = keyboardLLInfo.scanCode;

                if (keyAction == 0x100) 
                {
                    eventArgs.IsDown = true;
                }

                KeyboardEvent?.Invoke(null, eventArgs);
            }

            return CallNextHookEx(IntPtr.Zero, code, keyAction, lParam);
        }

        static IntPtr MouseHookCallback(int code, int mouseAction, IntPtr lParam)
        {
            MSLLHOOKSTRUCT mouseLLInfo = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            Point locationPoint = new Point(mouseLLInfo.pt.x, mouseLLInfo.pt.y);
            MouseEventArgs eventArgs = new MouseEventArgs
            {
                Location = locationPoint
            };

            switch (mouseAction)
            {
                case 0x201: // Left click down
                    eventArgs.IsDown = true;
                    eventArgs.MouseClick = MouseAction.LeftClick;
                    MouseEvent?.Invoke(
                        null,
                        eventArgs
                    );
                    break;
                case 0x202: // Left click up
                    eventArgs.IsDown = false;
                    eventArgs.MouseClick = MouseAction.LeftClick;
                    MouseEvent?.Invoke(
                        null,
                        eventArgs
                    );
                    break;
                case 0x204: // Right click down
                    eventArgs.IsDown = true;
                    eventArgs.MouseClick = MouseAction.RightClick;
                    MouseEvent?.Invoke(
                        null,
                        eventArgs
                    );
                    break;
                case 0x205: // Right click up
                    eventArgs.IsDown = false;
                    eventArgs.MouseClick = MouseAction.RightClick;
                    MouseEvent?.Invoke(
                        null,
                        eventArgs
                    );
                    break;
                default:
                    break;
            }

            return CallNextHookEx(IntPtr.Zero, code, mouseAction, lParam);
        }

        static IntPtr keyboardHook = 0;
        static IntPtr mouseHook = 0;

        /// <summary>
        /// Starts listening to mouse & keyboard events
        /// </summary>
        /// <exception cref="Exception"></exception>
        public static void StartListening()
        {
            var process = Process.GetCurrentProcess();
            if (process.MainModule == null)
            {
                throw new Exception("Couldn't get module from process");
            }

            var moduleHandle = GetModuleHandle(process.MainModule.ModuleName);
            if (moduleHandle == 0)
            {
                throw new Exception(String.Format("Getting module handle failed - Error %d", GetLastError()));
            }

            keyboardHook = SetWindowsHookEx(
                13, // 13 - Low-level keyboard
                KeyboardHookCallback, 
                moduleHandle, 
                0
            );
            mouseHook = SetWindowsHookEx(
                14, // 14 - Low-level mouse
                MouseHookCallback,
                moduleHandle,
                0
            );

            if (mouseHook == 0 || keyboardHook == 0)
            {
                StopListening();
                throw new Exception(String.Format("Couldn't set required hooks - Error %d", GetLastError()));
            }
        }

        /// <summary>
        /// Stops listening to mouse & keyboard events
        /// </summary>
        public static void StopListening()
        {
            if (keyboardHook != 0)
            {
                UnhookWindowsHookEx(keyboardHook);
            }

            if (mouseHook != 0)
            {
                UnhookWindowsHookEx(mouseHook);
            }
        }

        /// <summary>
        /// Simulates input from a keyboard
        /// </summary>
        /// <param name="scanCode">
        /// Scancode of the key on the keyboard
        /// </param>
        /// <param name="isDown">
        /// Specifies if a key is pressed or released
        /// </param>
        /// <exception cref="Exception"></exception>
        public static void SimulateInput(int scanCode, bool isDown)
        {
            KEYBDINPUT keyboardInput = new KEYBDINPUT
            {
                wScan = (ushort)scanCode,
                dwFlags = 8
            };

            if (isDown)
            {
                Debug.WriteLine($"Simulating key {scanCode} press");
            }
            else
            {
                Debug.WriteLine($"Simulating key {scanCode} release");
                keyboardInput.dwFlags += 2;
            }

            INPUT input = new INPUT
            {
                type = 1,
                data = new INPUT_DATA {
                    keyboardInput = keyboardInput
                }
            };
            int inputSize = Marshal.SizeOf(typeof(INPUT));
            INPUT[] inputArray = { input };

            uint eventsInserted = SendInput((uint)inputArray.Length, inputArray, inputSize);
            if (eventsInserted == 0)
            {
                throw new Exception(String.Format("Couldn't send input - Error {0}", GetLastError()));
            }
        }

        /// <summary>
        /// Simulated click input from a mouse
        /// </summary>
        /// <param name="mouseAction">
        /// Specifies if it is the right or left click of a mouse
        /// </param>
        /// <param name="isDown">
        /// Specifies if a click is pressed down or released
        /// </param>
        /// <param name="location">
        /// Location of the mouse cursor
        /// </param>
        /// <exception cref="Exception"></exception>
        public static void SimulateInput(MouseAction mouseAction, bool isDown, Point location)
        {
            // Translate screen point to MOUSEINPUT point (0 is 0% and 65535 is 100% of width/height location)
            Size screenSize = GetDisplaySize();
            Point translatedLocation = new Point((location.X / screenSize.Width) * 65535, (location.Y / screenSize.Height) * 65535);

            // Create MOUSEINPUT object
            MOUSEINPUT mouseInput = new MOUSEINPUT
            {
                xPos = (int)translatedLocation.X,
                yPos = (int)translatedLocation.Y,
                dwFlags = 0x8001 // 0x8000 (MOUSEEVENTF_ABSOLUTE) + 0x0001 (MOUSEEVENTF_MOVE)
            };

            switch (mouseAction)
            {
                case MouseAction.LeftClick:
                    if (isDown)
                    {
                        mouseInput.dwFlags += 0x02;
                    }
                    else
                    {
                        mouseInput.dwFlags += 0x04;
                    }
                    break;
                case MouseAction.RightClick:
                    if (isDown)
                    {
                        mouseInput.dwFlags += 0x08;
                    }
                    else
                    {
                        mouseInput.dwFlags += 0x10;
                    }
                    break;
                default:
                    break;
            }

            if (isDown)
            {
                Debug.WriteLine($"Simulating mouse {mouseAction} press");
            }
            else
            {
                Debug.WriteLine($"Simulating mouse {mouseAction} release");
            }

            INPUT input = new INPUT
            {
                type = 0,
                data = new INPUT_DATA
                {
                    mouseInput = mouseInput
                }
            };
            int inputSize = Marshal.SizeOf(typeof(INPUT));
            INPUT[] inputArray = { input };

            uint eventsInserted = SendInput((uint)inputArray.Length, inputArray, inputSize);
            if (eventsInserted == 0)
            {
                throw new Exception(String.Format("Couldn't send input - Error {0}", GetLastError()));
            }
        }

        public static Size GetDisplaySize()
        {
            var displayWidth = GetSystemMetrics(78);
            var displayHeight = GetSystemMetrics(79);
            return new Size(displayWidth, displayHeight);
        }
    }
}
