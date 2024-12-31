using System;
using System.Runtime.InteropServices;

namespace Gear_Shifting_Anim
{
    static class Dll
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string lpFileName);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    delegate bool FnGetBool();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate IntPtr FnGetIntPtr();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void FnSetInt(int arg);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void FnSetVoid(bool enable);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void FnVoid();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U4)]
    delegate uint FnGetUint();
}
