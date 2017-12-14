using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GitDensity.Density.CloneDensity
{
	[XmlRoot(ElementName = "clones")]
	public class ClonesXml
	{
		[XmlElement("check")]
		public ClonesXmlCheck[] Checks { get; set; }

		public static void Test()
		{
			XmlSerializer serializer = new XmlSerializer(typeof(ClonesXml));
			var reader = new StreamReader(@"C:\Users\Admin\Desktop\src\allClones.xml");
			var c = (ClonesXml)serializer.Deserialize(reader);
			reader.Close();
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
	}


}
