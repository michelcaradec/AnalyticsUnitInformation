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
		#region WMI

		private const string Win32_ProcessCollection = "Win32_Process";

		public static bool IsWin32_Process(string name)
		{
			return name.StartsWith(Win32_ProcessCollection + "[");
		}

		private static string FormatWMI(string collection, string name, object value, int number = 1)
		{
			return string.Format(
				"{0}[{1}]/{2}={3}",
				collection,
				number,
				name,
				(value ?? string.Empty)
			);
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
					yield return FormatWMI(collection, property.Name, property.Value, number);
				}
			}
		}

		#endregion
		#region Memory Stress

		private const string MemoryStressCollection = "MemoryStress";

		public static bool IsMemoryStress(string name)
		{
			return name.StartsWith(MemoryStressCollection);
		}

		private static IEnumerable<string> GetMemoryStressProperties()
		{
			int chunkCount = DefaultChunkCount;
			ulong chunkSize = DefaultChunkSize;
			ulong size = StressMemory(chunkCount, chunkSize);

			var memoryStress
				= new[]
				{
						FormatWMI(MemoryStressCollection, "Allocated", size),
						FormatWMI(MemoryStressCollection, "ChunkCount", chunkCount, 2),
						FormatWMI(MemoryStressCollection, "ChunkSize", chunkSize, 3)
				};

			return memoryStress;
		}

		private const int DefaultChunkCount = 10;
		private const ulong DefaultChunkSize = 100 * 1024 * 1024;

		private static ulong StressMemory(int chunkCount = DefaultChunkCount, ulong chunkSize = DefaultChunkSize)
		{
			ulong totalSize = 0;

			var rand = new Random();
			var buffers = new List<byte[]>(chunkCount);
			for (int idx = 0; idx < chunkCount; idx++)
			{
				try
				{
					var buffer = new byte[chunkSize];
					rand.NextBytes(buffer);
					buffers.Add(buffer);
				}
				catch (OutOfMemoryException)
				{
					break;
				}

				totalSize += chunkSize;
			}

			return totalSize;
		}

		#endregion

		public static IEnumerable<string> Properties
		{
			get
			{
				// Collect Win32_ComputerSystem/TotalPhysicalMemory (with initial 2 GB or RAM)
				var computerSystem = GetProperties("Win32_ComputerSystem").ToList();

				return computerSystem
					// Collect Win32_PhysicalMemory/Capacity
					.Concat(GetMemoryStressProperties())    // Allocate some memory
					.Concat(GetProperties("Win32_PhysicalMemory"))
					.Concat(GetProperties("Win32_DiskDrive"))
					.Concat(GetProperties("Win32_Processor"))
					.Concat(GetProperties(Win32_ProcessCollection));
			}
		}
	}
}
