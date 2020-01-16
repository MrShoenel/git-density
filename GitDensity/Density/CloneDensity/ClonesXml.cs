/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2020 Sebastian Hönel [sebastian.honel@lnu.se]
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
		/// <param name="skipValidation">If true, will skip validation. That
		/// leads to this method not performing any null-checks. If you obtain
		/// a serialized instance in order to read data from it, it's always
		/// recommended to perform validation, as then no (nested) element is
		/// allowed to be null.</param>
		/// <returns></returns>
		public static bool TryDeserialize(String pathToXml, out ClonesXml clonesXml, bool skipValidation = false)
		{
			try
			{
				XmlSerializer serializer = new XmlSerializer(typeof(ClonesXml));
				using (var reader = new StreamReader(pathToXml))
				{
					clonesXml = (ClonesXml)serializer.Deserialize(reader);
				}
			}
			catch
			{
				clonesXml = default(ClonesXml);
			}

			if (clonesXml is ClonesXml)
			{
				if (skipValidation)
				{
					return true;
				}

				return clonesXml.Checks is ClonesXmlCheck[] &&
					clonesXml.Checks.All(check =>
					{
						return check is ClonesXmlCheck &&
							check.Sets is ClonesXmlSet[] &&
							check.Sets.All(set =>
							{
								return set is ClonesXmlSet &&
									set.Blocks is ClonesXmlSetBlock[] &&
									set.Blocks.All(block => block is ClonesXmlSetBlock);
							});
					});
			}

			return false;
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
