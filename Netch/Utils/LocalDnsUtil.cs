using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Netch.Utils
{
    public class LocalDnsUtil
    {
        public List<string> InitDnsServers { get; set; }

        public string Id { get; set; }

        public bool InitDynamicDns { get; set; }

        public NetworkInterface GetNetworkInterfaceById(string id)
        {
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.Id == id)
                {
                    return adapter;
                }
            }
            return null;
        }

        public NetworkInterface GetNetworkInterfaceByIp(string ipAddr)
        {
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                IPInterfaceProperties adapterProperties = adapter.GetIPProperties();
                IPv4InterfaceProperties p = adapterProperties.GetIPv4Properties();

                foreach (UnicastIPAddressInformation ip in adapterProperties.UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        if (ip.Address.ToString() == ipAddr)
                        {
                            //Console.WriteLine($"当前出口 IPv4 地址：{ip.Address.ToString()}");
                            return adapter;
                        }
                    }
                }
            }
            return null;
        }

        public LocalDnsUtil(string ip)
        {
            InitDnsServers = new List<string>();
            NetworkInterface networkInterface = GetNetworkInterfaceByIp(ip);
            if (networkInterface == null)
            {
                throw new Exception();
            }
            Id = networkInterface.Id;
            InitDynamicDns = IsDNSAuto();


        }

        public void ChangeDns(List<string> dnsServers)
        {
            BackupDns();

            ManagementClass mClass = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection mObjCol = mClass.GetInstances();
            foreach (ManagementObject mObj in mObjCol)
            {
                if ((string)mObj["SettingID"] == Id)
                {
                    ManagementBaseObject mboDNS = mObj.GetMethodParameters("SetDNSServerSearchOrder");
                    if (mboDNS != null)
                    {
                        mboDNS["DNSServerSearchOrder"] = dnsServers.ToArray();
                        mObj.InvokeMethod("SetDNSServerSearchOrder", mboDNS, null);
                    }
                    break;
                }
            }
        }

        public void BackupDns()
        {
            InitDnsServers.Clear();
            InitDynamicDns = IsDNSAuto();
            if (!InitDynamicDns)
            {
                GetNetworkInterfaceById(Id).GetIPProperties().DnsAddresses.ToList().ForEach(dns =>
                {
                    InitDnsServers.Add(dns.ToString());
                });
            }
        }

        public void RecoverDns()
        {
            ManagementClass mClass = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection mObjCol = mClass.GetInstances();
            foreach (ManagementObject mObj in mObjCol)
            {
                if ((string)mObj["SettingID"] == Id)
                {
                    ManagementBaseObject mboDNS = mObj.GetMethodParameters("SetDNSServerSearchOrder");
                    if (InitDynamicDns)
                    {
                        if (mboDNS != null)
                        {
                            mboDNS["DNSServerSearchOrder"] = null;
                            mObj.InvokeMethod("SetDNSServerSearchOrder", mboDNS, null);
                        }
                    }
                    else
                    {
                        if (mboDNS != null)
                        {
                            mboDNS["DNSServerSearchOrder"] = InitDnsServers.ToArray();
                            mObj.InvokeMethod("SetDNSServerSearchOrder", mboDNS, null);
                        }
                    }
                    return;
                }
            }
        }


        private bool IsDNSAuto()
        {
            string path = "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces\\" + Id;
            string ns = (string)Registry.GetValue(path, "NameServer", null);
            if (string.IsNullOrEmpty(ns))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /*
        public NetworkInterface SearchOutbounds()
        {
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                IPInterfaceProperties adapterProperties = adapter.GetIPProperties();
                IPv4InterfaceProperties p = adapterProperties.GetIPv4Properties();

                foreach (UnicastIPAddressInformation ip in adapterProperties.UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        if (ip.Address.ToString() == "192.168.100.242")
                        {
                            Console.WriteLine($"当前出口 IPv4 地址：{ip.Address.ToString()}");
                            return adapter;
                        }
                    }
                }
            }
            return null;
        }
        */
    }
}
