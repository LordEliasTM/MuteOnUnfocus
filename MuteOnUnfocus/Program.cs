using CSCore.CoreAudioAPI;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class MixerTest
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();


    //Process executable name without .exe
    static String name = "VALORANT-Win64-Shipping";
    static bool applicationMuted = false;

    static void Main(string[] args)
    {
        AppDomain.CurrentDomain.ProcessExit += new EventHandler(ProcessExit);

    Start:
        Console.SetCursorPosition(0, 0);
        Console.Write("Waiting for process...");
        while (true)
        {
            if (!processExists(name)) continue;
            break;
        }

        while (true)
        {
            if (!processExists(name)) goto Start;

            bool isFocused = isWindowFocused(name);

            if (isFocused && applicationMuted)
            {
                mute(false, name);
                applicationMuted = false;
                Console.SetCursorPosition(0, 0);
                Console.WriteLine("Unmuted " + name + "                       ");
            }
            else if (!isFocused && !applicationMuted)
            {
                mute(true, name);
                applicationMuted = true;
                Console.SetCursorPosition(0, 0);
                Console.WriteLine("Muted " + name + "                       ");
            }
        }
    }

    // Thanks to https://stackoverflow.com/a/70367167
    // b: true  -> muted
    // b: false -> unmuted
    // name: Process name
    static void mute(bool b, String name)
    {
        foreach (AudioSessionManager2 sessionManager in GetDefaultAudioSessionManager2(DataFlow.Render))
        {
            using (sessionManager)
            {
                using (var sessionEnumerator = sessionManager.GetSessionEnumerator())
                {
                    foreach (var session in sessionEnumerator)
                    {
                        using var simpleVolume = session.QueryInterface<SimpleAudioVolume>();
                        using var sessionControl = session.QueryInterface<AudioSessionControl2>();
                        //Console.WriteLine((sessionControl.Process.ProcessName, sessionControl.SessionIdentifier));
                        if (Process.GetProcessById(sessionControl.ProcessID).ProcessName.Equals(name))
                        {
                            simpleVolume.IsMuted = b;
                        }
                    }
                }
            }
        }
    }
    private static IEnumerable<AudioSessionManager2> GetDefaultAudioSessionManager2(DataFlow dataFlow)
    {
        using var enumerator = new MMDeviceEnumerator();
        using var devices = enumerator.EnumAudioEndpoints(dataFlow, DeviceState.Active);
        foreach (var device in devices)
        {
            //Console.WriteLine("Device: " + device.FriendlyName);
            var sessionManager = AudioSessionManager2.FromMMDevice(device);
            yield return sessionManager;
        }
    }

    // returns the window handle give a process name
    // name: Process name
    static IntPtr handleByProcessName(String name)
    {
        Process[] processes = Process.GetProcessesByName(name);

        foreach (Process p in processes)
        {
            IntPtr windowHandle = p.MainWindowHandle;
            if (windowHandle != IntPtr.Zero)
                return windowHandle;
        }

        return IntPtr.Zero;
    }

    // name: Process name
    static bool isWindowFocused(String name)
    {
        IntPtr gameWindow = handleByProcessName(name);
        IntPtr foregroundWindow = GetForegroundWindow();

        return gameWindow == foregroundWindow;
    }

    // name Process name
    static bool processExists(String name)
    {
        Process[] processes = Process.GetProcessesByName(name);
        return processes.Length != 0;
    }

    // Unmute the process when this application exits
    static void ProcessExit(object sender, EventArgs e)
    {
        mute(false, name);
    }
}