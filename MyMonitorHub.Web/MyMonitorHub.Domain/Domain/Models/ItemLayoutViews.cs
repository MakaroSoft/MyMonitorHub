using System;
using System.Collections.Generic;

namespace MyMonitorHub.Domain.Models
{
    public class ItemLayoutView
    {
        public List<ItemLayoutViewDeviceGroup> DeviceGroups = new List<ItemLayoutViewDeviceGroup>();
        public List<String> Headers = new List<string>();
    }

    public class ItemLayoutViewDeviceGroup
    {
        public string Description;
        public int DeviceGroupId;
        public List<ItemLayoutViewDevice> Devices = new List<ItemLayoutViewDevice>();
    }

    public class ItemLayoutViewDevice
    {
        public List<string> Codes = new List<string>();
        public string Description;
        public int DeviceId;
        public ItemLayoutViewStatus Status;
    }

    public enum ItemLayoutViewStatus
    {
        Connected,
        Disconnected,
        Unknown
    }
}