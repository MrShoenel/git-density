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
using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace GitDensity.Density.CloneDensity
{
	[XmlRoot(ElementName = "clones")]
	public class ClonesXml
	{
		[XmlElement("check")]
		public ClonesXmlCheck[] Checks { get; set; }

		/// <summary>
		/// Attempts to deserialize the result of running the default clone detection.
		/// </summary>
		/// <param name="pathToXml"></param>
		/// <param name="clonesXml"></param>
		/// <returns></returns>
		public static bool TryDeserialize(String pathToXml, out ClonesXml clonesXml)
		{
			try
			{
				XmlSerializer serializer = new XmlSerializer(typeof(ClonesXml));
				using (var reader = new StreamReader(pathToXml)) {
					clonesXml = (ClonesXml)serializer.Deserialize(reader);
					return true;
				}
			}
			catch
			{
				clonesXml = default(ClonesXml);
				return false;
			}
		}
	}

	public class ClonesXmlCheck
	{
		[XmlAttribute(AttributeName = "config")]
		public String Config { get; set; }

		[XmlElement("set")]
		public ClonesXmlSet[] Sets { get; set; }
	}

	public class ClonesXmlSet
	{
		[XmlAttribute(AttributeName = "id")]
		public Int32 Id { get; set; }

		[XmlAttribute(AttributeName = "count")]
		public Int32 Count { get; set; }

		[XmlAttribute(AttributeName = "string")]
		public String String { get; set; }

		[XmlElement("block")]
		public ClonesXmlSetBlock[] Blocks { get; set; }
	}

	public class ClonesXmlSetBlock
	{
		[XmlAttribute(AttributeName = "source")]
		public String Source { get; set; }

		[XmlAttribute(AttributeName = "start")]
		public Int32 Start { get; set; }

		[XmlAttribute(AttributeName = "end")]
		public Int32 End { get; set; }

		/// <summary>
		/// Returns an <see cref="Int32[]"/> of affected line numbers.
		/// </summary>
		[XmlIgnore]
		public UInt32[] LineNumbers
		{
			get => Enumerable.Range(this.Start, 1 + this.End - this.Start).Select(i => (UInt32)i).ToArray();
		}
	}
}
