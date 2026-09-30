using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Uno.UI.Tasks.RuntimeAssetsSelector;

namespace Uno.UI.Tasks.Tests;

/// <summary>
/// A scratch project importing the shipped uno.winui.runtime-replace.targets, run through <c>dotnet msbuild</c>, so
/// the target-level conditions (variant mapping, UNOB0023/0024/0025) are exercised as a consumer's build sees them.
/// </summary>
internal sealed class RuntimeReplaceProject
{
	private readonly Dictionary<string, string> _properties = new(StringComparer.Ordinal);
	private readonly List<string> _items = [];

	public RuntimeReplaceProject(string directory, string targetFramework)
	{
		Directory = directory;
		_properties["TargetFramework"] = targetFramework;

		// What the .NET SDK would derive from the target framework; the scratch project imports no SDK.
		_properties["TargetFrameworkVersion"] = "v" + targetFramework.Substring(3).Split('-')[0];
	}

	public string Directory { get; }

	public RuntimeReplaceProject Property(string name, string value)
	{
		_properties[name] = value;
		return this;
	}

	public RuntimeReplaceProject RuntimeEnabledPackage(string packageId, string packageBasePath)
	{
		_items.Add($"<UnoRuntimeEnabledPackage Include=\"{packageId}\" PackageBasePath=\"{packageBasePath}\" />");
		return this;
	}

	public RuntimeReplaceProject Item(string xml)
	{
		_items.Add(xml);
		return this;
	}

	/// <summary>Project content added after the import, e.g. a consumer's own target.</summary>
	public RuntimeReplaceProject Raw(string xml)
	{
		_raw.Add(xml);
		return this;
	}

	private readonly List<string> _raw = [];

	private bool _importCrossRuntimeTargets;

	/// <summary>Also imports uno.winui.cross-runtime.targets, which packs UnoRuntimeProjectReference outputs.</summary>
	public RuntimeReplaceProject ImportCrossRuntimeTargets()
	{
		_importCrossRuntimeTargets = true;
		return this;
	}

	public Result Run(string target, string[]? properties = null, string[]? items = null)
	{
		System.IO.Directory.CreateDirectory(Directory);
		var projectPath = Path.Combine(Directory, "Head.proj");
		var logPath = Path.Combine(Directory, $"{target}.log");
		File.WriteAllText(projectPath, BuildProject());

		var arguments = new List<string> { "msbuild", projectPath, $"-t:{target}", "-nologo", "-nodeReuse:false", $"-flp:LogFile={logPath};Verbosity=normal" };
		// A single -getProperty prints the bare value; asking for two always yields JSON.
		arguments.AddRange((properties ?? []).Append("TargetFramework").Distinct().Select(p => $"-getProperty:{p}"));
		arguments.AddRange((items ?? []).Select(i => $"-getItem:{i}"));

		var startInfo = new ProcessStartInfo("dotnet")
		{
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			WorkingDirectory = Directory,
		};
		arguments.ForEach(startInfo.ArgumentList.Add);
		startInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";

		using var process = Process.Start(startInfo)!;
		var stdout = process.StandardOutput.ReadToEndAsync();
		var stderr = process.StandardError.ReadToEndAsync();
		process.WaitForExit();

		var log = File.Exists(logPath) ? File.ReadAllText(logPath) : "";
		return new Result(process.ExitCode, stdout.Result + stderr.Result, log);
	}

	private string BuildProject()
	{
		var tasksAssembly = typeof(RuntimeAssetsSelectorTask_v0).Assembly.Location;
		var builder = new StringBuilder();
		builder.AppendLine("<Project>");
		builder.AppendLine("\t<PropertyGroup>");
		foreach (var (name, value) in _properties)
		{
			builder.AppendLine($"\t\t<{name}>{value}</{name}>");
		}

		builder.AppendLine("\t</PropertyGroup>");
		builder.AppendLine("\t<ItemGroup>");
		_items.ForEach(item => builder.AppendLine("\t\t" + item));
		builder.AppendLine("\t</ItemGroup>");
		builder.AppendLine($"\t<UsingTask AssemblyFile=\"{tasksAssembly}\" TaskName=\"Uno.UI.Tasks.RuntimeAssetsSelector.RuntimeAssetsSelectorTask_v0\" TaskFactory=\"TaskHostFactory\" Runtime=\"CurrentRuntime\" Architecture=\"CurrentArchitecture\" />");
		builder.AppendLine($"\t<UsingTask AssemblyFile=\"{tasksAssembly}\" TaskName=\"Uno.UI.Tasks.RuntimeAssetsValidator.RuntimeAssetsValidatorTask_v0\" TaskFactory=\"TaskHostFactory\" Runtime=\"CurrentRuntime\" Architecture=\"CurrentArchitecture\" />");
		builder.AppendLine($"\t<Import Project=\"{RepositoryPaths.Get("build", "nuget", "uno.winui.runtime-replace.targets")}\" />");
		if (_importCrossRuntimeTargets)
		{
			builder.AppendLine($"\t<Import Project=\"{RepositoryPaths.Get("build", "nuget", "uno.winui.cross-runtime.targets")}\" />");
		}

		// Stand-ins for the SDK targets the shipped ones hook, so each can be run on its own.
		builder.AppendLine("\t<Target Name=\"BeforeBuild\" />");
		builder.AppendLine("\t<Target Name=\"CoreCompile\" />");
		_raw.ForEach(xml => builder.AppendLine("\t" + xml));
		builder.AppendLine("</Project>");
		return builder.ToString();
	}

	internal sealed record Result(int ExitCode, string Output, string Log)
	{
		public bool Succeeded => ExitCode == 0;

		public IEnumerable<string> Diagnostics(string code)
			=> Log.Split('\n').Select(l => l.Trim()).Where(l => l.Contains($" {code}:", StringComparison.Ordinal));

		public bool HasError(string code) => Diagnostics(code).Any(l => l.Contains($"error {code}:", StringComparison.Ordinal));

		public bool HasWarning(string code) => Diagnostics(code).Any(l => l.Contains($"warning {code}:", StringComparison.Ordinal));

		public string Property(string name)
		{
			using var json = ParseJson();
			return json.RootElement.GetProperty("Properties").GetProperty(name).GetString() ?? "";
		}

		public IReadOnlyList<string> Items(string name, string metadata = "Identity")
		{
			using var json = ParseJson();
			return json.RootElement.GetProperty("Items").GetProperty(name).EnumerateArray()
				.Select(item => item.GetProperty(metadata).GetString()!.Replace('\\', '/'))
				.ToList();
		}

		private JsonDocument ParseJson()
		{
			var start = Output.IndexOf('{', StringComparison.Ordinal);
			if (start < 0)
			{
				throw new InvalidOperationException($"No JSON in the msbuild output:{Environment.NewLine}{Output}{Environment.NewLine}{Log}");
			}

			// Warnings can follow the JSON on the console, so read just the first value.
			var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(Output[start..]));
			return JsonDocument.ParseValue(ref reader);
		}
	}
}
