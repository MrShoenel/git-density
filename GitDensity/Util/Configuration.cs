using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitDensity.Util
{
	public class Configuration
	{
		/// <summary>
		/// The absolute path to the executable for handling clone detection.
		/// This should not contain any arguments, use the other property
		/// for that.
		/// </summary>
		[JsonProperty(Required = Required.Always, PropertyName = "pathToCloneDetectionBinary")]
		public String PathToCloneDetectionBinary { get; set; }

		/// <summary>
		/// This should contain any args that are passed to the clone-detection.
		/// If a Jar-file is used, than those args should be placed here as
		/// well, e.g. "-jar /path/to/cloneDetection.jar", while the path
		/// should then only point to the Java-binary.
		/// </summary>
		[JsonProperty(Required = Required.Default, PropertyName = "cloneDetectionArgs")]
		public String CloneDetectionArgs { get; set; }

		/// <summary>
		/// An example that is used to create an initial configuration, if
		/// none exists.
		/// </summary>
		public static readonly Configuration Example = new Configuration
		{
			PathToCloneDetectionBinary = @"C:\temp\binary.exe",
			CloneDetectionArgs = "-myarg 2 -bla true"
		};
	}
}
