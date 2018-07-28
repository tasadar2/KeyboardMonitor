using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using KeyboardMonitor.Win32;

namespace KeyboardMonitor.Gathering.FrameRate
{
    public class FrapsService
    {
        private const int ProcessPollTime = 1000;
        private IntPtr _sharedData;
        private readonly Timer _processWatcher;

        public FrapsService()
        {
            //Process.EnterDebugMode();
            _processWatcher = new Timer(ProcessWatcher_Callback, null, 0, ProcessPollTime);
            //ProcessWatcher_Callback(null);
        }

        private void ProcessWatcher_Callback(object state)
        {
            var process = Process.GetProcessesByName("fraps").FirstOrDefault();
            if (process != null)
            {
                try
                {
                    LoggerInstance.LogWriter.Debug($"Process {process.ProcessName}");
                    _sharedData = GetSharedData(process);
                    if (_sharedData != IntPtr.Zero)
                    {
                        process.Exited += Process_Exited;
                        _processWatcher.Change(Timeout.Infinite, Timeout.Infinite);
                    }
                }
                catch (Exception ex)
                {
                    LoggerInstance.LogWriter.Error("Failed to get chared data.", ex);
                    // Not the right Fraps
                }
            }
        }

        private IntPtr GetSharedData(Process process)
        {
            var sharedData = IntPtr.Zero;

            //var processHandle = Kernel32.OpenProcess(ProcessAccessRights.PROCESS_ALL_ACCESS, true, (uint)process.Id);

            //uint modulesSize = 0x200;
            //var modulesHandle = Marshal.AllocHGlobal((int)modulesSize);
            //var successBool = Kernel32.EnumProcessModulesEx(processHandle, modulesHandle, modulesSize, out modulesSize, ModuleFilerFlags.LIST_MODULES_32BIT);
            //if (!successBool)
            //{
            //    LogLastError();
            //}

            //var sizeOfIntPtr = Marshal.SizeOf(typeof(IntPtr));
            //var fraps32DllModuleHandle = IntPtr.Zero;
            //var offset = 0;
            //while (fraps32DllModuleHandle == IntPtr.Zero && offset < modulesSize)
            //{
            //    var moduleHandle = new IntPtr(Marshal.ReadInt32(modulesHandle, offset));

            //    uint size = 1024;
            //    var moduleNameHandle = Marshal.AllocHGlobal((int)size);
            //    size = Kernel32.GetModuleBaseName(processHandle, moduleHandle, moduleNameHandle, size);
            //    var moduleName = Marshal.PtrToStringAnsi(moduleNameHandle, (int)size);
            //    Marshal.FreeHGlobal(moduleNameHandle);

            //    if (string.Equals(moduleName, "fraps32.dll", StringComparison.OrdinalIgnoreCase))
            //    {
            //        fraps32DllModuleHandle = moduleHandle;
            //    }

            //    offset += sizeOfIntPtr;
            //}

            //Marshal.FreeHGlobal(modulesHandle);

            //if (fraps32DllModuleHandle != IntPtr.Zero)
            //{
            //    uint size = 0x1000;
            //    var moduleFileNameHandle = Marshal.AllocHGlobal((int)size);
            //    size = Kernel32.GetModuleFileName(processHandle, fraps32DllModuleHandle, moduleFileNameHandle, size);
            //    var moduleFileName = Marshal.PtrToStringUni(moduleFileNameHandle, (int)size);
            //    Marshal.FreeHGlobal(moduleFileNameHandle);

            //    var libraryHandle = Kernel32.LoadLibraryEx(moduleFileName, IntPtr.Zero, 0x00000001);

            //    var sharedDataFunctionPtr = Kernel32.GetProcAddress(libraryHandle, "FrapsSharedData");
            //    LoggerInstance.LogWriter.Debug($"Data Function {sharedDataFunctionPtr}");

            //    if (sharedDataFunctionPtr != IntPtr.Zero)
            //    {
            //        var sharedDataFunction = (GetSharedDataDelegate)Marshal.GetDelegateForFunctionPointer(sharedDataFunctionPtr, typeof(GetSharedDataDelegate));
            //        sharedData = sharedDataFunction();
            //        LoggerInstance.LogWriter.Debug($"Shared Data {sharedData}");

            //        Marshal.Release(sharedDataFunctionPtr);
            //    }
            //    else
            //    {
            //        LogLastError();
            //    }
            //}

            //Kernel32.CloseHandle(processHandle);


            LoggerInstance.LogWriter.Debug($"module count: {process.Modules.Count}");
            var module = process.Modules.Cast<ProcessModule>().FirstOrDefault(m => string.Equals(m.ModuleName, "fraps32.dll", StringComparison.InvariantCultureIgnoreCase));

            if (module != null)
            {
                var libraryHandle = Kernel32.LoadLibraryEx(module.FileName, IntPtr.Zero, LoadLibraryFlags.DONT_RESOLVE_DLL_REFERENCES);
                if (libraryHandle != IntPtr.Zero)
                {
                    var sharedDataFunctionPtr = Kernel32.GetProcAddress(libraryHandle, "FrapsSharedData");
                    LoggerInstance.LogWriter.Debug($"Data Function {sharedDataFunctionPtr}");

                    if (sharedDataFunctionPtr != IntPtr.Zero)
                    {
                        var sharedDataFunction = (GetSharedDataDelegate)Marshal.GetDelegateForFunctionPointer(sharedDataFunctionPtr, typeof(GetSharedDataDelegate));
                        sharedData = sharedDataFunction();
                        LoggerInstance.LogWriter.Debug($"Shared Data {sharedData}");

                        Marshal.Release(sharedDataFunctionPtr);
                    }
                }

                if (sharedData == IntPtr.Zero)
                {
                    LogLastError();
                }
            }

            return sharedData;
        }

        private static void LogLastError()
        {
            var lasterror = Kernel32.GetLastError();
            var errorTextHandle = Marshal.AllocHGlobal(1024);
            uint size = 1024;
            size = Kernel32.FormatMessage(0x1000, IntPtr.Zero, lasterror, MAKELANGID(0x09, 0x01), errorTextHandle, size, IntPtr.Zero);
            var errorText = Marshal.PtrToStringAnsi(errorTextHandle, (int)size);
            Marshal.FreeHGlobal(errorTextHandle);

            LoggerInstance.LogWriter.Debug($"Last Error {errorText}");
        }

        public static uint MAKELANGID(int primary, int sub)
        {
            return (uint)(((ushort)sub << 10) | (ushort)primary);
        }

        public static uint PRIMARYLANGID(uint lcid)
        {
            return (uint)((ushort)lcid & 0x3ff);
        }

        public static uint SUBLANGID(uint lcid)
        {
            return (uint)((ushort)lcid >> 10);
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            Marshal.Release(_sharedData);
            _sharedData = IntPtr.Zero;

            if (sender is Process process)
            {
                process.Exited -= Process_Exited;
            }

            _processWatcher.Change(ProcessPollTime, ProcessPollTime);
        }

        public FrapsData GetFrapsData()
        {
            var data = new FrapsData();

            if (_sharedData != IntPtr.Zero)
            {
                data = (FrapsData)Marshal.PtrToStructure(_sharedData, typeof(FrapsData));
            }

            return data;
        }

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        protected delegate IntPtr GetSharedDataDelegate();
    }
}