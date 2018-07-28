using System;
using System.Runtime.InteropServices;

namespace KeyboardMonitor.Win32
{
    public static class Kernel32
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();

        [DllImport("kernel32.dll")]
        public static extern uint FormatMessage(uint dwFlags, IntPtr lpSource, uint dwMessageId, uint dwLanguageId, IntPtr lpBuffer, uint nSize, IntPtr Arguments);

        [DllImport("psapi.dll", SetLastError = true)]
        public static extern bool EnumProcessModulesEx(IntPtr hProcess, IntPtr lphModule, uint cb, out uint lpcbNeeded, ModuleFilerFlags filterFlag);

        [DllImport("psapi.dll", SetLastError = true)]
        public static extern uint GetModuleBaseName(IntPtr hProcess, IntPtr hModule, IntPtr lpBaseName, uint nSize);

        [DllImport("psapi.dll", EntryPoint = "GetModuleFileNameExW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint GetModuleFileName(IntPtr hProcess, IntPtr hModule, IntPtr lpFilename, uint nSize);

        public const string Dll = "kernel32.dll";

        [DllImport(Dll, SetLastError = true)]
        public static extern IntPtr OpenProcess(ProcessAccessRights desiredAccess, bool inheritHandle, uint processId);

        [DllImport(Dll, SetLastError = true)]
        public static extern bool CloseHandle(IntPtr handle);

        [DllImport(Dll, EntryPoint = "LoadLibraryExW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, LoadLibraryFlags dwFlags);

    }

    [Flags]
    public enum LoadLibraryFlags : uint
    {
        /// <summary>
        /// If this value is used, and the executable module is a DLL, the system does not call DllMain for process and thread initialization and termination. Also, the system does not load additional executable modules that are referenced by the specified module.
        /// 
        /// Note  Do not use this value; it is provided only for backward compatibility. If you are planning to access only data or resources in the DLL, use LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE or LOAD_LIBRARY_AS_IMAGE_RESOURCE or both. Otherwise, load the library as a DLL or executable module using the LoadLibrary function.
        /// </summary>
        DONT_RESOLVE_DLL_REFERENCES = 0x00000001,

    }

    [Flags]
    public enum ModuleFilerFlags : uint
    {
        /// <summary>
        /// List the 32-bit modules.
        /// </summary>
        LIST_MODULES_32BIT = 0x01,

        /// <summary>
        /// List the 64-bit modules.
        /// </summary>
        LIST_MODULES_64BIT = 0x02,

        /// <summary>
        /// List all modules.
        /// </summary>
        LIST_MODULES_ALL = 0x03,

        /// <summary>
        /// Use the default behavior.
        /// </summary>
        LIST_MODULES_DEFAULT = 0x0,

    }

    [Flags]
    public enum ProcessAccessRights : uint
    {
        /// <summary>
        /// Required to create a process.
        /// </summary>
        PROCESS_CREATE_PROCESS = 0x0080,

        /// <summary>
        /// Required to create a thread.
        /// </summary>
        PROCESS_CREATE_THREAD = 0x0002,

        /// <summary>
        /// Required to duplicate a handle using DuplicateHandle.
        /// </summary>
        PROCESS_DUP_HANDLE = 0x0040,

        /// <summary>
        /// Required to retrieve certain information about a process, such as its token, exit code, and priority class (see OpenProcessToken).
        /// </summary>
        PROCESS_QUERY_INFORMATION = 0x0400,

        /// <summary>
        /// Required to retrieve certain information about a process (see GetExitCodeProcess, GetPriorityClass, IsProcessInJob, QueryFullProcessImageName). A handle that has the PROCESS_QUERY_INFORMATION access right is automatically granted PROCESS_QUERY_LIMITED_INFORMATION.
        /// 
        /// Windows Server 2003 and Windows XP:  This access right is not supported.
        /// </summary>
        PROCESS_QUERY_LIMITED_INFORMATION = 0x1000,

        /// <summary>
        /// Required to set certain information about a process, such as its priority class (see SetPriorityClass).
        /// </summary>
        PROCESS_SET_INFORMATION = 0x0200,

        /// <summary>
        /// Required to set memory limits using SetProcessWorkingSetSize.
        /// </summary>
        PROCESS_SET_QUOTA = 0x0100,

        /// <summary>
        /// Required to suspend or resume a process.
        /// </summary>
        PROCESS_SUSPEND_RESUME = 0x0800,

        /// <summary>
        /// Required to terminate a process using TerminateProcess.
        /// </summary>
        PROCESS_TERMINATE = 0x0001,

        /// <summary>
        /// Required to perform an operation on the address space of a process (see VirtualProtectEx and WriteProcessMemory).
        /// </summary>
        PROCESS_VM_OPERATION = 0x0008,

        /// <summary>
        /// Required to read memory in a process using ReadProcessMemory.
        /// </summary>
        PROCESS_VM_READ = 0x0010,

        /// <summary>
        /// Required to write to memory in a process using WriteProcessMemory.
        /// </summary>
        PROCESS_VM_WRITE = 0x0020,

        /// <summary>
        /// Required to wait for the process to terminate using the wait functions.
        /// </summary>
        SYNCHRONIZE = 0x100000,

        /// <summary>
        /// All possible access rights for a process object.
        /// 
        /// Windows Server 2003 and Windows XP:  The size of the PROCESS_ALL_ACCESS flag increased on Windows Server 2008 and Windows Vista. If an application compiled for Windows Server 2008 and Windows Vista is run on Windows Server 2003 or Windows XP, the PROCESS_ALL_ACCESS flag is too large and the function specifying this flag fails with ERROR_ACCESS_DENIED. To avoid this problem, specify the minimum set of access rights required for the operation. If PROCESS_ALL_ACCESS must be used, set _WIN32_WINNT to the minimum operating system targeted by your application (for example, #define _WIN32_WINNT _WIN32_WINNT_WINXP). For more information, see Using the Windows Headers. 
        /// </summary>
        PROCESS_ALL_ACCESS = StandardAccessRights.STANDARD_RIGHTS_REQUIRED |
                             SYNCHRONIZE |
                             0xFFFF,

    }

    public enum StandardAccessRights : uint
    {
        /// <summary>
        /// Required to delete the object.
        /// </summary>
        DELETE = 0x00010000,

        /// <summary>
        /// Required to read information in the security descriptor for the object, not including the information in the SACL. To read or write the SACL, you must request the ACCESS_SYSTEM_SECURITY access right. For more information, see SACL Access Right.
        /// </summary>
        READ_CONTROL = 0x00020000,

        /// <summary>
        /// The right to use the object for synchronization. This enables a thread to wait until the object is in the signaled state.
        /// </summary>
        SYNCHRONIZE = 0x00100000,

        /// <summary>
        /// Required to modify the DACL in the security descriptor for the object.
        /// </summary>
        WRITE_DAC = 0x00040000,

        /// <summary>
        /// Required to change the owner in the security descriptor for the object.
        /// </summary>
        WRITE_OWNER = 0x00080000,

        /// <summary>
        /// Combines DELETE, READ_CONTROL, WRITE_DAC, and WRITE_OWNER access.
        /// </summary>
        STANDARD_RIGHTS_REQUIRED = 0x000F0000,

        /// <summary>
        /// Currently defined to equal READ_CONTROL.
        /// </summary>
        STANDARD_RIGHTS_READ = READ_CONTROL,

        /// <summary>
        /// Currently defined to equal READ_CONTROL.
        /// </summary>
        STANDARD_RIGHTS_WRITE = READ_CONTROL,

        /// <summary>
        /// Currently defined to equal READ_CONTROL.
        /// </summary>
        STANDARD_RIGHTS_EXECUTE = READ_CONTROL,

    }

}