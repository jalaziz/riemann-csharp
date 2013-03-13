using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;

namespace Riemann
{
    public static class Utility
    {
        public static string GetHostName()
        {
            var properties = IPGlobalProperties.GetIPGlobalProperties();
            return string.Format("{0}.{1}", properties.HostName, properties.DomainName);
        }
    }
}
