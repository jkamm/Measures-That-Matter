using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using SD = System.Drawing;
using SWF = System.Windows.Forms;
using EF = Eto.Forms;

using Rhino;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.DocObjects;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

public class Script_Instance : GH_ScriptInstance
{
    // Listener for HID device
    private HIDListener _hidListener;

    // Latest measurement and timestamp
    private volatile string _latestValue = "";
    private DateTime _latestTimestamp = DateTime.MinValue;
    private readonly object _timestampLock = new object();

    // Lists to accumulate all measurements and timestamps
    private List<string> _allMeasurements = new List<string>();
    private List<DateTime> _allTimestamps = new List<DateTime>();
    private readonly object _allDataLock = new object();

    // Reference to the Grasshopper document
    private GH_Document ghDoc;

    private void RunScript(
	bool startListening,
	bool clearCache,
	ref object A,
	ref object B,
	ref object C,
	ref object D)
    {
        // Initialize GH_Document and subscribe to ObjectsDeleted event once
        if (ghDoc == null)
        {
            ghDoc = this.Component.OnPingDocument();
            if (ghDoc != null)
            {
                ghDoc.ObjectsDeleted += GhDoc_ObjectsDeleted;
            }
        }

        // Handle Clear Cache input
        if (clearCache)
        {
            lock (_allDataLock)
            {
                _allMeasurements.Clear();
                _allTimestamps.Clear();
            }

            // Note: Users need to manually reset the clearCache input to False after clearing the cache.
            // Grasshopper C# scripting components cannot set input values programmatically.
        }

        // Start or stop the HIDListener based on the startListening toggle
        if (startListening)
        {
            if (_hidListener == null)
            {
                _hidListener = new HIDListener();
                _hidListener.OnMeasurementReceived += OnMeasurementReceived;
                try
                {
                    _hidListener.Start();
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine($"Error starting HIDListener: {ex.Message}");
                }
            }
        }
        else
        {
            if (_hidListener != null)
            {
                try
                {
                    _hidListener.Stop();
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine($"Error stopping HIDListener: {ex.Message}");
                }
                _hidListener.OnMeasurementReceived -= OnMeasurementReceived;
                _hidListener = null;
            }
        }

        // Output the latest measurement and timestamp
        A = _latestValue;
        lock (_timestampLock)
        {
            B = _latestTimestamp;
        }

        // Output all accumulated measurements and timestamps
        lock (_allDataLock)
        {
            C = new List<string>(_allMeasurements);
            D = new List<DateTime>(_allTimestamps);
        }
    }

    private void OnMeasurementReceived(string value)
    {
        _latestValue = value;

        // Capture the timestamp
        lock (_timestampLock)
        {
            _latestTimestamp = DateTime.Now;
        }

        // Accumulate the measurement and timestamp
        lock (_allDataLock)
        {
            _allMeasurements.Add(value);
            _allTimestamps.Add(_latestTimestamp);
        }

        // Schedule the Grasshopper solution to update outputs
        ghDoc.ScheduleSolution(1, doc =>
        {
            this.Component.ExpireSolution(false);
        });
    }

    // Event handler for when objects are deleted from the document
    private void GhDoc_ObjectsDeleted(object sender, GH_DocObjectEventArgs e)
    {
        if (e.Objects.Contains(this.Component))
        {
            if (_hidListener != null)
            {
                try
                {
                    _hidListener.Stop();
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine($"Error stopping HIDListener during ObjectsDeleted: {ex.Message}");
                }
                _hidListener.OnMeasurementReceived -= OnMeasurementReceived;
                _hidListener = null;
            }
        }
    }
}

public class HIDListener
{
    private IntPtr _hookID = IntPtr.Zero;
    private LowLevelKeyboardProc _proc;
    private Thread _hookThread;
    private readonly ManualResetEvent _exitEvent = new ManualResetEvent(false);

    public delegate void MeasurementReceivedHandler(string value);
    public event MeasurementReceivedHandler OnMeasurementReceived;

    public void Start()
    {
        if (_hookThread != null && _hookThread.IsAlive)
        {
            // Already running
            return;
        }

        _proc = HookCallback;

        _hookThread = new Thread(() =>
        {
            try
            {
                _hookID = SetHook(_proc);

                // Run the message loop until _exitEvent is signaled
                while (!_exitEvent.WaitOne(0))
                {
                    // Pump messages
                    SWF.Application.DoEvents();
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"HIDListener Thread Exception: {ex.Message}");
            }
            finally
            {
                if (_hookID != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_hookID);
                    _hookID = IntPtr.Zero;
                }
            }
        });

        _hookThread.SetApartmentState(ApartmentState.STA);
        _hookThread.IsBackground = true;
        _hookThread.Start();
    }

    public void Stop()
    {
        if (_hookThread == null || !_hookThread.IsAlive)
        {
            return;
        }

        // Signal the thread to exit
        _exitEvent.Set();

        // Wait for the thread to finish
        if (!_hookThread.Join(1000)) // Wait up to 1 second
        {
            RhinoApp.WriteLine("HIDListener thread did not terminate in a timely manner.");
        }

        _hookThread = null;
        _exitEvent.Reset();
    }

    private IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
        using (var curModule = curProcess.MainModule)
        {
            IntPtr moduleHandle = GetModuleHandle(curModule.ModuleName);
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, moduleHandle, 0);
        }
    }

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;

    private StringBuilder _buffer = new StringBuilder();

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                KBDLLHOOKSTRUCT kbData = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                int vkCode = kbData.vkCode;
                int scanCode = kbData.scanCode;

                // Get the character
                char character = GetCharFromVkCode(vkCode, scanCode);

                // Log the vkCode, scanCode, and character for debugging
                // Uncomment the line below if you need to debug
                // Rhino.RhinoApp.WriteLine($"vkCode: {vkCode}, scanCode: {scanCode}, character: {character}");

                if (character != '\0')
                {
                    if (char.IsDigit(character) || character == '.' || char.IsLetter(character))
                    {
                        _buffer.Append(character);
                    }
                    else if (character == '\r' || character == '\n') // Enter key
                    {
                        string measurement = _buffer.ToString();
                        _buffer.Clear();

                        // Raise event with the measurement value
                        OnMeasurementReceived?.Invoke(measurement);
                    }
                    else
                    {
                        // Clear buffer if unexpected character is received
                        _buffer.Clear();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"HookCallback Exception: {ex.Message}");
        }

        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public int vkCode;
        public int scanCode;
        public int flags;
        public int time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll")]
    private static extern int ToUnicode(
        uint wVirtKey,
        uint wScanCode,
        byte[] lpKeyState,
        [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff,
        int cchBuff,
        uint wFlags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetKeyboardState(byte[] lpKeyState);

    private char GetCharFromVkCode(int vkCode, int scanCode)
    {
        try
        {
            StringBuilder buffer = new StringBuilder(2);
            byte[] keyState = new byte[256];

            // Get the current keyboard state
            if (!GetKeyboardState(keyState))
            {
                return '\0';
            }

            int result = ToUnicode(
                (uint)vkCode,
                (uint)scanCode,
                keyState,
                buffer,
                buffer.Capacity,
                0);

            if (result > 0)
            {
                return buffer[0];
            }
            else
            {
                return '\0';
            }
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"GetCharFromVkCode Exception: {ex.Message}");
            return '\0';
        }
    }

    // Windows API functions and constants

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook,
        LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
        IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
}
