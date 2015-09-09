using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace KeyboardMonitor.Gathering.FrameRate
{
    public class FrapsService
    {
        private const int ProcessPollTime = 1000;
        private IntPtr _sharedData;
        private readonly Timer _processWatcher;

        public FrapsService()
        {
            _processWatcher = new Timer(ProcessWatcher_Callback, null, 0, ProcessPollTime);
        }

        private void ProcessWatcher_Callback(object state)
        {
            var process = Process.GetProcessesByName("fraps").FirstOrDefault();
            if (process != null)
            {
                try
                {
                    _sharedData = GetSharedData(process);
                    if (_sharedData != IntPtr.Zero)
                    {
                        process.Exited += Process_Exited;
                        _processWatcher.Change(Timeout.Infinite, Timeout.Infinite);
                    }
                }
                catch (Exception)
                {
                    // Not the right Fraps
                }
            }
        }

        private IntPtr GetSharedData(Process process)
        {
            var module = process.Modules.Cast<ProcessModule>().First(m => string.Equals(m.ModuleName, "fraps32.dll", StringComparison.InvariantCultureIgnoreCase));

            var sharedDataFunctionPtr = Win32.Kernel32.GetProcAddress(module.BaseAddress, "FrapsSharedData");

            var sharedDataFunction = (GetSharedDataDelegate)Marshal.GetDelegateForFunctionPointer(sharedDataFunctionPtr, typeof(GetSharedDataDelegate));
            var sharedData = sharedDataFunction();

            Marshal.Release(sharedDataFunctionPtr);

            return sharedData;
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            Marshal.Release(_sharedData);
            _sharedData = IntPtr.Zero;

            var process = sender as Process;
            if (process != null)
            {
                process.Exited -= Process_Exited;
            }

            _processWatcher.Change(ProcessPollTime, ProcessPollTime);
        }

        public FrapsData GetFrapsData()
        {
            if (_sharedData != IntPtr.Zero)
            {
                return (FrapsData)Marshal.PtrToStructure(_sharedData, typeof(FrapsData));
            }
            return new FrapsData();
        }

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        protected delegate IntPtr GetSharedDataDelegate();

    }
}
