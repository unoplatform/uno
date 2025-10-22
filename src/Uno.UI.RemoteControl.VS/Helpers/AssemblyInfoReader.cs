using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Uno.UI.RemoteControl.VS.Helpers;

internal static class AssemblyInfoReader
{
	/// <summary>
	/// Reads MVID and TargetPlatformAttribute.PlatformName (if present) from an assembly file without loading it.
	/// </summary>
	public static (Guid Mvid, string? PlatformName) Read(string assemblyPath)
	{
		using var fs = File.OpenRead(assemblyPath);
		using var pe = new PEReader(fs, PEStreamOptions.LeaveOpen);
		var md = pe.GetMetadataReader();

		// MVID
		var mvid = md.GetGuid(md.GetModuleDefinition().Mvid);

		string? platformName = null;
		// [assembly: TargetPlatformAttribute("Desktop1.0")]
		// Namespace: System.Runtime.Versioning
		foreach (var caHandle in md.GetAssemblyDefinition().GetCustomAttributes())
		{
			var ca = md.GetCustomAttribute(caHandle);
			if (ca.Constructor.Kind != HandleKind.MemberReference)
			{
				continue;
			}

			var mr = md.GetMemberReference((MemberReferenceHandle)ca.Constructor);
			var ct = md.GetTypeReference((TypeReferenceHandle)mr.Parent);
			var typeName = md.GetString(ct.Name);
			var typeNs = md.GetString(ct.Namespace);

			if (typeName == "TargetPlatformAttribute" && typeNs == "System.Runtime.Versioning")
			{
				var valReader = md.GetBlobReader(ca.Value);
				// Blob layout: prolog (0x0001), then fixed args
				if (valReader.ReadUInt16() == 1) // prolog
				{
					platformName = valReader.ReadSerializedString();
				}
				break;
			}
		}

		return (mvid, platformName);
	}
}
