using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Uno.Sdk.Tasks;

/// <summary>
/// Answers whether an app can reach the WebGPU backend, so a build that cannot is spared its native payload.
///
/// Reads compiled metadata rather than matching names in text: an unused reference is never emitted, so what an
/// assembly records is what its code actually binds to. A name occurring in a literal, a comment or a lookalike
/// type is not a reference and does not count.
///
/// Every managed assembly the app ships is examined, not just its own, so a registration coming from a package is
/// seen too.
/// </summary>
public sealed class DetectWebGpuUsage_v0 : Task
{
	// The assembly holding the backend, matched whole so a 'Contoso.WebGpuHelpers' does not read as ours. Its
	// interop sibling ('.Init') is deliberately not a signal: every Skia host references it for the WebGpu arm of
	// its context factory, an arm only a registered backend can reach, so that reference is in every app.
	private const string WebGpuAssemblyName = "Uno.UI.Composition.WebGpu";

	/// <summary>Every managed assembly the app ships.</summary>
	[Required]
	public ITaskItem[] Assemblies { get; set; } = Array.Empty<ITaskItem>();

	/// <summary>True when any of them can reach the backend, or when one could not be read.</summary>
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
			catch (BadImageFormatException)
			{
				// Not a PE at all, so it holds no reference to anything. A placeholder or empty file carrying a
				// .dll extension lands here, and calling that unknown would disable the trimming with no sign.
				reason = null;
			}
			catch (Exception e) when (e is IOException or UnauthorizedAccessException)
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

	/// <summary>True when this assembly is part of the backend itself, which cannot count as reaching it.</summary>
	private static bool IsWebGpuAssembly(string name)
		=> name == WebGpuAssemblyName || name.StartsWith(WebGpuAssemblyName + ".", StringComparison.Ordinal);

	/// <summary>The reason this assembly can reach the backend, or null when it cannot.</summary>
	private static string? Inspect(string path)
	{
		using var stream = File.OpenRead(path);
		using var pe = new PEReader(stream);
		if (!pe.HasMetadata)
		{
			return null;
		}

		var reader = pe.GetMetadataReader();
		if (reader.IsAssembly && IsWebGpuAssembly(reader.GetString(reader.GetAssemblyDefinition().Name)))
		{
			return null;
		}

		foreach (var handle in reader.AssemblyReferences)
		{
			var name = reader.GetString(reader.GetAssemblyReference(handle).Name);
			if (name == WebGpuAssemblyName)
			{
				return $"references {name}";
			}
		}

		return null;
	}
}
