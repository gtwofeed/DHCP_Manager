using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Xml;
using System.Net;

namespace dhcp_enum_clients
{
    public struct CUSTOM_CLIENT_INFO
    {
    public string ClientName;
    public string IpAddress;
    public string MacAddress;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DHCP_CLIENT_INFO_ARRAY
    {
    public uint NumElements;
    public IntPtr Clients;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DHCP_CLIENT_UID
    {
    public uint DataLength;
    public IntPtr Data;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DATE_TIME
    {
    public uint dwLowDateTime;
    public uint dwHighDateTime;

    public DateTime Convert()
    {
        if (dwHighDateTime== 0 && dwLowDateTime == 0)
        {
        return DateTime.MinValue;
        }
        if (dwHighDateTime == int.MaxValue && dwLowDateTime == UInt32.MaxValue)
        {
        return DateTime.MaxValue;
        }
        return DateTime.FromFileTime((((long) dwHighDateTime) << 32) | (UInt32) dwLowDateTime);
    }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct DHCP_HOST_INFO
    {
    public uint IpAddress;
    public string NetBiosName;
    public string HostName;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct DHCP_CLIENT_INFO
    {
    public uint ClientIpAddress;
    public uint SubnetMask;
    public DHCP_CLIENT_UID ClientHardwareAddress; //no pointer -> structure !!
    [MarshalAs(UnmanagedType.LPWStr)]
    public string ClientName;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string ClientComment;
    public DATE_TIME ClientLeaseExpires; //no pointer -> structure !!
    public DHCP_HOST_INFO OwnerHost; //no pointer -> structure
    }

    class Program
    {
    static void Main()
    {
        enum_clients("192.168.0.254", "192.168.0.0");
    }

    static void enum_clients(string Server, string Subnet)
    {
        string ServerIpAddress = Server;
        uint Response = 0;
        uint SubnetMask = StringIPAddressToUInt32(Subnet);
        IntPtr info_array_ptr;
        uint ResumeHandle = 0;
        uint nr_clients_read = 0;
        uint nr_clients_total = 0;

        Response = DhcpEnumSubnetClients(ServerIpAddress, SubnetMask, ref ResumeHandle,
           65536, out info_array_ptr, ref nr_clients_read, ref nr_clients_total);

        DHCP_CLIENT_INFO_ARRAY clients = (DHCP_CLIENT_INFO_ARRAY)Marshal.PtrToStructure(info_array_ptr, typeof(DHCP_CLIENT_INFO_ARRAY));
        Console.WriteLine(clients.NumElements.ToString());
        int size = (int)clients.NumElements;
        IntPtr[] ptr_array = new IntPtr[size];
        IntPtr current = clients.Clients;
        for (int i = 0; i < size; i++)
        {
        ptr_array[i] = Marshal.ReadIntPtr(current);
        current = (IntPtr)((int)current + (int)Marshal.SizeOf(typeof(IntPtr)));
        }
        CUSTOM_CLIENT_INFO[] clients_array = new CUSTOM_CLIENT_INFO[size];
        for (int i = 0; i < size; i++)
        {
        DHCP_CLIENT_INFO curr_element = (DHCP_CLIENT_INFO)Marshal.PtrToStructure(ptr_array[i], typeof(DHCP_CLIENT_INFO));
        clients_array[i].IpAddress = UInt32IPAddressToString(curr_element.ClientIpAddress);
        clients_array[i].ClientName = curr_element.ClientName;
        clients_array[i].MacAddress = String.Format("{0:x2}-{1:x2}-{2:x2}-{3:x2}-{4:x2}-{5:x2}",
            Marshal.ReadByte(curr_element.ClientHardwareAddress.Data),
            Marshal.ReadByte(curr_element.ClientHardwareAddress.Data, 1),
            Marshal.ReadByte(curr_element.ClientHardwareAddress.Data, 2),
            Marshal.ReadByte(curr_element.ClientHardwareAddress.Data, 3),
            Marshal.ReadByte(curr_element.ClientHardwareAddress.Data, 4),
            Marshal.ReadByte(curr_element.ClientHardwareAddress.Data, 5));

        //This section will throw an AccessViolationException
        // Marshal.DestroyStructure(current, typeof(DHCP_CLIENT_INFO));
        // current = (IntPtr)((int)current + (int)Marshal.SizeOf(curr_element));
        //Replace with:
        Marshal.DestroyStructure(ptr_array[i], typeof(DHCP_CLIENT_INFO));
        }
        Console.WriteLine("");
    }

    public static uint StringIPAddressToUInt32(string ip_string)
    {
        IPAddress IpA = System.Net.IPAddress.Parse(ip_string);
        byte[] ip_bytes = IpA.GetAddressBytes();
        uint ip_uint = (uint)ip_bytes[0] << 24;
        ip_uint += (uint)ip_bytes[1] << 16;
        ip_uint += (uint)ip_bytes[2] << 8;
        ip_uint += (uint)ip_bytes[3];
        return ip_uint;
    }

    public static string UInt32IPAddressToString(uint ipAddress)
    {
        IPAddress ipA = new IPAddress(ipAddress);
        string[] sIp = ipA.ToString().Split('.');

        return sIp[3] + "." + sIp[2] + "." + sIp[1] + "." + sIp[0];
    }

    [DllImport("dhcpsapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern uint DhcpEnumSubnetClients(
        string ServerIpAddress,
        uint SubnetAddress,
        ref uint ResumeHandle,
        uint PreferredMaximum,
        out IntPtr ClientInfo,
        ref uint ElementsRead,
        ref uint ElementsTotal
    );

    }
}