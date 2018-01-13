using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitDensity.Util
{
	public static class DirectoryInfoExtensions
	{
		/// <summary>
		/// Clears a <see cref="DirectoryInfo"/>. That means that all files and folders
		/// in it will be deleted recursively, without the actual directory being deleted.
		/// </summary>
		/// <param name="directoryInfo">The director to clear/wipe/empty.</param>
		/// <returns>The same given <see cref="DirectoryInfo"/>.</returns>
		public static DirectoryInfo Clear(this DirectoryInfo directoryInfo)
		{
			foreach (var file in directoryInfo.GetFiles())
			{
				file.Delete();
			}
			foreach (var dir in directoryInfo.GetDirectories())
			{
				dir.Delete(true);
			}

			return directoryInfo;
		}
	}
}
