using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using Topshelf;

namespace KeyboardMonitor
{
    public static class Program
    {
        static readonly ManualResetEventSlim QuitEvent = new ManualResetEventSlim(false);

        public static void Main(string[] args)
        {
            LoggerInstance.LogWriter.Info($"Args: {(args == null ? "empty" : string.Join(" ", args))}");

            if (args == null || !args.Any())
                //if (args != null && args.Any() && args[0] == "run")
            {
                var service = new KeyboardMonitorService();
                service.Start();
                QuitEvent.Wait();
            }
            else
            {
                HostFactory.Run(x =>
                {
                    x.Service<KeyboardMonitorService>(s =>
                    {
                        s.ConstructUsing(name => new KeyboardMonitorService());
                        s.WhenStarted(tc => tc.Start());
                        s.WhenStopped(tc => tc.Stop());
                    });
                    x.RunAsLocalSystem();

                    x.SetDescription("Monitors computer activities to report to keyboard.");
                    x.SetDisplayName("Keyboard Monitor");
                    x.SetServiceName("KeyboardMonitor");
                });
            }
        }
    }

    public class ProcessService
    {
        private Process process;

        public void Start()
        {
            try
            {
                //process = Process.Start(new ProcessStartInfo
                //{
                //    FileName = Assembly.GetEntryAssembly().Location,
                //    Arguments = "run",
                //    UseShellExecute = false,
                //    //UserName = "josh",
                //    //Password = CreateSecureString("Kerm&pop45"),
                //});

                LoggerInstance.LogWriter.Info("Starting process...");
                if (StartProcessAndBypassUac(Assembly.GetEntryAssembly().Location, out var processInfo))
                {
                    LoggerInstance.LogWriter.Info($"Process started: {processInfo.dwProcessId}");
                    process = Process.GetProcessById((int)processInfo.dwProcessId);
                }
                //else
                //{
                //    LoggerInstance.LogWriter.Info("Waiting...");
                //    Thread.Sleep(20000);
                //    LoggerInstance.LogWriter.Info("Starting process again...");
                //    if (StartProcessAndBypassUac(Assembly.GetEntryAssembly().Location, out var processInfo2))
                //    {
                //        LoggerInstance.LogWriter.Info($"Process started: {processInfo.dwProcessId}");
                //        process = Process.GetProcessById((int)processInfo.dwProcessId);
                //    }
                //}
            }
            catch (Exception ex)
            {
                LoggerInstance.LogWriter.Error("Failed to start service.", ex);
            }
        }

        private static SecureString CreateSecureString(string value)
        {
            var secure = new SecureString();
            foreach (var character in value)
            {
                secure.AppendChar(character);
            }
            return secure;
        }

        public void Stop()
        {
            LoggerInstance.LogWriter.Info("Ending process...");
            process?.Kill();
            LoggerInstance.LogWriter.Info("Process ended.");
        }


        public const int TokenDuplicate = 0x0002;
        public const uint MaximumAllowed = 0x2000000;
        public const int CreateNewConsole = 0x00000010;

        public const int IdlePriorityClass = 0x40;
        public const int NormalPriorityClass = 0x20;
        public const int HighPriorityClass = 0x80;
        public const int RealtimePriorityClass = 0x100;
        const Int32 TOKEN_ADJUST_SESSIONID = 0x0100;

