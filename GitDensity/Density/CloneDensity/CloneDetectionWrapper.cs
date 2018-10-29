/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2018 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project GitDensity. All files in this project,
/// if not noted otherwise, are licensed under the GPLv3-license. You will
/// find a copy of this file in the project's root directory.
///
/// Note that the license changed from MIT to GPLv3. In general, the license
/// from the latest public commit applies.
///
/// ---------------------------------------------------------------------------------
///
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Util;
using Util.Logging;

namespace GitDensity.Density.CloneDensity
{
	/// <summary>
	/// A wrapper for the Java-based clone-detection we use. It is configured
	/// by <see cref="Program.Configuration"/> and its related properties. The
	/// wrapper executes the clone detection and deserializes the results. This
	/// wrapper works with the <see cref="DefaultCloneDetectionSupportedLangs"/>.
	/// </summary>
	internal class CloneDetectionWrapper
	{
		private static BaseLogger<CloneDetectionWrapper> logger =
			Program.CreateLogger<CloneDetectionWrapper>();

		private const String stdErrNoMatchingFiles = "No matching files found under";

		/// <summary>
		/// The directory to analyze for clones.
		/// </summary>
		public DirectoryInfo Directory { get; protected internal set; }

		/// <summary>
		/// The programming language to check.
		/// </summary>
		public ProgrammingLanguage Language { get; protected internal set; }

		protected virtual DirectoryInfo tempDirectory { get; set; }

		/// <summary>
		/// Initializes a wrapper that can facilitate an external application using
		/// a wrapped <see cref="Process"/>.
		/// </summary>
		/// <param name="directory">The directory to start detecting clones in.</param>
		/// <param name="language">The programming language to analyze in the directory.</param>
		public CloneDetectionWrapper(DirectoryInfo directory, ProgrammingLanguage language, DirectoryInfo tempDirectory = null)
		{
			this.Directory = directory;
			this.Language = language;
			this.tempDirectory = tempDirectory ?? new DirectoryInfo(Path.GetTempPath());
		}

		/// <summary>
		/// Performs a clone detection in the specified directory. The result is written to
		/// a temporary file that is finally deleted. The essential information is returned
		/// as <see cref="IEnumerable{ClonesXmlSet}"/>.
		/// The <see cref="Program.Configuration"/> is used to configure the clone detection.
		/// Dynamically, only the language, temporary file and directory is passed.
		/// </summary>
		/// <returns>An enumerable list of Sets, where each set contains blocks.</returns>
		public IEnumerable<ClonesXmlSet> PerformCloneDetection()
		{
			var tempFile = $"{Path.Combine(this.tempDirectory.FullName, Path.GetRandomFileName())}.xml";

			try
			{
				using (var proc = Process.Start(new ProcessStartInfo
				{
					WorkingDirectory = this.Directory.FullName,
					FileName = Path.GetFullPath(Program.Configuration.PathToCloneDetectionBinary),
					Arguments = $"{Program.Configuration.CloneDetectionArgs} -l {this.Language.ToString().ToLowerInvariant()} " +
						$"-t {tempFile} -s \"{this.Directory.FullName}\"",
					RedirectStandardError = true,
					RedirectStandardOutput = true,
					UseShellExecute = false
				}))
				{
					logger.LogDebug("Starting clone-detection with args: {0} {1}",
						proc.StartInfo.FileName, proc.StartInfo.Arguments);
					logger.LogDebug("Writing temporary results to file: {0}", tempFile);

					var stdOut = proc.StandardOutput.ReadToEnd();
					var stdErr = proc.StandardError.ReadToEnd();
					
					var hasExited = proc.WaitForExit((Int32)TimeSpan.FromMinutes(2).TotalMilliseconds);
					if (!hasExited || proc.ExitCode != 0)
					{
						logger.LogError("Clone detection failed with Exit-Code {0}. Output:\n\n{1}",
							proc.ExitCode, stdErr);
						throw new InvalidOperationException(
							"The process exited with a non-zero exitcode.");
					}

					if (!hasExited)
					{
						proc.Kill();
						throw new InvalidOperationException(
							"The clone detection timed out.");
					}

					logger.LogTrace("Clone detection output:\n\n{0}{1}", stdOut, stdErr);

					if (!File.Exists(tempFile) || !ClonesXml.TryDeserialize(tempFile, out ClonesXml clonesXml))
					{
						// It might be the case that there had been no relevant/matching files,
						// in which our used clone detection does not write an output. We can
						// detect this case by looking into stdErr:
						if (stdErr.Contains(stdErrNoMatchingFiles))
						{
							return Enumerable.Empty<ClonesXmlSet>();
						}

						logger.LogError("Cannot de-serialize output of Clone detection.");
						throw new IOException("Cannot deserialize the analysis' result.");
					}

					return clonesXml.Checks.SelectMany(checks => checks.Sets);
				}
			}
			finally
			{
				if (File.Exists(tempFile))
				{
					File.Delete(tempFile);
				}
			}
		}
	}
}
