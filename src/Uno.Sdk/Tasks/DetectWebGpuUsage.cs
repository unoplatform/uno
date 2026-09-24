using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Uno.Sdk.Tasks;

/// <summary>
/// Answers whether an app can reach the WebGPU native library, so a build that cannot is spared its payload.
///
/// Reads compiled metadata rather than matching names in text: an unused reference is never emitted, so what an
/// assembly records is what its code actually binds to. All WebGPU plumbing is ours, so binding to one of our
/// WebGPU assemblies is the whole question - a name occurring in a literal, a comment or a lookalike type is not
/// a reference and does not count.
/// </summary>
public sealed class DetectWebGpuUsage_v0 : Task
{
	// The Uno backend assemblies. Matched on the full name or a dotted child ('.Init'), never as a substring, so a
	// 'Contoso.WebGpuHelpers' does not read as ours.
	private const string WebGpuAssemblyName = "Uno.UI.Composition.WebGpu";

	/// <summary>The app's own assemblies: its head, plus anything built alongside it.</summary>
	[Required]
	public ITaskItem[] Assemblies { get; set; } = Array.Empty<ITaskItem>();

	/// <summary>True when any of them can reach the native, or when one could not be read.</summary>
	[Output]
	public bool CanReachWebGpu { get; private set; }

	/// <summary>What decided it, for the build log.</summary>
	[Output]
	public string Reason { get; private set; } = string.Empty;

	public override bool Execute()
	{
		foreach (var item in Assemblies)
		{
			var path = item.GetMetadata("FullPath");
			if (!File.Exists(path))
			{
				continue;
			}

			string? reason;
			try
			{
				reason = Inspect(path);
			}
			catch (Exception e) when (e is BadImageFormatException or IOException or UnauthorizedAccessException)
			{
				// Unreadable means unknown, and unknown has to keep the payload.
				CanReachWebGpu = true;
				Reason = $"{Path.GetFileName(path)} could not be read ({e.GetType().Name})";
				return true;
			}

			if (reason is not null)
			{
				CanReachWebGpu = true;
				Reason = $"{Path.GetFileName(path)} {reason}";
				return true;
			}
		}

		return true;
	}

	/// <summary>The reason this assembly can reach the native, or null when it cannot.</summary>
	private static string? Inspect(string path)
	{
		using var stream = File.OpenRead(path);
		using var pe = new PEReader(stream);
		if (!pe.HasMetadata)
		{
			return null;
		}

		var reader = pe.GetMetadataReader();

		foreach (var handle in reader.AssemblyReferences)
		{
			var name = reader.GetString(reader.GetAssemblyReference(handle).Name);
			if (name == WebGpuAssemblyName || name.StartsWith(WebGpuAssemblyName + ".", StringComparison.Ordinal))
			{
				return $"references {name}";
			}
		}

		return null;
	}
}
