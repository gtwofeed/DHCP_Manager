using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Net;

namespace DHCP_Manager_v2
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class DHCPDS_SERVER
    {
        public UInt32 Version;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string ServerName;
        public UInt32 ServerAddress;
        public UInt32 Flags;
        public UInt32 State;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string DsLocation;
        public UInt32 DsLocType;
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class DHCP_SERVER_INFO_ARRAY
    {
        public UInt32 Flags;
        public UInt32 NumElements;
        public IntPtr Servers;
    }
    // This is a custom type/class
    public struct DHCP_SERVERS
    {
        public string ServerName;
        public string ServerAddress;
    }   

    internal class Program
    {
        static void Main(string[] args)
        {
            DHCP_SERVERS[] dhcpServers = GetDHCPServers();
        }

        public static DHCP_SERVERS[] GetDHCPServers() // добавил static
        {
            UInt32 DHCPResult = 0;
            uint nr = 0;
            IntPtr svrs;
            DHCPResult = DhcpEnumServers(nr, ref nr, out svrs, ref nr, ref nr);
            if (DHCPResult == 0)
            {
                DHCP_SERVER_INFO_ARRAY dsArray = (DHCP_SERVER_INFO_ARRAY)Marshal.PtrToStructure(svrs, typeof(DHCP_SERVER_INFO_ARRAY));
                int size = (int)dsArray.NumElements;
                IntPtr outArray = dsArray.Servers;
                DHCPDS_SERVER[] serverList = new DHCPDS_SERVER[size];
                DHCP_SERVERS[] outlist = new DHCP_SERVERS[size];
                IntPtr current = outArray;
                for (int i = 0; i < size; i++)
                {
                    serverList[i] = new DHCPDS_SERVER();
                    Marshal.PtrToStructure(current, serverList[i]);
                    Marshal.DestroyStructure(current, typeof(DHCPDS_SERVER));
                    current = (IntPtr)((int)current + Marshal.SizeOf(serverList[i]));
                    outlist[i].ServerName = serverList[i].ServerName;
                    outlist[i].ServerAddress = UInt32IPAddressToString(serverList[i].ServerAddress);
                }
                Marshal.FreeCoTaskMem(outArray);
                return outlist;
            }
            return null; // no server found
        }
        public static string UInt32IPAddressToString(uint ipAddress) // заменил private на public static
        {
            IPAddress ipA = new IPAddress(ipAddress);
            string[] sIp = ipA.ToString().Split('.');
            return sIp[3] + "." + sIp[2] + "." + sIp[1] + "." + sIp[0];
        }

        [DllImport("dhcpsapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern uint DhcpEnumServers(
        uint Flags,
        ref uint IdInfo,
        out IntPtr Servers,
        ref uint CallbackFn,
        ref uint CallbackData
    );
    }
}