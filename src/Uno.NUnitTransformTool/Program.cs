using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Transactions;
using System.Xml;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace Uno.ReferenceImplComparer
{
	class Program
	{
		static int Main(string[] args)
		{
			switch (args[0])
			{
				case "list-failed":
					return ListFailedTests(args[1], args[2]);
			}

			return 0;
		}

		private static int ListFailedTests(string inputFile, string outputFile)
		{
			var doc = new XmlDocument();
			doc.LoadXml(File.ReadAllText(inputFile));

			var failedNodes = doc.SelectNodes("//test-case[@result='Failed']")!;

			var builder = new StringBuilder();
			foreach(var failedNode in failedNodes.OfType<XmlElement>())
			{
				builder.AppendLine(failedNode.GetAttribute("fullname"));
			}
				
			// Add a dummy line to be used to rerun the test running in case 
			// tests get canceled. This condition happens when running nunit-console
			// and the retry attribute which markes runners as cancelled and fails any
			// subsequent test.
			builder.AppendLine("invalid-test-for-retry");

			File.WriteAllText(outputFile, builder.ToString());

			return 0;
		}
	}
}
