using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace y0trainer
{
    public class MemoryException : Exception
    {
        public MemoryException() { }
        public MemoryException(string message) : base(message) { }
        public MemoryException(string message, Exception inner) : base(message, inner) { }
    }

    public class Memory
    {
        const UInt32 INFINITE = 0xFFFFFFFF;

        public enum AllocationType
        {
            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
            Release = 0x8000,
            Reset = 0x80000,
            Physical = 0x400000,
            TopDown = 0x100000,
            WriteWatch = 0x200000,
            LargePages = 0x20000000
        }

        public enum MemoryProtection
        {
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            NoAccess = 0x01,
            ReadOnly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            GuardModifierflag = 0x100,
            NoCacheModifierflag = 0x200,
            WriteCombineModifierflag = 0x400
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] buffer, int dwSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] buffer, int dwSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

        string ProcessName = null;
        Process Proc = null;

        public Memory(string TargetName)
        {
            ProcessName = TargetName;
        }

        public bool Valid()
        {
            if (Proc == null)
                return false;

            if (Proc.HasExited)
                return false;

            return true;
        }

        public bool Open()
        {
            Process[] vals = Process.GetProcessesByName(ProcessName);
            if (vals.Length == 0)
                return false;

            Proc = vals[0];
            return true;
        }

        public Process GetProcess()
        {
            return Proc;
        }

        public bool AllocateBuffer(byte[] Buffer, ref IntPtr Address)
        {
            Address = VirtualAllocEx(Proc.Handle, IntPtr.Zero, (uint)Buffer.Length, AllocationType.Commit, MemoryProtection.ExecuteReadWrite);
            if (Address == IntPtr.Zero)
                return false;

            return true;
        }

        public bool ExecuteBuffer(IntPtr Address, IntPtr Argument)
        {
            IntPtr ThreadId = IntPtr.Zero;
            IntPtr Thread = CreateRemoteThread(Proc.Handle, IntPtr.Zero, 0, Address, Argument, 0, out ThreadId);
            if (Thread == IntPtr.Zero)
                return false;

            WaitForSingleObject(Thread, INFINITE);
            return true;
        }

        public string ByteArrayToString(byte[] bytes)
        {
            var sb = new StringBuilder("new byte[] { ");
            foreach (var b in bytes)
            {
                sb.Append(b.ToString("X") + ", ");
            }
            sb.Append("}");

            return sb.ToString();
        }

        public bool CallAddress(UInt64 Address, IntPtr Argument)
        {
            if (Address == 0)
                return false;

            byte[] CallDestination = BitConverter.GetBytes(Address);
            byte[] CallInstruction = 
            {
                0xFF, 0x25, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };

            for (int i = 0; i < CallDestination.Length; i++)
            {
                CallInstruction[6 + i] = CallDestination[i];
            }

            // System.Windows.Forms.MessageBox.Show(ByteArrayToString(CallInstruction));

            IntPtr RemoteBuffer = IntPtr.Zero;
            if (!AllocateBuffer(CallInstruction, ref RemoteBuffer))
                return false;

            IntPtr NumberOfBytesWritten = IntPtr.Zero;
            if (!WriteProcessMemory(Proc.Handle, RemoteBuffer, CallInstruction, CallInstruction.Length, out NumberOfBytesWritten))
                return false;

            return ExecuteBuffer(RemoteBuffer, Argument);
        }

        public bool Read(IntPtr Address, ref byte[] Buffer)
        {
            IntPtr num;
            return ReadProcessMemory(Proc.Handle, Address, Buffer, Buffer.Length, out num);
        }

        public bool Write(IntPtr Address, byte[] Buffer)
        {
            IntPtr num;
            return WriteProcessMemory(Proc.Handle, Address, Buffer, Buffer.Length, out num);
        }

        public IntPtr Ptr(IntPtr Address)
        {
            byte[] value = new byte[8];
            if (!Read(Address, ref value))
                throw new MemoryException("Unable to read pointer at (" + Address.ToInt64().ToString("X") + ")");

            return new IntPtr((long) BitConverter.ToUInt64(value, 0));
        }

        public float Float(IntPtr Address)
        {
            byte[] value = new byte[4];
            if (!Read(Address, ref value))
                throw new MemoryException("Unable to read memory.");

            return BitConverter.ToSingle(value, 0);
        }

        public void Float(IntPtr Address, float value)
        {
            if (!Write(Address, BitConverter.GetBytes(value)))
                throw new MemoryException("Unable to write memory.");
        }

        public Byte U8(IntPtr Address)
        {
            byte[] value = new byte[1];
            if (!Read(Address, ref value))
                throw new MemoryException("Unable to read memory.");

            return value[0];
        }

        public void U8(IntPtr Address, Byte value)
        {
            if (!Write(Address, BitConverter.GetBytes(value)))
                throw new MemoryException("Unable to write memory.");
        }

        public UInt16 U16(IntPtr Address)
        {
            byte[] value = new byte[2];
            if (!Read(Address, ref value))
                throw new MemoryException("Unable to read memory.");

            return BitConverter.ToUInt16(value, 0);
        }

        public void U16(IntPtr Address, UInt16 value)
        {
            if (!Write(Address, BitConverter.GetBytes(value)))
                throw new MemoryException("Unable to write memory.");
        }

        public UInt32 U32(IntPtr Address)
        {
            byte[] value = new byte[4];
            if (!Read(Address, ref value))
                throw new MemoryException("Unable to read memory.");

            return BitConverter.ToUInt32(value, 0);
        }

        public void U32(IntPtr Address, UInt32 value)
        {
            if (!Write(Address, BitConverter.GetBytes(value)))
                throw new MemoryException("Unable to write memory.");
        }

        public UInt64 U64(IntPtr Address)
        {
            byte[] value = new byte[8];
            if (!Read(Address, ref value))
                throw new MemoryException("Unable to read memory.");

            return BitConverter.ToUInt64(value, 0);
        }

        public void U64(IntPtr Address, UInt64 value)
        {
            if (!Write(Address, BitConverter.GetBytes(value)))
                throw new MemoryException("Unable to write memory.");
        }
    }
}
