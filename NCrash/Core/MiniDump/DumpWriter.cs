using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Common.Logging;

namespace NCrash.Core.MiniDump
{
    /// <summary>
    /// Sample usage:
    /// <code>
    /// using (FileStream fs = new FileStream("minidump.mdmp", FileMode.Create, FileAccess.ReadWrite, FileShare.Write)) 
    /// { 
    ///		DumpWriter.Write(fs.SafeFileHandle, DumpTypeFlag.WithDataSegs | DumpTypeFlag.WithHandleData); 
    /// } 
    /// </code>
    /// </summary>
    /// <remarks>Code snippet is from http://blogs.msdn.com/b/dondu/archive/2010/10/24/writing-minidumps-in-c.aspx </remarks>
    internal static class DumpWriter
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Creates a new memory dump of specified type and writes it to the specified file.
        /// </summary>
        /// <param name="minidumpFilePath">The minidump file path. Overwritten if exists.</param>
        /// <param name="type">Mini dump type</param>
        /// <returns>True if Settings.MiniDumpType settings is set to anything else then MiniDumpType.None.</returns>
        internal static bool Write(string minidumpFilePath, MiniDumpType type)
        {
            if (type == MiniDumpType.None)
            {
                return false;
            }

            bool created;

            using (var fileStream = new FileStream(minidumpFilePath, FileMode.Create, FileAccess.Write))
            {
                // ToDo: Create the minidump at a seperate process! Use this to deal with access errors: http://social.msdn.microsoft.com/Forums/en/csharpgeneral/thread/c314e6ca-4892-41e7-ae19-b3a36ad640e9
                // Bug: In process minidumps causes all sorts of access problems (i.e. one of them is explained below, debugger prevents accessing private memory)
                created = Write(fileStream.SafeFileHandle, type);
            }

            if (created)
            {
                return true;
            }
            File.Delete(minidumpFilePath);
            return false;
        }

        private static bool Write(SafeHandle fileHandle, MiniDumpType dumpType)
        {
            switch (dumpType)
            {
                case MiniDumpType.None:
                    // just ignore
                    break;
                case MiniDumpType.Tiny:
                    return Write(fileHandle, DumpTypeFlag.WithIndirectlyReferencedMemory | DumpTypeFlag.ScanMemory);
                case MiniDumpType.Normal:
                    // If the debugger is attached, it is not possible to access private read-write memory
                    if (Debugger.IsAttached)
                    {
                        return Write(fileHandle, DumpTypeFlag.WithDataSegs | DumpTypeFlag.WithHandleData | DumpTypeFlag.WithUnloadedModules);
                    }
                    // Bug: Combination of WithPrivateReadWriteMemory + WithDataSegs hangs Visual Studio 2010 SP1 on some cases while loading the minidump for debugging in mixed mode which was created in by a release build application
                    return Write(fileHandle, DumpTypeFlag.WithPrivateReadWriteMemory | DumpTypeFlag.WithDataSegs | DumpTypeFlag.WithHandleData | DumpTypeFlag.WithUnloadedModules);
                case MiniDumpType.Full:
                    return Write(fileHandle, DumpTypeFlag.WithFullMemory);
                default:
                    throw new ArgumentOutOfRangeException("dumpType");
            }
            return false;
        }

        private static bool Write(SafeHandle fileHandle, DumpTypeFlag dumpTypeFlag)
        {
            Process currentProcess = Process.GetCurrentProcess();
            IntPtr currentProcessHandle = currentProcess.Handle;
            uint currentProcessId = (uint)currentProcess.Id;
            MiniDumpExceptionInformation exp;
            exp.ThreadId = GetCurrentThreadId();
            exp.ClientPointers = false;
            exp.ExceptionPointers = IntPtr.Zero;
            exp.ExceptionPointers = Marshal.GetExceptionPointers();

            bool bRet;

            try
            {
                if (exp.ExceptionPointers == IntPtr.Zero)
                {
                    bRet = MiniDumpWriteDump(currentProcessHandle, currentProcessId, fileHandle, (uint)dumpTypeFlag, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                }
                else
                {
                    bRet = MiniDumpWriteDump(currentProcessHandle, currentProcessId, fileHandle, (uint)dumpTypeFlag, ref exp, IntPtr.Zero, IntPtr.Zero);
                }
            }
            catch (DllNotFoundException)
            {
                Logger.Warn("dbghelp.dll was not found inside the application folder, the system path or SDK folder. Minidump was not generated. If you are not planning on using the minidump feature, you can disable it with the Configurator tool.");
                return false;
            }

            if (!bRet)
            {
                int errorCode = Marshal.GetLastWin32Error();
                string errorMessage = new Win32Exception(errorCode).Message;
                Logger.Error("Cannot write the minidump. MiniDumpWriteDump (dbghelp.dll) function returned error code: " + errorCode + " message: " + errorMessage);
                return false;
            }
            return true;
        }

        /* typedef struct _MINIDUMP_EXCEPTION_INFORMATION {
         *    DWORD ThreadId;
         *    PEXCEPTION_POINTERS ExceptionPointers;
         *    BOOL ClientPointers;
         * } MINIDUMP_EXCEPTION_INFORMATION, *PMINIDUMP_EXCEPTION_INFORMATION;
         */

        [StructLayout(LayoutKind.Sequential, Pack = 4)]  // Pack=4 is important! So it works also for x64! 
        private struct MiniDumpExceptionInformation
        {
            public uint ThreadId;
            public IntPtr ExceptionPointers;
            [MarshalAs(UnmanagedType.Bool)]
            public bool ClientPointers;
        }

        /* BOOL 
         * WINAPI 
         * MiniDumpWriteDump( 
         *    __in HANDLE hProcess, 
         *    __in DWORD ProcessId, 
         *    __in HANDLE hFile, 
         *    __in MINIDUMP_TYPE DumpType, 
         *    __in_opt PMINIDUMP_EXCEPTION_INFORMATION ExceptionParam, 
         *    __in_opt PMINIDUMP_USER_STREAM_INFORMATION UserStreamParam, 
         *    __in_opt PMINIDUMP_CALLBACK_INFORMATION CallbackParam 
         *    ); 
         */

        // Overload requiring MiniDumpExceptionInformation 
        [DllImport("dbghelp.dll", EntryPoint = "MiniDumpWriteDump", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        private static extern bool MiniDumpWriteDump(IntPtr hProcess, uint processId, SafeHandle hFile, uint dumpType, ref MiniDumpExceptionInformation expParam, IntPtr userStreamParam, IntPtr callbackParam);

        // Overload supporting MiniDumpExceptionInformation == NULL 
        [DllImport("dbghelp.dll", EntryPoint = "MiniDumpWriteDump", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        private static extern bool MiniDumpWriteDump(IntPtr hProcess, uint processId, SafeHandle hFile, uint dumpType, IntPtr expParam, IntPtr userStreamParam, IntPtr callbackParam);

        [DllImport("kernel32.dll", EntryPoint = "GetCurrentThreadId", ExactSpelling = true, SetLastError = true)]
        private static extern uint GetCurrentThreadId();
    }
}