        private enum TokenType
        {
            TokenPrimary = 1,
            TokenImpersonation = 2
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SecurityAttributes
        {
            public int Length;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Startupinfo
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public uint dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ProcessInformation
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TOKEN_GROUPS
        {
            public uint GroupCount;
            [MarshalAs(UnmanagedType.ByValArray)]
            public SID_AND_ATTRIBUTES[] Groups;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SID_AND_ATTRIBUTES
        {
            public IntPtr Sid;
            public uint Attributes;
        }

        private enum SecurityImpersonationLevel
        {
            SecurityAnonymous = 0,
            SecurityIdentification = 1,
            SecurityImpersonation = 2,
            SecurityDelegation = 3
        }

        public enum TOKEN_INFORMATION_CLASS
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin,
            TokenElevationType,
            TokenLinkedToken,
            TokenElevation,
            TokenHasRestrictions,
            TokenAccessInformation,
            TokenVirtualizationAllowed,
            TokenVirtualizationEnabled,
            TokenIntegrityLevel,
            TokenUIAccess,
            TokenMandatoryPolicy,
            TokenLogonSid,
            TokenIsAppContainer,
            TokenCapabilities,
            TokenAppContainerSid,
            TokenAppContainerNumber,
            TokenUserClaimAttributes,
            TokenDeviceClaimAttributes,
            TokenRestrictedUserClaimAttributes,
            TokenRestrictedDeviceClaimAttributes,
            TokenDeviceGroups,
            TokenRestrictedDeviceGroups,
            TokenSecurityAttributes,
            TokenIsRestricted,
            MaxTokenInfoClass
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hSnapshot);

        [DllImport("kernel32.dll")]
        private static extern uint WTSGetActiveConsoleSessionId();

        [DllImport("advapi32.dll", EntryPoint = "CreateProcessAsUser", SetLastError = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern bool CreateProcessAsUser(IntPtr hToken,
                                                      string lpApplicationName,
                                                      string lpCommandLine,
                                                      IntPtr lpProcessAttributes,
                                                      IntPtr lpThreadAttributes,
                                                      bool bInheritHandle,
                                                      int dwCreationFlags,
                                                      IntPtr lpEnvironment,
                                                      string lpCurrentDirectory,
                                                      ref Startupinfo lpStartupInfo,
                                                      out ProcessInformation lpProcessInformation);

        [DllImport("advapi32.dll", EntryPoint = "CreateProcessWithLogonW", SetLastError = true, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern bool CreateProcessWithLogonW(string lpUsername,
                                                          string lpDomain,
                                                          string lpPassword,
                                                          int dwLogonFlags,
                                                          string lpApplicationName,
                                                          string lpCommandLine,
                                                          int dwCreationFlags,
                                                          IntPtr lpEnvironment,
                                                          string lpCurrentDirectory,
                                                          ref Startupinfo lpStartupInfo,
                                                          out ProcessInformation lpProcessInformation);

        [DllImport("kernel32.dll")]
        private static extern bool ProcessIdToSessionId(uint dwProcessId, ref uint pSessionId);

        [DllImport("advapi32.dll", EntryPoint = "DuplicateTokenEx")]
        public static extern bool DuplicateTokenEx(IntPtr existingTokenHandle,
                                                   uint dwDesiredAccess,
                                                   ref SecurityAttributes lpThreadAttributes,
                                                   int tokenType,
                                                   int impersonationLevel,
                                                   ref IntPtr duplicateTokenHandle);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern Boolean SetTokenInformation(IntPtr TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, ref UInt32 TokenInformation, UInt32 TokenInformationLength);

        [DllImport("advapi32", SetLastError = true)]
        [SuppressUnmanagedCodeSecurity]
        private static extern bool OpenProcessToken(IntPtr processHandle, int desiredAccess, ref IntPtr tokenHandle);

        [DllImport("advapi32", SetLastError = true)]
        private static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword, int dwLogonType, int dwLogonProvider, ref IntPtr phToken);

        [DllImport("Wtsapi32.dll", SetLastError = true)]
        private static extern bool WTSQueryUserToken(uint SessionId, out IntPtr phToken);

        [DllImport("advapi32", SetLastError = true)]
        private static extern bool AdjustTokenGroups(IntPtr TokenHandle, bool ResetToDefault, IntPtr NewState, int BufferLength, IntPtr PreviousState, out int ReturnLength);

        [DllImport("advapi32", SetLastError = true)]
        private static extern bool ConvertStringSidToSid(string StringSid, out IntPtr Sid);

        public static bool StartProcessAndBypassUac(string applicationName, out ProcessInformation procInfo)
        {
            //uint winlogonPid = 0;
            procInfo = new ProcessInformation();

            //// obtain the currently active session id; every logged on user in the system has a unique session id
            var dwSessionId = WTSGetActiveConsoleSessionId();
            LoggerInstance.LogWriter.Debug($"Session: {dwSessionId}");
            var userToken = IntPtr.Zero;
            if (!WTSQueryUserToken(dwSessionId, out userToken))
            {
                var lastError = Marshal.GetLastWin32Error();
                var exception = new Win32Exception(lastError);
                LoggerInstance.LogWriter.Error("WTSQueryUserToken", exception);
                return false;
            }

            if (!ConvertStringSidToSid("S-1-5-32-544", out var administratosSid))
            {
                var lastError = Marshal.GetLastWin32Error();
                var exception = new Win32Exception(lastError);
                LoggerInstance.LogWriter.Error("ConvertStringSidToSid", exception);
                return false;
            }

            var groups = new TOKEN_GROUPS
            {
                GroupCount = 1,
                Groups = new[]
                {
                    new SID_AND_ATTRIBUTES
                    {
                        Sid = administratosSid,
                        Attributes = 4
                    }
                }
            };

            var groupsPtr = Marshal.AllocHGlobal(Marshal.SizeOf(groups));
            Marshal.StructureToPtr(groups, groupsPtr, false);

            if (!AdjustTokenGroups(userToken, false, groupsPtr, 0, IntPtr.Zero, out _))
            {
                var lastError = Marshal.GetLastWin32Error();
                var exception = new Win32Exception(lastError);
                LoggerInstance.LogWriter.Error("AdjustTokenGroups", exception);
                return false;
            }

            //// obtain the process id of the winlogon process that is running within the currently active session
            //var processes = Process.GetProcessesByName("winlogon");
            //foreach (var p in processes)
            //{
            //    if ((uint)p.SessionId == dwSessionId)
            //    {
            //        winlogonPid = (uint)p.Id;
            //    }
            //}

            //// obtain a handle to the winlogon process
            //hProcess = OpenProcess(MaximumAllowed, false, winlogonPid);
            //var hProcess = OpenProcess(MaximumAllowed, false, (uint)Process.GetCurrentProcess().Id);

            // obtain a handle to the access token of the winlogon process
            //IntPtr hPToken = IntPtr.Zero;
            //if (!OpenProcessToken(hProcess, TokenDuplicate, ref hPToken))
            //{
            //    CloseHandle(hProcess);
            //    return false;
            //}

            //if (!LogonUser("josh", null, "Kerm&pop45", 2, 0, ref hPToken))
            //{
            //    CloseHandle(hProcess);
            //    return false;
            //}

            // Security attibute structure used in DuplicateTokenEx and CreateProcessAsUser
            // I would prefer to not have to use a security attribute variable and to just 
            // simply pass null and inherit (by default) the security attributes
            // of the existing token. However, in C# structures are value types and therefore
            // cannot be assigned the null value.

            // copy the access token of the winlogon process; the newly created token will be a primary token
            //var hUserTokenDup = IntPtr.Zero;
            //var sa = new SecurityAttributes();
            //sa.Length = Marshal.SizeOf(sa);
            //if (!DuplicateTokenEx(IntPtr.Zero, MaximumAllowed, ref sa, (int)SecurityImpersonationLevel.SecurityImpersonation, (int)TokenType.TokenPrimary, ref hUserTokenDup))
            //{
            //    var lastError = Marshal.GetLastWin32Error();
            //    var exception = new Win32Exception(lastError);
            //    LoggerInstance.LogWriter.Error("DuplicateTokenEx", exception);
            //    //CloseHandle(hProcess);
            //    CloseHandle(IntPtr.Zero);
            //    return false;
            //}

            //int sessionId = 1;
            //var intSize = Marshal.SizeOf(sessionId);
            //var sessionPtr = Marshal.AllocHGlobal(intSize);
            //Marshal.Copy(BitConverter.GetBytes(sessionId), 0, sessionPtr, intSize);

            //if (!SetTokenInformation(hUserTokenDup, TOKEN_INFORMATION_CLASS.TokenSessionId, ref dwSessionId, (uint)IntPtr.Size))
            //{
            //    //didnt set session:!
            //    var lastError = Marshal.GetLastWin32Error();
            //    var exception = new Win32Exception(lastError);
            //    LoggerInstance.LogWriter.Error(exception.Message);
            //    CloseHandle(hUserTokenDup);
            //    //CloseHandle(hProcess);
            //    CloseHandle(userToken);
            //    return false;
            //}

            //Marshal.FreeHGlobal(sessionPtr);

            // By default CreateProcessAsUser creates a process on a non-interactive window station, meaning
            // the window station has a desktop that is invisible and the process is incapable of receiving
            // user input. To remedy this we set the lpDesktop parameter to indicate we want to enable user 
            // interaction with the new process.
            var si = new Startupinfo();
            si.cb = Marshal.SizeOf(si);
            si.lpDesktop = @"winsta0\default"; // interactive window station parameter; basically this indicates that the process created can display a GUI on the desktop

            // flags that specify the priority and creation method of the process
            var dwCreationFlags = NormalPriorityClass | CreateNewConsole;

            //create a new process in the current user's logon session
            var result = CreateProcessAsUser(userToken, // client's access token
                                             null, // file to execute
                                             applicationName, // command line
                                             IntPtr.Zero, // pointer to process SECURITY_ATTRIBUTES
                                             IntPtr.Zero, // pointer to thread SECURITY_ATTRIBUTES
                                             false, // handles are not inheritable
                                             dwCreationFlags, // creation flags
                                             IntPtr.Zero, // pointer to new environment block 
                                             null, // name of current directory 
                                             ref si, // pointer to STARTUPINFO structure
                                             out procInfo // receives information about new process
            );

            //var result = CreateProcessWithLogonW("josh", // client's access token,
            //                                     ".",
            //                                     "Kerm&pop45", // file to execute
            //                                     1,
            //                                     null, // file to execute
            //                                     applicationName, // command line
            //                                     dwCreationFlags, // creation flags
            //                                     IntPtr.Zero, // pointer to new environment block 
            //                                     null, // name of current directory 
            //                                     ref si, // pointer to STARTUPINFO structure
            //                                     out procInfo // receives information about new process
            //);

            if (!result)
            {
                var lastError = Marshal.GetLastWin32Error();
                var exception = new Win32Exception(lastError);
                LoggerInstance.LogWriter.Error("CreateProcessAsUser", exception);
            }

            // invalidate the handles
            //CloseHandle(hProcess);
            //CloseHandle(hPToken);
            CloseHandle(userToken);
            //CloseHandle(hUserTokenDup);

            return result; // return the result
        }
    }
}