#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Transactions;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;

namespace Uno.UI.Tasks.EmbeddedResourceInjector
{
	/// <summary>
	/// A task used to merge linker definition files and embed the result in an assembly
	/// </summary>
	public class EmbeddedResourceInjectorTask_v0 : Microsoft.Build.Utilities.Task
	{
		private const MessageImportance DefaultLogMessageLevel

#if DEBUG
			= MessageImportance.High;
#else
			= MessageImportance.Low;
#endif

		[Required]
		public Microsoft.Build.Framework.ITaskItem[]? EmbeddedResources { get; set; }

		[Required]
		public string TargetAssembly { get; set; } = "";

		[Required]
		public Microsoft.Build.Framework.ITaskItem[]? ReferencePath { get; set; }


		public override bool Execute()
		{
			try
			{
				// Debugger.Launch();

				if (EmbeddedResources?.Length > 0)
				{
					Log.LogMessage(DefaultLogMessageLevel, $"Writing embedded files to {TargetAssembly}");

					var resolver = new DefaultAssemblyResolver();
					foreach (var path in BuildReferencesPaths())
					{
						resolver.AddSearchDirectory(path);
					}

					using (var asm = AssemblyDefinition.ReadAssembly(TargetAssembly, new ReaderParameters() { AssemblyResolver = resolver, ReadSymbols = true, ReadWrite = true }))
					{
						foreach (var embeddedResource in EmbeddedResources)
						{
							var logicalName = embeddedResource.GetMetadata("LogicalName");

							Log.LogMessage(MessageImportance.Low, $"Embedding file {embeddedResource.ItemSpec} as {logicalName}");

							// Remove existing resources with the same name
							var existingResources = asm.MainModule.Resources
								.OfType<EmbeddedResource>()
								.Where(r => r.Name == logicalName)
								.ToArray();

							foreach (var existingResource in existingResources)
							{
								asm.MainModule.Resources.Remove(existingResource);
							}

							// Add the new merged content
							asm.MainModule.Resources.Add(new EmbeddedResource(logicalName, ManifestResourceAttributes.Public, File.ReadAllBytes(embeddedResource.GetMetadata("FullPath"))));
						}

						asm.Write(new WriterParameters() { WriteSymbols = true });
					}

					WaitForUnlockedFile(TargetAssembly);
					WaitForUnlockedFile(Path.ChangeExtension(TargetAssembly, "pdb"));
				}

				return true;
			}
			catch (Exception e)
			{
				// Require because the task is running out of process
				// and can't marshal non-CLR known exceptions.
				throw new Exception(e.ToString());
			}
		}

		private string[] BuildReferencesPaths() => ReferencePath
				.Select(p => Path.GetDirectoryName(p.ItemSpec))
				.Distinct()
				.ToArray();

		private void WaitForUnlockedFile(string filePath)
		{
			var sw = Stopwatch.StartNew();

			while (sw.Elapsed < TimeSpan.FromSeconds(5))
			{
				try
				{
					File.OpenWrite(filePath).Dispose();

					break;
				}
				catch
				{
					Log.LogMessage(MessageImportance.Low, $"Waiting for availability for {TargetAssembly}");
				}

				Thread.Sleep(100);
			}
		}
	}
}
