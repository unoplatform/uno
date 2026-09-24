using System;
using System.Collections.Generic;
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
/// assembly records is what its code actually binds to. Three things count as reach, and any one keeps the payload:
/// a reference to a WebGPU assembly, a P/Invoke into the native module, or registering a graphics backend at all
/// (which provider it registers cannot be known here, and it may well be a WebGPU one we did not write).
/// </summary>
public sealed class DetectWebGpuUsage_v0 : Task
{
	// The Uno backend assemblies. Matched on the full name or a dotted child ('.Init'), never as a substring, so a
	//'Contoso.WebGpuHelpers' does not read as ours.
	private const string WebGpuAssemblyName = "Uno.UI.Composition.WebGpu";

	// The native the payload provides. A P/Invoke names it without 'lib' or a file extension, but normalize anyway
	// so an explicit "libwgpu_native.so" is recognized too.
	private static readonly string[] NativeModules = { "webgpu", "wgpu_native" };

	// Registering a backend goes through this extension method; the call site records both names.
	private const string HostBuilderExtensionsType = "UnoPlatformHostBuilderExtensions";
	private const string HostBuilderExtensionsNamespace = "Uno.UI.Hosting";
	private const string RegisterBackendMethod = "GraphicsBackend";

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

		foreach (var handle in reader.MethodDefinitions)
		{
			var method = reader.GetMethodDefinition(handle);
			if ((method.Attributes & System.Reflection.MethodAttributes.PinvokeImpl) == 0)
			{
				continue;
			}

			var import = method.GetImport();
			if (import.Module.IsNil)
			{
				continue;
			}

			var module = Normalize(reader.GetString(reader.GetModuleReference(import.Module).Name));
			foreach (var native in NativeModules)
			{
				if (module == native)
				{
					return $"P/Invokes '{module}'";
				}
			}
		}

		foreach (var handle in reader.MemberReferences)
		{
			var member = reader.GetMemberReference(handle);
			if (reader.GetString(member.Name) != RegisterBackendMethod
				|| member.Parent.Kind != HandleKind.TypeReference)
			{
				continue;
			}

			var declaring = reader.GetTypeReference((TypeReferenceHandle)member.Parent);
			if (reader.GetString(declaring.Name) == HostBuilderExtensionsType
				&& reader.GetString(declaring.Namespace) == HostBuilderExtensionsNamespace)
			{
				return "registers a graphics backend";
			}
		}

		return null;
	}

	/// <summary>A module name reduced to what the loader matches on: no 'lib' prefix, no file extension.</summary>
	private static string Normalize(string module)
	{
		var name = Path.GetFileNameWithoutExtension(module);
		if (name.StartsWith("lib", StringComparison.OrdinalIgnoreCase) && name.Length > 3)
		{
			name = name.Substring(3);
		}

		return name.ToLowerInvariant();
	}
}
