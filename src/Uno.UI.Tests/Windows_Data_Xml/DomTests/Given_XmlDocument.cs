using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Data.Xml.Dom;

namespace Uno.UI.Tests.Windows_Data_Xml.DomTests
{
	[TestClass]
	public class Given_XmlDocument
	{
		[TestMethod]
		public void When_LoadXml()
		{
			var document = new XmlDocument();
			document.LoadXml(GetTestXml());
			Assert.HasCount(1, document.ChildNodes);
			Assert.IsInstanceOfType(document.FirstChild, typeof(XmlElement));
			Assert.AreEqual("chapter", document.FirstChild.NodeName);
		}

		[TestMethod]
		public void When_Enumerate_ChildNodes()
		{
			var document = new XmlDocument();
			document.LoadXml(GetTestXml());
			var childNodes = document.FirstChild.ChildNodes;
			foreach (var childNode in childNodes)
			{
				Assert.IsInstanceOfType(childNode, typeof(XmlElement));
			}
			Assert.AreEqual("title", childNodes[0].NodeName);
			Assert.AreEqual("para", childNodes[(int)childNodes.Length - 1].NodeName);
		}

		[TestMethod]
		public void When_XPath_Search()
		{
			var document = new XmlDocument();
			document.LoadXml(GetTestXml());
			var node = document.SelectSingleNode("/chapter/para/aside");
			Assert.IsInstanceOfType(node, typeof(XmlElement));
			var attributeText = document.SelectSingleNode("/chapter/para/aside[@alt]/text()");
			Assert.IsInstanceOfType(attributeText, typeof(XmlText));
		}

		[TestMethod]
		public void When_SetAttribute()
		{
			var badgeXml = new XmlDocument();
			badgeXml.LoadXml("<badge value=\"\" />");
			var badgeAttributes = badgeXml.GetElementsByTagName("badge");
			((XmlElement)badgeAttributes[0]).SetAttribute("value", "26");
			var xml = badgeXml.GetXml();
			Assert.AreEqual("<badge value=\"26\" />", xml);
		}

		private string GetTestXml()
		{
			using var stream = typeof(Given_XmlDocument).Assembly.GetManifestResourceStream("Uno.UI.Tests.Windows_Data_Xml.DomTests.basictest.xml");
			using var streamReader = new StreamReader(stream);
			return streamReader.ReadToEnd();
		}
	}
}
