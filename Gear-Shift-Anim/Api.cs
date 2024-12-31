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
    public delegate bool FnGetBool();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr FnGetIntPtr();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int FnGetInt();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void FnSetInt(int arg);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void FnSetVoid(bool enable);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void FnVoid();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U4)]
    public delegate uint FnGetUint();
}
