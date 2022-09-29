#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ResourceConverter
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Converting iOS resource file [{0}] to Android resource file [{1}]", args[0], args[1]);

			var lines = File.ReadAllLines(args[0]);

			using (var w = XmlTextWriter.Create(args[1], new XmlWriterSettings() { Indent = true, Encoding = Encoding.UTF8 }))
			{
				w.WriteStartDocument(true);

				w.WriteStartElement("resources");

				foreach (var line in lines)
				{
					w.WriteStartElement("string");

					var sep = line.IndexOf('=');
					var key = line.Substring(0, sep).Trim().TrimStart('\"').TrimEnd('\"');

					var value = line
						.Substring(sep + 1, line.Length-sep-2)
						.Trim()
						.TrimStart('\"')
						.TrimEnd('\"')
						.Replace("\'", "\\'");

					w.WriteAttributeString("name", key);
					w.WriteString(value);

					w.WriteEndElement();
				}
			}
		}
	}
}
