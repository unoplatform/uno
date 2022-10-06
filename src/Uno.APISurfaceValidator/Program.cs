#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Mono.Cecil;

namespace Uno.APISurfaceValidator
{
	class Program
	{
		private static (string path, string targetAssembly, string pattern)[] _contractsToValidate = new[]
		{
			(@"C:\Program Files (x86)\Windows Kits\10\References\10.0.19041.0\Windows.Foundation.FoundationContract\4.0.0.0\Windows.Foundation.FoundationContract.winmd", "Uno.Foundation.dll", "*"),
			(@"C:\Program Files (x86)\Windows Kits\10\References\10.0.19041.0\Windows.Foundation.UniversalApiContract\10.0.0.0\Windows.Foundation.UniversalApiContract.winmd", "Uno.dll", "-Windows.UI.Xaml"),
			(@"C:\Program Files (x86)\Windows Kits\10\References\10.0.19041.0\Windows.Phone.PhoneContract\1.0.0.0\Windows.Phone.PhoneContract.winmd", "Uno.dll", "*"),
			(@"C:\Program Files (x86)\Windows Kits\10\References\10.0.19041.0\Windows.Networking.Connectivity.WwanContract\2.0.0.0\Windows.Networking.Connectivity.WwanContract.winmd", "Uno.dll", "*"),
			(@"C:\Program Files (x86)\Windows Kits\10\References\10.0.19041.0\Windows.ApplicationModel.Calls.CallsPhoneContract\5.0.0.0\Windows.ApplicationModel.Calls.CallsPhoneContract.winmd", "Uno.dll", "*"),
			(@"C:\Program Files (x86)\Windows Kits\10\References\10.0.19041.0\Windows.Foundation.UniversalApiContract\10.0.0.0\Windows.Foundation.UniversalApiContract.winmd", "Uno.UI.dll", "+Windows.UI.Xaml")
		};

		static int Main(string[] args)
		{
			var hasErrors = false;

			var filePath = UnpackArchive(args[0]);

			Console.WriteLine($"Validating package {args[0]}");

			foreach (var contract in _contractsToValidate)
			{
				Console.WriteLine($"Validating {Path.GetFileName(contract.path)} :");

				var referenceAssembly = ReadAssemblyDefinition(contract.path);
				var assembly = ReadAssemblyDefinition(Path.Combine(filePath, "lib", "netstandard2.0", contract.targetAssembly));

				hasErrors |= CompareAssemblies(referenceAssembly, assembly, contract.pattern, contract.targetAssembly);
			}

			return hasErrors ? 1 : 0;
		}

		private static bool CompareAssemblies(AssemblyDefinition referenceAssembly, AssemblyDefinition assembly, string pattern, string identifier)
		{
			var hasError = false;

			var referenceTypes = referenceAssembly.MainModule.GetTypes();
			var types = assembly.MainModule.GetTypes().ToDictionary(t => t.FullName);

			Func<TypeDefinition, bool> predicate = pattern[0] switch
			{
				'+' => t => t.IsPublic && t.FullName.StartsWith(pattern[1..]),
				'-' => t => t.IsPublic && !t.FullName.StartsWith(pattern[1..]),
				_ => t => t.IsPublic
			};

			foreach (var referenceType in referenceTypes.Where(predicate))
			{
				if (types.TryGetValue(referenceType.FullName, out var type))
				{
					if (referenceType.BaseType?.FullName != type.BaseType?.FullName)
					{
						Console.WriteLine($"{referenceType.FullName} base type is different {referenceType.BaseType?.FullName} in reference, {type.BaseType?.FullName} in {identifier}");
						hasError = true;
					}

					hasError |= CompareMembers(referenceType.Methods.Where(m => m.IsPublic), type.Methods, identifier);
					hasError |= CompareMembers(referenceType.Properties.Where(m => m.GetMethod?.IsPublic ?? false), type.Properties, identifier);
					hasError |= CompareMembers(referenceType.Fields.Where(m => m.IsPublic), type.Fields, identifier);
					hasError |= CompareMembers(referenceType.Events.Where(m => m.AddMethod?.IsPublic ?? false), type.Events, identifier);
				}
				else
				{
					hasError = true;
					Console.WriteLine($"The type {referenceType} is missing from {identifier}");
				}
			}

			return hasError;
		}

