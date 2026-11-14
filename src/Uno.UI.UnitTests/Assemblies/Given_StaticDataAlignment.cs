#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Assemblies;

[UITestClass]
public class Given_StaticDataAlignment
{
	// CoreCLR checks this alignment only when JIT optimizations are off (under a debugger), so nothing else
	// catches a post-compile rewrite of the assembly that drops it.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25626")]
	public unsafe void When_Span_Data_Is_Read_Then_It_Is_Aligned()
	{
		var testAssembly = typeof(Given_StaticDataAlignment).Assembly;
		if (!testAssembly.GetManifestResourceNames().Contains("ILLink.Substitutions.xml"))
		{
#if DEBUG
			Assert.Inconclusive("Only Release builds rewrite assemblies after compile; run this test in Release.");
#else
			Assert.Fail("The post-compile rewrite (EmbeddedResourceInjectorTask) didn't run on this assembly, so this test would check nothing.");
#endif
		}

		var checkedFields = new Dictionary<Assembly, int>();
		var misaligned = new List<string>();

		foreach (var assembly in GetUnoAssemblies())
		{
			if (assembly.GetType("<PrivateImplementationDetails>") is not { } dataType)
			{
				continue;
			}

			foreach (var field in dataType.GetFields(BindingFlags.Static | BindingFlags.NonPublic))
			{
				// Only the compiler's __StaticArrayInitTypeSize=N_Align=M types carry an alignment, as their packing size.
				if ((field.Attributes & FieldAttributes.HasFieldRVA) == 0 || field.FieldType.DeclaringType != dataType)
				{
					continue;
				}

				var alignment = field.FieldType.StructLayoutAttribute?.Pack ?? 1;
				if (alignment <= 1)
				{
					continue;
				}

				checkedFields[assembly] = checkedFields.GetValueOrDefault(assembly) + 1;

				// Read as bytes, which never throws, so every misaligned field is listed rather than only the first.
				fixed (byte* data = RuntimeHelpers.CreateSpan<byte>(field.FieldHandle))
				{
					if ((nint)data % alignment is var offset and not 0)
					{
						misaligned.Add($"{assembly.GetName().Name}: {field.Name} ({field.FieldType.Name}) is {offset} byte(s) off its {alignment}-byte alignment");
					}
				}
			}
		}

		Assert.IsTrue(checkedFields.ContainsKey(testAssembly), "The span data in AlignedData wasn't found, so this test checks nothing.");
		Assert.IsEmpty(
			misaligned,
			"Misaligned span data makes RuntimeHelpers.CreateSpan throw \"The field is invalid for initializing array or span\" under a debugger. "
			+ "A post-compile IL rewrite usually causes it: check EmbeddedResourceInjectorTask and the Mono.Cecil version in src/Directory.Packages.props."
			+ Environment.NewLine + string.Join(Environment.NewLine, misaligned));
	}

	// Uno's assemblies share Uno.UI's version (src/Directory.Build.props); third-party dependencies don't.
	private static IEnumerable<Assembly> GetUnoAssemblies()
	{
		var version = typeof(UIElement).Assembly.GetName().Version;
		var seen = new HashSet<string>(StringComparer.Ordinal);
		var pending = new Queue<Assembly>([typeof(Given_StaticDataAlignment).Assembly]);

		while (pending.TryDequeue(out var assembly))
		{
			yield return assembly;

			foreach (var reference in assembly.GetReferencedAssemblies().Where(r => r.Version == version && seen.Add(r.Name!)))
			{
				pending.Enqueue(Assembly.Load(reference));
			}
		}
	}

	// Aligned span data of every size, mixed with odd-sized blobs, so a rewrite that packs the data without
	// padding misaligns some of it whatever order the fields end up in.
	private static class AlignedData
	{
		internal static ReadOnlySpan<char> Chars1 => [':', '.'];
		internal static ReadOnlySpan<char> Chars2 => ['(', ')', '['];
		internal static ReadOnlySpan<char> Chars3 => ['x', 'y', 'z', 'w', 'v'];
		internal static ReadOnlySpan<int> Ints1 => [1, 2];
		internal static ReadOnlySpan<int> Ints2 => [3, 4, 5];
		internal static ReadOnlySpan<int> Ints3 => [6, 7, 8, 9, 10];
		internal static ReadOnlySpan<long> Longs1 => [11, 12];
		internal static ReadOnlySpan<long> Longs2 => [13, 14, 15];
		internal static ReadOnlySpan<long> Longs3 => [16, 17, 18, 19, 20];
		internal static ReadOnlySpan<byte> Odd3 => [21, 22, 23];
		internal static ReadOnlySpan<byte> Odd5 => [24, 25, 26, 27, 28];
		internal static ReadOnlySpan<byte> Odd7 => [29, 30, 31, 32, 33, 34, 35];
		internal static ReadOnlySpan<byte> Odd9 => [36, 37, 38, 39, 40, 41, 42, 43, 44];
		internal static ReadOnlySpan<byte> Odd11 => [45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55];
	}
}
