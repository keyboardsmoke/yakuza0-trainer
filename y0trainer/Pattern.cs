using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace y0trainer
{
    public class Pattern
    {
        enum ProcessorArchitecture
        {
            X86 = 0,
            X64 = 9,
            @Arm = -1,
            Itanium = 6,
            Unknown = 0xFFFF,
        }

        [StructLayout(LayoutKind.Sequential)]
        struct SystemInfo
        {
            public ProcessorArchitecture ProcessorArchitecture;
            public UInt32 PageSize;
            public IntPtr MinimumApplicationAddress;
            public IntPtr MaximumApplicationAddress;
            public IntPtr ActiveProcessorMask;
            public UInt32 NumberOfProcessors;
            public UInt32 ProcessorType;
            public UInt32 AllocationGranularity;
            public UInt16 ProcessorLevel;
            public UInt16 ProcessorRevision;
        }

        [DllImport("kernel32.dll", SetLastError = false)]
        static extern void GetSystemInfo(out SystemInfo Info);

        static bool Compare(byte[] Data, Int64 Index, string Pattern, string Mask)
        {
            for (int i = 0; i < Mask.Length; i++)
            {
                if (Mask[i] == 'x' && Data[i + Index] != Pattern[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static IntPtr Find(y0trainer.Memory Mem, IntPtr Start, IntPtr End, string Pattern, string Mask)
        {
            Int64 PartitionSize = 0x10000;

            if (Start == IntPtr.Zero)
            {
                SystemInfo info = new SystemInfo();
                GetSystemInfo(out info);

                Start = info.MinimumApplicationAddress;
                End = info.MaximumApplicationAddress;
            }

            for (IntPtr i = Start; i.ToInt64() < End.ToInt64();)
            {
                IntPtr ReadAddress = (i == Start) ? i : IntPtr.Subtract(i, Mask.Length);
                Int64 ReadSize = ((i.ToInt64() + PartitionSize) <= End.ToInt64()) ? PartitionSize : (End.ToInt64() - i.ToInt64());
                Int64 FullReadSize = (ReadAddress != i) ? ReadSize + Mask.Length : ReadSize;

                byte[] currentBuffer = new byte[FullReadSize];

                try
                {
                    if (Mem.Read(ReadAddress, ref currentBuffer))
                    {
                        for (Int64 p = 0; p < FullReadSize; p++)
                        {
                            bool result = Compare(currentBuffer, p, Pattern, Mask);

                            if (result == true)
                            {
                                return IntPtr.Add(ReadAddress, (int)p);
                            }
                        }
                    }
                }
                catch(Exception)
                {
                    // Do nothing
                }

                i = IntPtr.Add(i, (int)ReadSize);
            }

            return IntPtr.Zero;
        }

        public static IntPtr Find(y0trainer.Memory Mem, string Pattern, string Mask)
        {
            if (Mem.GetProcess() == null)
                return IntPtr.Zero;

            var Module = Mem.GetProcess().MainModule;

            IntPtr Start = Module.BaseAddress;
            IntPtr End = IntPtr.Add(Module.BaseAddress, Module.ModuleMemorySize);

            return Find(Mem, Start, End, Pattern, Mask);
        }
    }
}
