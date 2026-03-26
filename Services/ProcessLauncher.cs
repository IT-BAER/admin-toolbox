using AdminToolbox.Models;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;

namespace AdminToolbox.Services;

/// <summary>
/// Launches MMC snap-ins using CreateProcessWithLogonW (runas /netonly equivalent)
/// so the GUI runs under the local interactive session but network authentication
/// uses the supplied domain admin credentials.
/// No plain-text password is ever written to a string; it is extracted from the
/// SecureString in a pinned GCHandle and zeroed immediately after the LogonW call.
/// </summary>
public static class ProcessLauncher
{
    public static void Launch(AdminTool tool)
    {
        var store = CredentialStore.Instance;
        if (!store.HasCredentials)
            throw new InvalidOperationException("No credentials stored.");

        // Expand msc path (e.g. %SystemRoot%)
        var mscExpanded = Environment.ExpandEnvironmentVariables(tool.MscPath);
        var commandLine = $"mmc.exe \"{mscExpanded}\"";

        // GetPassword returns the stored reference — we do NOT own it, so no using/dispose.
        // The CredentialStore disposes it on Lock or app exit.
        var pwd = store.GetPassword();
        LaunchWithLogon(store.Username!, store.Domain, pwd, commandLine);
    }

    // -------------------------------------------------------------------------
    // P/Invoke
    // -------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct STARTUPINFO
    {
        public int    cb;
        public string? lpReserved;
        public string? lpDesktop;
        public string? lpTitle;
        public uint   dwX, dwY, dwXSize, dwYSize;
        public uint   dwXCountChars, dwYCountChars;
        public uint   dwFillAttribute;
        public uint   dwFlags;
        public short  wShowWindow, cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess, hThread;
        public uint   dwProcessId, dwThreadId;
    }

    private const uint LOGON_NETCREDENTIALS_ONLY = 2; // equivalent to runas /netonly
    private const uint CREATE_DEFAULT_ERROR_MODE = 0x04000000;

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateProcessWithLogonW(
        string lpUsername,
        string? lpDomain,
        IntPtr lpPassword,          // LPCWSTR — we pass the pinned BSTR ourselves
        uint dwLogonFlags,
        string? lpApplicationName,
        string lpCommandLine,
        uint dwCreationFlags,
        IntPtr lpEnvironment,
        string? lpCurrentDirectory,
        ref STARTUPINFO lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr hObject);

    private static void LaunchWithLogon(
        string username,
        string? domain,
        SecureString password,
        string commandLine)
    {
        // Marshal the SecureString to an unmanaged BSTR and zero it immediately after use.
        IntPtr bstr = Marshal.SecureStringToGlobalAllocUnicode(password);
        try
        {
            var si = new STARTUPINFO { cb = Marshal.SizeOf<STARTUPINFO>() };

            bool ok = CreateProcessWithLogonW(
                username,
                domain,
                bstr,
                LOGON_NETCREDENTIALS_ONLY,
                null,
                commandLine,
                CREATE_DEFAULT_ERROR_MODE,
                IntPtr.Zero,
                null,
                ref si,
                out var pi);

            if (!ok)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            // We do not wait for the child process — close handles immediately.
            CloseHandle(pi.hProcess);
            CloseHandle(pi.hThread);
        }
        finally
        {
            // Zero the BSTR in unmanaged memory before freeing.
            Marshal.ZeroFreeGlobalAllocUnicode(bstr);
        }
    }
}