		private static bool CompareMembers(IEnumerable<MemberReference> referenceMembers, IEnumerable<MemberReference> members, string identifier)
		{
			var hasError = false;

			var membersLookup = members.Select(RewriteMember).ToDictionary(m => m.ToString());

			foreach (var referenceMember in referenceMembers.Where(IncludeReferenceMember).Select(RewriteReferenceMember))
			{
				if (!membersLookup.ContainsKey(referenceMember))
				{
					Console.WriteLine($"The member {referenceMember} cannot be found in {identifier}");
					hasError = true;
				}
			}

			return hasError;
		}

		private static AssemblyDefinition ReadAssemblyDefinition(string assemblyPath)
			=> AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters() { ApplyWindowsRuntimeProjections = true, AssemblyResolver = new DefaultAssemblyResolver() });

		private static MemberReference RewriteMember(MemberReference member)
		{
			if (member is MethodDefinition methodDefinition)
			{
				if (methodDefinition.HasOverrides && methodDefinition.Name.EndsWith("IEnumerable.GetEnumerator"))
				{
					methodDefinition.Name = "GetEnumerator";
				}
			}

			return member;
		}

		private static string RewriteReferenceMember(MemberReference member)
		{
			if (member is MethodDefinition methodDefinition)
			{
				if (methodDefinition.IsAddOn && methodDefinition.ReturnType.FullName == "System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken")
				{
					methodDefinition.ReturnType = methodDefinition.Module.TypeSystem.Void;
				}

				if (methodDefinition.IsRemoveOn && methodDefinition.Parameters.Count == 1 && methodDefinition.Parameters[0].ParameterType.FullName == "System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken")
				{
					var parameterType = methodDefinition.DeclaringType.Methods.First(e => e.Name == "add_" + methodDefinition.Name.Substring(7)).Parameters[0].ParameterType;

					methodDefinition.Parameters[0].ParameterType = parameterType;
				}

				if (methodDefinition.IsSetter && methodDefinition.Name.StartsWith("put_"))
				{
					var suffix = methodDefinition.Name.Substring(4);

					return methodDefinition.ToString().Replace("put_" + suffix, "set_" + suffix);
				}
			}

			return member.ToString();
		}

		private static bool IncludeReferenceMember(MemberReference member)
		{
			if (member is MethodDefinition methodDefinition)
			{
				if (methodDefinition.DeclaringType.FullName == "Windows.UI.Xaml.Controls.WebView")
				{
					switch (methodDefinition.Name)
					{
						case "get_XYFocusLeft":
						case "put_XYFocusLeft":
						case "get_XYFocusRight":
						case "put_XYFocusRight":
						case "get_XYFocusUp":
						case "put_XYFocusUp":
						case "get_XYFocusDown":
						case "put_XYFocusDown":
						case "get_XYFocusLeftProperty":
						case "get_XYFocusRightProperty":
						case "get_XYFocusUpProperty":
						case "get_XYFocusDownProperty":
							return false;
					}
				}
			}
			else if (member is PropertyDefinition propertyDefinition)
			{
				if (propertyDefinition.DeclaringType.FullName == "Windows.UI.Xaml.Controls.WebView")
				{
					switch (propertyDefinition.Name)
					{
						case "XYFocusLeft":
						case "XYFocusRight":
						case "XYFocusUp":
						case "XYFocusDown":
						case "XYFocusLeftProperty":
						case "XYFocusRightProperty":
						case "XYFocusUpProperty":
						case "XYFocusDownProperty":
							return false;
					}
				}
			}

			return true;
		}

		private static string UnpackArchive(string packagePath)
		{
			var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

			Directory.CreateDirectory(path);

			Console.WriteLine($"Extracting {packagePath} -> {path}");
			using (var file = File.OpenRead(packagePath))
			{
				using (var archive = new ZipArchive(file, ZipArchiveMode.Read))
				{
					archive.ExtractToDirectory(path);
				}
			}

			if (Directory.GetFiles(path, "*.nuspec", SearchOption.AllDirectories).FirstOrDefault() is string)
			{
				return path;
			}
			else
			{
				throw new InvalidOperationException($"Unable to find nuspec file in {packagePath}");
			}
		}
	}
}
