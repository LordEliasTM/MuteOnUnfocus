using CSCore.CoreAudioAPI;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class MixerTest
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();


    //Process executable name without .exe
    static readonly string name = "VALORANT-Win64-Shipping";
    static bool applicationMuted = false;

    static void Main() {
        Console.SetWindowSize(40, 2);
        Console.SetBufferSize(40, 2);
        AppDomain.CurrentDomain.ProcessExit += new EventHandler(ProcessExit);

    Start:
        Console.SetCursorPosition(0, 0);
        Console.Write("Waiting for process...");
        while (true) {
            if (!ProcessExists(name)) continue;
            break;
        }

        while (true) {
            if (!ProcessExists(name)) goto Start;

            bool isFocused = IsWindowFocused(name);

            if (isFocused && applicationMuted) {
                Mute(false, name);
                applicationMuted = false;
                Console.SetCursorPosition(0, 0);
                Console.WriteLine("Unmuted " + name + "    ");
            }
            else if (!isFocused && !applicationMuted) {
                Mute(true, name);
                applicationMuted = true;
                Console.SetCursorPosition(0, 0);
                Console.WriteLine("Muted " + name + "    ");
            }
        }
    }

    // Thanks to https://stackoverflow.com/a/70367167
    // b: true  -> muted
    // b: false -> unmuted
    // name: Process name
    static void Mute(bool b, string name) {
        foreach (AudioSessionManager2 sessionManager in GetDefaultAudioSessionManager2(DataFlow.Render)) {
            using (sessionManager) {
                using var sessionEnumerator = sessionManager.GetSessionEnumerator();
                foreach (var session in sessionEnumerator) {
                    using var simpleVolume = session.QueryInterface<SimpleAudioVolume>();
                    using var sessionControl = session.QueryInterface<AudioSessionControl2>();
                    //Console.WriteLine((sessionControl.Process.ProcessName, sessionControl.SessionIdentifier));
                    if (Process.GetProcessById(sessionControl.ProcessID).ProcessName.Equals(name)) {
                        simpleVolume.IsMuted = b;
                    }
                }
            }
        }
    }

    private static IEnumerable<AudioSessionManager2> GetDefaultAudioSessionManager2(DataFlow dataFlow)
    {
        using var enumerator = new MMDeviceEnumerator();
        using var devices = enumerator.EnumAudioEndpoints(dataFlow, DeviceState.Active);
        foreach (var device in devices) {
            //Console.WriteLine("Device: " + device.FriendlyName);
            var sessionManager = AudioSessionManager2.FromMMDevice(device);
            yield return sessionManager;
        }
    }

    // returns the window handle give a process name
    // name: Process name
    static IntPtr HandleByProcessName(String name) {
        Process[] processes = Process.GetProcessesByName(name);

        foreach (Process p in processes) {
            IntPtr windowHandle = p.MainWindowHandle;
            if (windowHandle != IntPtr.Zero)
                return windowHandle;
        }

        return IntPtr.Zero;
    }

    // name: Process name
    static bool IsWindowFocused(String name) {
        IntPtr gameWindow = HandleByProcessName(name);
        IntPtr foregroundWindow = GetForegroundWindow();

        return gameWindow == foregroundWindow;
    }

    // name: Process name
    static bool ProcessExists(String name) {
        Process[] processes = Process.GetProcessesByName(name);
        return processes.Length != 0;
    }

    // Unmute the process when this application exits
    static void ProcessExit(object? sender, EventArgs e) {
        Mute(false, name);
    }
}