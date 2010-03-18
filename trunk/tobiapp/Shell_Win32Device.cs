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
        private long m_PreviousHandle = 0;
        private bool m_WasPreviouslyTriggeredEvent_Arrived;

        private IntPtr HwndSourceHookWindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == Win32.WM_DEVICECHANGE)
            {
                int code = wParam.ToInt32();
                switch (code)
                {
                    case Win32.DBT_DEVICEARRIVAL:
                        {
                        if (!m_WasPreviouslyTriggeredEvent_Arrived)
                            {
                            m_PreviousHandle = 0;
                            }
                        m_WasPreviouslyTriggeredEvent_Arrived = true;

                            var d = DeviceArrived;
                            if (d != null
                                && m_PreviousHandle != hwnd.ToInt64     () )
                            {
                                d(this, EventArgs.Empty);
                                m_PreviousHandle = hwnd.ToInt64 ();
                            }
                            break;
                        }
                    case Win32.DBT_DEVICEREMOVECOMPLETE:
                        {
                        if (m_WasPreviouslyTriggeredEvent_Arrived)
                            {
                            m_PreviousHandle = 0;
                            }
                        m_WasPreviouslyTriggeredEvent_Arrived = false;

                            var d = DeviceRemoved;
                            if (d != null
                                && m_PreviousHandle != hwnd.ToInt64 ())
                            {
                                d(this, EventArgs.Empty);
                                m_PreviousHandle = hwnd.ToInt64 ();
                            }
                            break;
                        }

                    case Win32.DBT_DEVNODES_CHANGED:
                        {
//#if DEBUG
//                            Debugger.Break();
//#endif
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

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var source = PresentationSource.FromVisual(this) as HwndSource;
            if (source != null)
            {
                source.AddHook(new HwndSourceHook(HwndSourceHookWindowProc));

                var dbi = new Win32.DEV_BROADCAST_DEVICEINTERFACE();
                int size = Marshal.SizeOf(dbi);
                dbi.dbcc_size = size;
                dbi.dbcc_devicetype = Win32.DBT_DEVTYP_DEVICEINTERFACE;
                dbi.dbcc_reserved = 0;
                dbi.dbcc_classguid = Win32.GUID_DEVINTERFACE_HID;
                dbi.dbcc_name = 0;
                IntPtr buffer = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(dbi, buffer, true);

                IntPtr result = Win32.RegisterDeviceNotification(
                    source.Handle,
                    buffer,
                    Win32.DEVICE_NOTIFY_WINDOW_HANDLE | Win32.DEVICE_NOTIFY_ALL_INTERFACE_CLASSES);

                if(result == IntPtr.Zero)
                {
                    Console.WriteLine(Win32.GetLastError().ToString());
#if DEBUG
                    Debugger.Break();
#endif
                }
            }
        }

        private bool CheckDevice(ref Guid guid)
        {
            IntPtr devinfo = Win32.SetupDiGetClassDevs(ref guid, IntPtr.Zero, IntPtr.Zero, Win32.DIGCF_PRESENT);
            Win32.SP_DEVINFO_DATA devInfoSet = new Win32.SP_DEVINFO_DATA();
            devInfoSet.cbSize = Marshal.SizeOf(typeof(Win32.SP_DEVINFO_DATA));
            return Win32.SetupDiEnumDeviceInfo(devinfo, 0, ref devInfoSet);
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

    public class Win32
    {
        public const int WM_DEVICECHANGE = 0x0219,
                         DBT_DEVICEARRIVAL = 0x8000,
                         DBT_DEVICEREMOVECOMPLETE = 0x8004,
                         DBT_DEVNODES_CHANGED = 0x7,
                         DEVICE_NOTIFY_ALL_INTERFACE_CLASSES = 0x00000004,
                         DEVICE_NOTIFY_WINDOW_HANDLE = 0;

        public const int
                        DEVICE_NOTIFY_SERVICE_HANDLE = 1,
                        DBT_DEVTYP_DEVICEINTERFACE = 5;

        public static Guid GUID_DEVINTERFACE_HID = new Guid("4D1E55B2-F16F-11CF-88CB-001111000030");


        public static Guid GUID_DEVCLASS_KEYBOARD = new Guid("4D36E96B-E325-11CE-BFC1-08002BE10318");
        public static Guid GUID_DEVCLASS_MOUSE = new Guid("4D36E96F-E325-11CE-BFC1-08002BE10318");

        [StructLayout(LayoutKind.Sequential)]
        public class DEV_BROADCAST_DEVICEINTERFACE
        {
            public int dbcc_size;
            public int dbcc_devicetype;
            public int dbcc_reserved;
            public Guid dbcc_classguid;
            public short dbcc_name;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr RegisterDeviceNotification(
        IntPtr hRecipient,
        IntPtr NotificationFilter,
        Int32 Flags);

        [DllImport("kernel32.dll")]
        public static extern int GetLastError();

        public const int DIGCF_PRESENT = 2;

        [DllImport("setupapi.dll")]
        public static extern IntPtr SetupDiGetClassDevs(ref Guid ClassGuid,
        IntPtr Enumerator, IntPtr hWndParent, int Flags);

        [DllImport("setupapi.dll")]
        public static extern bool SetupDiEnumDeviceInfo(IntPtr DeviceInfoSet,
        int Supplies, ref SP_DEVINFO_DATA DeviceInfoData);

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVINFO_DATA
        {
            public int cbSize;
            public Guid ClassGuid;
            public int DevInst;
            public int Reserved;
        }

        public const int CR_SUCCESS = 0;
        [DllImport("cfgmgr32.dll")]
        public static extern int CM_Get_Device_ID(int DevInst, IntPtr Buffer, int
        BufferLen, int Flags);

        public static string CM_Get_Device_ID(int DevInst)
        {
            string s = null;
            int len = 300;
            IntPtr buffer = Marshal.AllocHGlobal(len);
            int r = CM_Get_Device_ID(DevInst, buffer, len, 0);
            if (r == CR_SUCCESS) s = Marshal.PtrToStringAnsi(buffer);
            return s;
        }
    }
}
