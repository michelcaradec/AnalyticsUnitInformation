using Microsoft.Analytics.Interfaces;
using Microsoft.Analytics.Types.Sql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Management;

namespace AnalyticsUnitInformation
{
	public static class WMIInfos
	{
		public static bool IsWin32_Process(string name)
		{
			return name.StartsWith("Win32_Process[");
		}

		private static IEnumerable<string> GetProperties(string collection)
		{
			var wmiSearch = new ManagementObjectSearcher(string.Format("Select * From {0}", collection));

			int number = 0;
			foreach (var wmiObject in wmiSearch.Get())
			{
				number++;

				foreach (var property in wmiObject.Properties)
				{
					yield return string.Format(
						"{0}[{1}]/{2}={3}",
						collection,
						number,
						property.Name,
						(property.Value ?? string.Empty)
					);
				}
			}
		}

		public static IEnumerable<string> Properties
		{
			get
			{
				return GetProperties("Win32_ComputerSystem")
					.Concat(GetProperties("Win32_PhysicalMemory"))
					.Concat(GetProperties("Win32_DiskDrive"))
					.Concat(GetProperties("Win32_Processor"))
					.Concat(GetProperties("Win32_Process"));
			}
		}
	}
}
