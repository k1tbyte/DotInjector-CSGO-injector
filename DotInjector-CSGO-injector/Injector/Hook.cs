using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using static DotInjector_CSGO_injector.Injector.Native;
using System.Security.Cryptography;
using System.Windows.Shapes;
using System.Text;

namespace DotInjector_CSGO_injector.Injector
{
    internal enum InjectResponse : byte
    {
        InvalidPath,
        DllNotExist,
        InvalidProcess,
        Not32xDll,
        BypassUnhookError,
        BypassRestoreHookError,
        AttachDllError,
        OK,
    }


    internal static class Hook
    {
        private static byte[,] Bytes;
        private static Process GameProcess;


        public static InjectResponse Inject(Process process,string dllPath)
        {
            if (process == null || process.Id == 0 || process.Handle == IntPtr.Zero)
                return InjectResponse.InvalidProcess;

            if (!File.Exists(dllPath))
                return InjectResponse.InvalidPath;

            if (Native.IsWow64Dll(dllPath))
                return InjectResponse.Not32xDll;

            Bytes = new byte[17, 6];
            GameProcess = process;

            //Unhook vac
            for (int i = 0; i < Native.WinAPIFuncsList.Count; i++)
            {
                if (!UnhookDll(Native.WinAPIFuncsList[i], i))
                    return InjectResponse.BypassUnhookError;
            }

            //Inject
            if (!AttachDll(dllPath))
                return InjectResponse.AttachDllError;


            //Restore hook vac
            for (int i = 0; i < Native.WinAPIFuncsList.Count; i++)
            {
                if (!HookDll(Native.WinAPIFuncsList[i], i))
                    return InjectResponse.BypassRestoreHookError;
            }

            return InjectResponse.OK;
        }

        private static bool AttachDll(string dllPath)
        {
            IntPtr size = (IntPtr)dllPath.Length;

            IntPtr DLLMemory = VirtualAllocEx(GameProcess.Handle, IntPtr.Zero, size, AllocationType.Reserve | AllocationType.Commit,
                MemoryProtection.ExecuteReadWrite);

            if (DLLMemory == IntPtr.Zero)
                return false;
            

            byte[] bytes = Encoding.ASCII.GetBytes(dllPath);

            if (!WriteProcessMemory(GameProcess.Handle, DLLMemory, bytes, (int)bytes.Length, out _))
                return false;

            IntPtr kernel32Handle = GetModuleHandle("Kernel32.dll");
            IntPtr loadLibraryAAddress = GetProcAddress(kernel32Handle, "LoadLibraryA");

            if (loadLibraryAAddress == IntPtr.Zero)
                   return false;

            IntPtr threadHandle = CreateRemoteThread(GameProcess.Handle, IntPtr.Zero, 0, loadLibraryAAddress, DLLMemory, 0,
                IntPtr.Zero);

            if (threadHandle == IntPtr.Zero)
                return false;

            CloseHandle(threadHandle);
            CloseHandle(GameProcess.Handle);
            return true;
        }


        private static bool UnhookDll(Native.WinAPIFunc func, Int32 index)
        {
            IntPtr originalMethodAddress = Native.GetProcAddress(Native.LoadLibrary(func.DllName), func.FunctionName);

            if (originalMethodAddress == IntPtr.Zero)
            {
                return false;
            }

            byte[] originalGameBytes = new byte[6];

            Native.ReadProcessMemory(GameProcess.Handle, originalMethodAddress, originalGameBytes, sizeof(byte) * 6, out _);

            for (int i = 0; i < originalGameBytes.Length; i++)
            {
                Bytes[index, i] = originalGameBytes[i];
            }

            byte[] originalDLLBytes = new byte[6];

            GCHandle pinnedArray = GCHandle.Alloc(originalDLLBytes, GCHandleType.Pinned);
            IntPtr originalDLLBytesPointer = pinnedArray.AddrOfPinnedObject();

            Native.memcpy(originalDLLBytesPointer, originalMethodAddress, (UIntPtr)(sizeof(byte) * 6));

            return Native.WriteProcessMemory(GameProcess.Handle, originalMethodAddress, originalDLLBytes, sizeof(byte) * 6, out _);
        }

        private static bool HookDll(Native.WinAPIFunc func, Int32 index)
        {
            IntPtr originalMethodAdress = Native.GetProcAddress(Native.LoadLibrary(func.DllName), func.FunctionName);

            if (originalMethodAdress == IntPtr.Zero)
            {
                return false;
            }

            byte[] origBytes = new byte[6];

            for (int i = 0; i < origBytes.Length; i++)
            {
                origBytes[i] = Bytes[index, i];
            }

            return Native.WriteProcessMemory(GameProcess.Handle, originalMethodAdress, origBytes, sizeof(byte) * 6, out _);
        }

        
    }
}
