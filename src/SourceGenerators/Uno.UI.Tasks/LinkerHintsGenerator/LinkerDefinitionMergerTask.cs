#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Transactions;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;

namespace Uno.UI.Tasks.LinkerHintsGenerator
{
	/// <summary>
	/// A task used to merge linker definition files and embed the result in an assembly
	/// </summary>
	public class LinkerDefinitionMergerTask_v0 : Microsoft.Build.Utilities.Task
	{
		private const MessageImportance DefaultLogMessageLevel

#if DEBUG
			= MessageImportance.High;
#else
			= MessageImportance.Low;
#endif

		[Required]
		public Microsoft.Build.Framework.ITaskItem[]? DefinitionFiles { get; set; }

		[Required]
		public string TargetDefinitionFile { get; set; } = "";


		public override bool Execute()
		{
			// Debugger.Launch();

			if (DefinitionFiles != null)
			{
				var doc = new XmlDocument();

				var xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);

				var root = doc.DocumentElement;
				doc.InsertBefore(xmlDeclaration, root);

				var linkerNode = doc.CreateElement(string.Empty, "linker", string.Empty);
				doc.AppendChild(linkerNode);

				foreach (var definition in DefinitionFiles)
				{
					Log.LogMessage(DefaultLogMessageLevel, $"Merging substitution file {definition.ItemSpec}");

					var defDoc = new XmlDocument();
					defDoc.Load(definition.ItemSpec);

					linkerNode.InnerXml += defDoc.DocumentElement.InnerXml;
				}

				string? existingContent;
				try
				{
					existingContent = File.ReadAllText(TargetDefinitionFile);
				}
				catch (FileNotFoundException)
				{
					// We use catch instead of File.Exists in case it could get deleted in between.
					existingContent = null;
				}

				using (var writer = new StringWriter())
				{
					doc.Save(writer);
					var output = writer.ToString();
					if (existingContent != output)
					{
						// Make sure to only write the file if there is a change.
						// This has a large effect on the whole build as writing the file here unnecessarily would cause 
						// _UnoEmbeddedResourcesInjection to run unnecessarily becase:
						// Input file "obj\Uno.UI.Skia\Debug\net7.0\ILLink.Substitutions.xml" is newer than output file "obj\Uno.UI.Skia\Debug\net7.0\Uno.UI.dll".
						// which will in turn cause many other things to be considered NOT up-to-date even when they should be up-to-date.
						File.WriteAllText(TargetDefinitionFile, output);
					}
				}
			}

			return true;
		}
	}
}
