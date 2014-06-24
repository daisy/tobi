using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Tobi
{
    public partial class Shell
    {
        public event EventHandler DeviceArrived;
        public event EventHandler DeviceRemoved;

        private IntPtr HwndSourceHookWindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == Win32.WM_DEVICECHANGE)
            {
                int code = wParam.ToInt32();

                switch (code)
                {
                    case Win32.DBT_DEVICEARRIVAL:
                        {
                            Console.WriteLine(" --- Win32.DBT_DEVICEARRIVAL");

                            if (isGUID_DEVINTERFACE_HID(lParam))
                            {
                                var eventHandler = DeviceArrived;
                                if (eventHandler != null)
                                {
                                    eventHandler(this, EventArgs.Empty);
                                }
                            }
                            break;
                        }
                    case Win32.DBT_DEVICEREMOVECOMPLETE:
                        {
                            Console.WriteLine(" --- Win32.DBT_DEVICEREMOVECOMPLETE");

                            if (isGUID_DEVINTERFACE_HID(lParam))
                            {
                                var eventHandler = DeviceRemoved;
                                if (eventHandler != null)
                                {
                                    eventHandler(this, EventArgs.Empty);
                                }
                            }
                            break;
                        }

                    case Win32.DBT_DEVNODES_CHANGED:
                        {
                            Console.WriteLine(" --- Win32.DBT_DEVNODES_CHANGED");
                            break;
                        }
                    default:
                        {
#if DEBUG
                            Debugger.Break();
#endif
                            break;
                        }
                }
            }

            return IntPtr.Zero;
        }

        private bool isGUID_DEVINTERFACE_HID(IntPtr lParam)
        {
            var pHDR = (Win32.DEV_BROADCAST_HDR)Marshal.PtrToStructure(lParam, typeof(Win32.DEV_BROADCAST_HDR));

            int devType = pHDR.dbch_devicetype; // Marshal.ReadInt32(lParam, 4);
            switch (devType)
            {
                case Win32.DBT_DEVTYP_DEVICEINTERFACE:
                    {
                        Console.WriteLine("Win32.DBT_DEVTYP_DEVICEINTERFACE");

                        var structure = (Win32.DEV_BROADCAST_DEVICEINTERFACE)Marshal.PtrToStructure(lParam, typeof(Win32.DEV_BROADCAST_DEVICEINTERFACE));
                        Console.WriteLine("GUID: " + structure.dbcc_classguid.ToString());
                        
                        if (structure.dbcc_classguid.Equals(Win32.GUID_DEVINTERFACE_USB_DEVICE))
                        {
                            Console.WriteLine("(GUID_DEVINTERFACE_USB_DEVICE)");
                        }
                        else if (structure.dbcc_classguid.Equals(Win32.GUID_DEVINTERFACE_HID))
                        {
                            Console.WriteLine("(GUID_DEVINTERFACE_HID)");
                            return true;
                        }

                        //var structure1 = (Win32.DEV_BROADCAST_DEVICEINTERFACE1)Marshal.PtrToStructure(lParam, typeof(Win32.DEV_BROADCAST_DEVICEINTERFACE1));
                        //Console.WriteLine("GUID1: " + new Guid(structure1.dbcc_classguid).ToString());
                        //Console.WriteLine("NAME1: " + new String(structure1.dbcc_name).ToString());
                        break;
                    }
                case Win32.DBT_DEVTYP_VOLUME:
                    {
                        Console.WriteLine("Win32.DBT_DEVTYP_VOLUME");

                        var structure = (Win32.DEV_BROADCAST_VOLUME)Marshal.PtrToStructure(lParam, typeof(Win32.DEV_BROADCAST_VOLUME));
                        Console.WriteLine("DRIVE: " + Win32.DriveMaskToLetter(structure.dbcv_unitmask));
                        break;
                    }
                case Win32.DBT_DEVTYP_HANDLE:
                    {
#if DEBUG
                        Debugger.Break();
#endif
                        break;
                    }
                case Win32.DBT_DEVTYP_PORT:
                    {
#if DEBUG
                        Debugger.Break();
#endif
                        break;
                    }
                case Win32.DBT_DEVTYP_OEM:
                    {
#if DEBUG
                        Debugger.Break();
#endif
                        break;
                    }
                default:
                    {
#if DEBUG
                        Debugger.Break();
#endif
                        break;
                    }
            }
            return false;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            //WindowInteropHelper wih = new WindowInteropHelper(this);
            //int style = GetWindowLong(wih.Handle, GWL_STYLE);
            //SetWindowLong(wih.Handle, GWL_STYLE, style & ~WS_SYSMENU);

            var source = PresentationSource.FromVisual(this) as HwndSource;
            if (source != null)
            {
                source.AddHook(new HwndSourceHook(HwndSourceHookWindowProc));

                Win32.RegisterDeviceNotification_All(source.Handle);
            }
        }

        private void resetDeviceHook()
        {
            //IntPtr windowHandle = (new WindowInteropHelper(this)).Handle;
            //HwndSource source = HwndSource.FromHwnd(windowHandle);

            var source = PresentationSource.FromVisual(this) as HwndSource;
            if (source != null)
                source.RemoveHook(new HwndSourceHook(HwndSourceHookWindowProc));
        }
    }

    //dbt.h and winuser.h
    public class Win32
    {
        //private const int GWL_STYLE = -16;
        //private const int WS_SYSMENU = 0x00080000;

        //[DllImport("user32.dll")]
        //private extern static int SetWindowLong(IntPtr hwnd, int index, int value);
        //[DllImport("user32.dll")]
        //private extern static int GetWindowLong(IntPtr hwnd, int index);

        public const int WM_DEVICECHANGE = 0x0219, // MSG
                         DBT_DEVICEARRIVAL = 0x8000, // WPARAM
                         DBT_DEVICEREMOVECOMPLETE = 0x8004, // WPARAM
                         DBT_DEVNODES_CHANGED = 0x7; // WPARAM

        public const int DEVICE_NOTIFY_ALL_INTERFACE_CLASSES = 0x00000004,
                         DEVICE_NOTIFY_WINDOW_HANDLE = 0,
                         DEVICE_NOTIFY_SERVICE_HANDLE = 1;

        // DEV_BROADCAST_HDR (LPARAM) device type
        //http://msdn.microsoft.com/en-us/library/aa363246%28VS.85%29.aspx
        public const int DBT_DEVTYP_DEVICEINTERFACE = 0x00000005,
            DBT_DEVTYP_HANDLE = 0x00000006,
            DBT_DEVTYP_OEM = 0x00000000,
            DBT_DEVTYP_PORT = 0x00000003,
            DBT_DEVTYP_VOLUME = 0x00000002;

        public static Guid GUID_DEVINTERFACE_USB_DEVICE = new Guid("a5dcbf10-6530-11d2-901f-00c04fb951ed");
        public static Guid GUID_DEVINTERFACE_HID = new Guid("4D1E55B2-F16F-11CF-88CB-001111000030");

        public static Guid GUID_DEVCLASS_VOLUME = new Guid("53f5630d-b6bf-11d0-94f2-00a0c91efb8b");
        public static Guid GUID_DEVCLASS_KEYBOARD = new Guid("4D36E96B-E325-11CE-BFC1-08002BE10318");
        public static Guid GUID_DEVCLASS_MOUSE = new Guid("4D36E96F-E325-11CE-BFC1-08002BE10318");

        [StructLayout(LayoutKind.Sequential)]
        public struct DEV_BROADCAST_HDR
        {
            public int dbch_size;
            public int dbch_devicetype;
            public int dbch_reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DEV_BROADCAST_DEVICEINTERFACE
        {
            public int dbcc_size;
            public int dbcc_devicetype;
            public int dbcc_reserved;
            public Guid dbcc_classguid;
            public short dbcc_name;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public class DEV_BROADCAST_DEVICEINTERFACE1
        {
            public int dbcc_size;
            public int dbcc_devicetype;
            public int dbcc_reserved;

            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 16)]
            public byte[] dbcc_classguid;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
            public char[] dbcc_name;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DEV_BROADCAST_VOLUME
        {
            public int dbcv_size;
            public int dbcv_devicetype;
            public int dbcv_reserved;
            public int dbcv_unitmask;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVINFO_DATA
        {
            public int cbSize;
            public Guid ClassGuid;
            public int DevInst;
            public int Reserved;
        }

        public static char DriveMaskToLetter(int mask)
        {
            char letter;
            string drives = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            // 1 = A, 2 = B, 4 = C...
            int cnt = 0;
            int pom = mask / 2;
            while (pom != 0)
            {
                // while there is any bit set in the mask, shift it to the right...
                pom = pom / 2;
                cnt++;
            }

            if (cnt < drives.Length)
                letter = drives[cnt];
            else
                letter = '?';

            return letter;
        }

        public const int DIGCF_PRESENT = 2;

        public const int CR_SUCCESS = 0;

        public static string CM_Get_Device_ID(int DevInst)
        {
            string s = null;
            int len = 300;
            IntPtr buffer = Marshal.AllocHGlobal(len);
            int r = CM_Get_Device_ID(DevInst, buffer, len, 0);
            if (r == CR_SUCCESS) s = Marshal.PtrToStringAnsi(buffer);
            return s;
        }

        public static bool CheckDevice(ref Guid guid)
        {
            IntPtr devinfo = SetupDiGetClassDevs(ref guid, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT);
            SP_DEVINFO_DATA devInfoSet = new SP_DEVINFO_DATA();
            devInfoSet.cbSize = Marshal.SizeOf(typeof(SP_DEVINFO_DATA));
            return SetupDiEnumDeviceInfo(devinfo, 0, ref devInfoSet);
        }

        public static void RegisterDeviceNotification_All(IntPtr handle)
        {
            var dbi = new DEV_BROADCAST_DEVICEINTERFACE();
            int size = Marshal.SizeOf(dbi);
            dbi.dbcc_size = size;
            dbi.dbcc_devicetype = DBT_DEVTYP_DEVICEINTERFACE;
            dbi.dbcc_reserved = 0;
            dbi.dbcc_classguid = GUID_DEVINTERFACE_HID;
            dbi.dbcc_name = 0;
            IntPtr buffer = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(dbi, buffer, true);

            IntPtr result = RegisterDeviceNotification(
                handle,
                buffer,
                DEVICE_NOTIFY_WINDOW_HANDLE | DEVICE_NOTIFY_ALL_INTERFACE_CLASSES);

            if (result == IntPtr.Zero)
            {
                Console.WriteLine(GetLastError().ToString());
#if DEBUG
                Debugger.Break();
#endif
            }

//            dbi.dbcc_classguid = GUID_DEVINTERFACE_USB_DEVICE;
//            buffer = Marshal.AllocHGlobal(size);
//            Marshal.StructureToPtr(dbi, buffer, true);
//            result = RegisterDeviceNotification(
//                handle,
//                buffer,
//                DEVICE_NOTIFY_WINDOW_HANDLE | DEVICE_NOTIFY_ALL_INTERFACE_CLASSES);

//            if (result == IntPtr.Zero)
//            {
//                Console.WriteLine(GetLastError().ToString());
//#if DEBUG
//                Debugger.Break();
//#endif
//            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr NotificationFilter, Int32 Flags);

        [DllImport("kernel32.dll")]
        public static extern int GetLastError();

        [DllImport("setupapi.dll")]
        public static extern IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, IntPtr Enumerator, IntPtr hWndParent, int Flags);

        [DllImport("setupapi.dll")]
        public static extern bool SetupDiEnumDeviceInfo(IntPtr DeviceInfoSet, int Supplies, ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("cfgmgr32.dll")]
        public static extern int CM_Get_Device_ID(int DevInst, IntPtr Buffer, int BufferLen, int Flags);
    }
}
