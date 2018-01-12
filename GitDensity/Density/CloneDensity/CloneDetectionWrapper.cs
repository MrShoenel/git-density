/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2018 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project GitDensity. All files in this project,
/// if not noted otherwise, are licensed under the MIT-license.
///
/// ---------------------------------------------------------------------------------
///
/// Permission is hereby granted, free of charge, to any person obtaining a
/// copy of this software and associated documentation files (the "Software"),
/// to deal in the Software without restriction, including without limitation
/// the rights to use, copy, modify, merge, publish, distribute, sublicense,
/// and/or sell copies of the Software, and to permit persons to whom the
/// Software is furnished to do so, subject to the following conditions:
///
/// The above copyright notice and this permission notice shall be included in all
/// copies or substantial portions of the Software.
///
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
/// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
/// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
///
/// ---------------------------------------------------------------------------------
///
using GitDensity.Util;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

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
					FileName = Program.Configuration.PathToCloneDetectionBinary,
					Arguments = $"{Program.Configuration.CloneDetectionArgs} -l {this.Language.ToString()} " +
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

					proc.WaitForExit();
					if (proc.ExitCode != 0)
					{
						logger.LogError("Clone detection failed with Exit-Code {0}. Output:\n\n{1}",
							proc.ExitCode, stdErr);
						throw new InvalidOperationException(
							"The process exited with a non-zero exitcode.");
					}
					logger.LogInformation("Clone detection output:\n\n{0}", stdOut);

					if (!ClonesXml.TryDeserialize(tempFile, out ClonesXml clonesXml))
					{
						logger.LogError("Cannot de-serialize output of Clone detection.");
						throw new IOException("Cannot deserialize the analysis' result.");
					}

					return clonesXml.Checks.SelectMany(checks => checks.Sets);
				}
			}
			finally
			{
				File.Delete(tempFile);
			}
		}
	}
}
