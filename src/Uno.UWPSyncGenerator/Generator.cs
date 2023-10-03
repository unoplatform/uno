using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Build.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Uno.Extensions;
using Uno.UWPSyncGenerator.Helpers;

namespace Uno.UWPSyncGenerator
{
	abstract class Generator
	{
		internal const string CSharpLangVersion = "12.0";
#if HAS_UNO_WINUI
		private static IEnumerable<PortableExecutableReference> _winuiReferences;
#endif

		private const string UnitTestsDefine = "IS_UNIT_TESTS";
		private const string AndroidDefine = "__ANDROID__";
		private const string iOSDefine = "__IOS__";
		private const string tvOSDefine = "__TVOS__";
		private const string NetStdReferenceDefine = "__NETSTD_REFERENCE__";
		private const string WasmDefine = "__WASM__";
		private const string SkiaDefine = "__SKIA__";

#if HAS_UNO_WINUI
		private const string BaseXamlNamespace = "Microsoft.UI.Xaml";
#else
		private const string BaseXamlNamespace = "Windows.UI.Xaml";
#endif

		private static readonly string[] _skipBaseTypes = new[]
		{
			// skipped because of legacy mismatched hierarchy
			BaseXamlNamespace + ".FrameworkElement",
			BaseXamlNamespace + ".UIElement",
			BaseXamlNamespace + ".Controls.Image",
			BaseXamlNamespace + ".Controls.CalendarViewDayItem",
			BaseXamlNamespace + ".Controls.ComboBox",
			BaseXamlNamespace + ".Controls.CheckBox",
			BaseXamlNamespace + ".Controls.TextBlock",
			BaseXamlNamespace + ".Controls.TextBox",
			BaseXamlNamespace + ".Controls.ProgressRing",
			BaseXamlNamespace + ".Controls.ListViewBase",
			BaseXamlNamespace + ".Controls.ListView",
			BaseXamlNamespace + ".Controls.ListViewHeaderItem",
			BaseXamlNamespace + ".Controls.GridView",
			BaseXamlNamespace + ".Controls.ComboBox",
			BaseXamlNamespace + ".Controls.UserControl",
			BaseXamlNamespace + ".Controls.RadioButton",
			BaseXamlNamespace + ".Controls.Slider",
			BaseXamlNamespace + ".Controls.PasswordBox",
			BaseXamlNamespace + ".Controls.RichEditBox",
			BaseXamlNamespace + ".Controls.ProgressBar",
			BaseXamlNamespace + ".Controls.ListViewItem",
			BaseXamlNamespace + ".Controls.ScrollContentPresenter",
			BaseXamlNamespace + ".Controls.Pivot",
			BaseXamlNamespace + ".Controls.CommandBar",
			BaseXamlNamespace + ".Controls.AppBar",
			BaseXamlNamespace + ".Controls.TimePickerFlyoutPresenter",
			BaseXamlNamespace + ".Controls.DatePickerFlyoutPresenter",
			BaseXamlNamespace + ".Controls.AppBarSeparator",
			BaseXamlNamespace + ".Controls.DatePickerFlyout",
			BaseXamlNamespace + ".Controls.TimePickerFlyout",
			BaseXamlNamespace + ".Controls.AppBarToggleButton",
			BaseXamlNamespace + ".Controls.FlipView",
			BaseXamlNamespace + ".Controls.FlipViewItem",
			BaseXamlNamespace + ".Controls.GridViewItem",
			BaseXamlNamespace + ".Controls.ComboBoxItem",
			BaseXamlNamespace + ".Controls.Flyout",
			BaseXamlNamespace + ".Controls.FontIcon",
			BaseXamlNamespace + ".Controls.MenuFlyout",
			BaseXamlNamespace + ".Data.CollectionView",
			BaseXamlNamespace + ".Controls.WebView",
			BaseXamlNamespace + ".Controls.UIElementCollection",
			BaseXamlNamespace + ".Media.Animation.FadeInThemeAnimation",
			BaseXamlNamespace + ".Media.Animation.FadeOutThemeAnimation",
			BaseXamlNamespace + ".Media.ImageBrush",
			BaseXamlNamespace + ".Media.LinearGradientBrush",
			BaseXamlNamespace + ".Data.RelativeSource",
			BaseXamlNamespace + ".Controls.Primitives.CarouselPanel",
			BaseXamlNamespace + ".Controls.MediaPlayerPresenter",
			BaseXamlNamespace + ".Controls.NavigationViewItemBase",
			"Microsoft.UI.Xaml.Controls.WebView2",
			// Mismatching public inheritance hierarchy because RadioMenuFlyoutItem has a double inheritance in WinUI.
			// Remove this and update RadioMenuFlyoutItem if WinUI 3 removed the double inheritance.
			"Microsoft.UI.Xaml.Controls.RadioMenuFlyoutItem",
		};

		private Compilation _iOSCompilation;
		private Compilation _tvOSCompilation;
		private Compilation _androidCompilation;
		private INamedTypeSymbol _iOSBaseSymbol;
		private INamedTypeSymbol _tvOSBaseSymbol;
		private INamedTypeSymbol _androidBaseSymbol;
		private static Compilation s_referenceCompilation;
		private Compilation _unitTestsCompilation;

		private Compilation _netstdReferenceCompilation;
		private Compilation _wasmCompilation;
		private Compilation _skiaCompilation;

		private ISymbol _dependencyPropertySymbol;
		protected ISymbol FlagsAttributeSymbol { get; private set; }
		protected ISymbol UIElementSymbol { get; private set; }
		private static string MSBuildBasePath;

		private static readonly string[] _unoUINamespaces = new[]
		{
			"Windows.UI.Xaml",
			"Windows.UI.Composition",
			"Windows.UI.Dispatching",
			"Microsoft.UI.Xaml",
			"Microsoft.Web",
#if HAS_UNO_WINUI
			"Microsoft.Foundation",
			"Microsoft.UI.Xaml",
			"Microsoft.UI.Composition",
			"Microsoft.UI.Dispatching",
			"Microsoft.UI.Text",
			"Microsoft.UI.Content",
			"Microsoft.UI.Windowing",
			"Microsoft.UI.Input",
			"Microsoft.System",
			"Microsoft.Graphics",
			"Microsoft.Windows.ApplicationModel.Resources",
			"Microsoft.Web",
#endif
			};

		static Generator()
		{
			RegisterAssemblyLoader();
			Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
			InitializeRoslyn();
		}

		public virtual async Task Build(
#if !HAS_UNO_WINUI
			string baseName, string sourceAssembly
#endif
			)
		{
#if !HAS_UNO_WINUI
			Console.WriteLine($"Generating for {baseName} {sourceAssembly}");
#endif
			const string referencesPath =
#if HAS_UNO_WINUI
				@"..\..\..\Uno.UWPSyncGenerator.Reference.WinUI\references.txt";
#else
				@"..\..\..\Uno.UWPSyncGenerator.Reference\references.txt";
#endif
			s_referenceCompilation ??= await LoadUWPReferenceProject(referencesPath);

			_dependencyPropertySymbol = s_referenceCompilation.GetTypeByMetadataName(BaseXamlNamespace + ".DependencyProperty");

			var topProject = @"..\..\..\Uno.UI\Uno.UI";

			_iOSCompilation = await LoadProject($@"{topProject}.netcoremobile.csproj", "net9.0-ios18.0");
			_tvOSCompilation = await LoadProject($@"{topProject}.netcoremobile.csproj", "net9.0-tvos18.0");
			_androidCompilation = await LoadProject($@"{topProject}.netcoremobile.csproj", "net9.0-android");
			_unitTestsCompilation = await LoadProject($@"{topProject}.Tests.csproj", "net9.0");

			_netstdReferenceCompilation = await LoadProject($@"{topProject}.Reference.csproj", "net9.0");
			_wasmCompilation = await LoadProject($@"{topProject}.Wasm.csproj", "net9.0");
			_skiaCompilation = await LoadProject($@"{topProject}.Skia.csproj", "net9.0");

			_iOSBaseSymbol = _iOSCompilation.GetTypeByMetadataName("UIKit.UIView");
			_tvOSBaseSymbol = _tvOSCompilation.GetTypeByMetadataName("UIKit.UIView");
			_androidBaseSymbol = _androidCompilation.GetTypeByMetadataName("Android.Views.View");

			FlagsAttributeSymbol = s_referenceCompilation.GetTypeByMetadataName("System.FlagsAttribute");
			UIElementSymbol = s_referenceCompilation.GetTypeByMetadataName(BaseXamlNamespace + ".UIElement");

#if !HAS_UNO_WINUI
			var origins = from externalRedfs in s_referenceCompilation.ExternalReferences
						  let fileNameWithoutExtension = Path.GetFileNameWithoutExtension(externalRedfs.Display)
						  where fileNameWithoutExtension.StartsWith("Windows.Foundation", StringComparison.Ordinal)
						  || fileNameWithoutExtension.StartsWith("Microsoft.WinUI", StringComparison.Ordinal)
						  || fileNameWithoutExtension.StartsWith("Microsoft.UI", StringComparison.Ordinal)
						  || fileNameWithoutExtension.StartsWith("Microsoft.Foundation", StringComparison.Ordinal)
						  || fileNameWithoutExtension.StartsWith("Microsoft.ApplicationModel.Resources", StringComparison.Ordinal)
						  || fileNameWithoutExtension.StartsWith("Microsoft.Graphics", StringComparison.Ordinal)
						  || fileNameWithoutExtension.StartsWith("Windows.Phone.PhoneContract", StringComparison.Ordinal)
						  || fileNameWithoutExtension.StartsWith("Windows.Networking.Connectivity.WwanContract", StringComparison.Ordinal)
						  || fileNameWithoutExtension.StartsWith("Windows.ApplicationModel.Calls.CallsPhoneContract", StringComparison.Ordinal)
						  || fileNameWithoutExtension.StartsWith("Windows.Services.Store.StoreContract", StringComparison.Ordinal)
						  || fileNameWithoutExtension.StartsWith("Windows.UI.Xaml.Hosting.HostingContract", StringComparison.Ordinal)
						  || fileNameWithoutExtension.StartsWith("Microsoft.Web.WebView2.Core", StringComparison.Ordinal)
						  let asm = s_referenceCompilation.GetAssemblyOrModuleSymbol(externalRedfs) as IAssemblySymbol
						  where asm != null
						  select asm;

			var excludeNamespaces = new List<string>();
			var includeNamespaces = new List<string>();

			// For UWP compilation we need to ignore these namespaces when not explicitly generating
			// for related projects.
			if (baseName == "Uno.UI.Dispatching")
			{
				includeNamespaces.Add("Windows.UI.Dispatching");
			}
			else if (baseName == "Uno.UI.Composition")
			{
				includeNamespaces.Add("Windows.UI.Composition");
			}
			else
			{
				excludeNamespaces.Add("Windows.UI.Dispatching");
				excludeNamespaces.Add("Windows.UI.Composition");
			}

			var q = from asm in origins
					from targetType in GetNamespaceTypes(asm.Modules.First().GlobalNamespace)
					where !SkipNamespace(targetType)
					where targetType.DeclaredAccessibility == Accessibility.Public
					where ((baseName == "Uno" || baseName == "Uno.Foundation") && !targetType.ContainingNamespace.ToString().StartsWith("Windows.UI.Xaml", StringComparison.Ordinal) && !targetType.ContainingNamespace.ToString().StartsWith("Microsoft.UI.Xaml", StringComparison.Ordinal))
					|| (
						(baseName == "Uno.UI" || baseName == "Uno.UI.Dispatching" || baseName == "Uno.UI.Composition")
						&& _unoUINamespaces.Any(n => targetType.ContainingNamespace.ToString().StartsWith(n, StringComparison.Ordinal))
					)
					where !excludeNamespaces.Any(n => targetType.ContainingNamespace.ToString().StartsWith(n, StringComparison.Ordinal))
					where (includeNamespaces.Count == 0) || includeNamespaces.Any(n => targetType.ContainingNamespace.ToString().StartsWith(n, StringComparison.Ordinal))
					group targetType by targetType.ContainingNamespace into namespaces
					orderby namespaces.Key.MetadataName
					select new
					{
						Namespace = namespaces.Key,
						Types = namespaces
							.Where(t => t.DeclaredAccessibility == Accessibility.Public)
					};

			foreach (var ns in q)
			{
				foreach (var type in ns.Types)
				{
					ProcessType(type, ns.Namespace);
				}
			}
#else
			var winuiReferenceDisplays = _winuiReferences.Select(r => r.Display).ToArray();
			foreach (var reference in s_referenceCompilation.ExternalReferences)
			{
				if (s_referenceCompilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly && winuiReferenceDisplays.Contains(reference.Display))
				{
					if (ShouldGenerateForAssembly(assembly.Name))
					{
						foreach (var type in GetNamespaceTypes(assembly.Modules.First().GlobalNamespace))
						{
							if (!SkipNamespace(type) && type.DeclaredAccessibility == Accessibility.Public)
							{
								ProcessType(type, type.ContainingNamespace);
							}
						}
					}
				}

			}
#endif
		}

#if HAS_UNO_WINUI
		private static bool ShouldGenerateForAssembly(string assemblyName)
		{
			switch (assemblyName)
			{
				case "Microsoft.InteractiveExperiences.Projection":
				case "Microsoft.WinUI":
				case "Microsoft.Windows.AppLifecycle.Projection":
				case "Microsoft.Windows.ApplicationModel.Resources.Projection":
				case "Microsoft.Windows.PushNotifications.Projection":
				case "Microsoft.Windows.System.Power.Projection":
				case "Microsoft.WindowsAppRuntime.Bootstrap.Net":
				case "Microsoft.Windows.SDK.NET":
				case "WinRT.Runtime":
					return true;

				case "Microsoft.Windows.ApplicationModel.DynamicDependency.Projection":
				case "Microsoft.Windows.ApplicationModel.WindowsAppRuntime.Projection":
				case "Microsoft.Windows.AppNotifications.Builder.Projection":
				case "Microsoft.Windows.AppNotifications.Projection":
				case "Microsoft.Windows.Security.AccessControl.Projection":
				case "Microsoft.Windows.System.Projection":
				case "Microsoft.Windows.Widgets.Projection":
					return false;

				default:
					// This can be hit if WinUI reference is updated to newer version that has new assemblies.
					// In this case, those new assemblies should be reviewed whether we should generate types for them or not.
					// You should also modify `GetNamespaceBasePath` to specify where types from this assemblies should be generated.
					throw new InvalidOperationException($"Unexpected assembly name '{assemblyName}'");
			}
		}
#endif

		private static bool SkipNamespace(INamedTypeSymbol namedTypeSymbol)
		{
			var @namespace = namedTypeSymbol.ContainingNamespace.ToDisplayString();
			if (@namespace.StartsWith("Microsoft.UI.Input.Experimental", StringComparison.Ordinal))
			{
				// Skip Microsoft.UI.Input.Experimental as it is not part of WinAppSDK desktop APIs
				return true;
			}
			else if (@namespace.StartsWith("ABI.", StringComparison.Ordinal))
			{
				return true;
			}
			else if (@namespace == "WinRT")
			{
				return true;
			}
			else if (@namespace == "WinRT.Interop")
			{
				return namedTypeSymbol.Name is
					"IUnknownVftbl" or
					"IWeakReference" or
					"IWeakReferenceSource" or
					"_add_EventHandler" or
					"_remove_EventHandler";
			}

			return false;
		}

		protected abstract void ProcessType(INamedTypeSymbol type, INamespaceSymbol ns);

		private static void InitializeRoslyn()
		{
			var installPath = Environment.GetEnvironmentVariable("VSINSTALLDIR");

			if (string.IsNullOrEmpty(installPath))
			{
				var pi = new System.Diagnostics.ProcessStartInfo(
					"cmd.exe",
					@"/c ""C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe"" -property installationPath -prerelease"
				)
				{
					RedirectStandardOutput = true,
					UseShellExecute = false,
					CreateNoWindow = true
				};

				var process = System.Diagnostics.Process.Start(pi);
				process.WaitForExit();
				installPath = process.StandardOutput.ReadToEnd().Split('\r').First();
			}

			SetupMSBuildLookupPath(installPath);
		}

		private static void SetupMSBuildLookupPath(string installPath)
		{
			var result = ProcessHelper.RunProcess("dotnet.exe", "--info");

			if (result.exitCode == 0)
			{
				var reader = new StringReader(result.output);

				while (reader.ReadLine() is string line)
				{
					if (line.Contains("Base Path:"))
					{
						MSBuildBasePath = line.Substring(line.IndexOf(':') + 1).Trim();
						return;
					}
				}

				throw new InvalidOperationException($"Unable to find dotnet SDK base path in:\n {result.output}");
			}

			throw new InvalidOperationException($"Unable to find dotnet SDK base path (Exit code: {result.exitCode})");
		}

		protected string GetNamespaceBasePath(INamedTypeSymbol type)
		{
#if !HAS_UNO_WINUI
			if (type.ContainingAssembly.Name == "Windows.Foundation.FoundationContract")
			{
				return @"..\..\..\Uno.Foundation\Generated\2.0.0.0";
			}

			var containingNamespaceName = type.ContainingNamespace.ToString();
			if (containingNamespaceName.StartsWith("Windows.UI.Composition", StringComparison.Ordinal))
			{
				return @"..\..\..\Uno.UI.Composition\Generated\3.0.0.0";
			}
			else if (containingNamespaceName.StartsWith("Windows.UI.Xaml", StringComparison.Ordinal)
				|| containingNamespaceName.StartsWith("Microsoft.UI.Xaml", StringComparison.Ordinal)
				|| containingNamespaceName.StartsWith("Microsoft.Web", StringComparison.Ordinal)
			)
			{
				return @"..\..\..\Uno.UI\Generated\3.0.0.0";
			}
			else
			{
				return @"..\..\..\Uno.UWP\Generated\3.0.0.0";
			}
#else
			var @namespace = type.ContainingNamespace.ToString();
			if (@namespace.StartsWith("Microsoft.UI.Composition", StringComparison.Ordinal))
			{
				return @"..\..\..\Uno.UI.Composition\Generated\3.0.0.0";
			}
			else if (@namespace.StartsWith("Microsoft.UI.Dispatching", StringComparison.Ordinal))
			{
				return @"..\..\..\Uno.UI.Dispatching\Generated\3.0.0.0";
			}
			else if (@namespace.StartsWith("Microsoft.UI.Input", StringComparison.Ordinal))
			{
				return @"..\..\..\Uno.UI\Generated\3.0.0.0";
			}
			else if (@namespace.StartsWith("Windows.Foundation.Collections", StringComparison.Ordinal) ||
				@namespace.StartsWith("Windows.Foundation.Metadata", StringComparison.Ordinal))
			{
				return @"..\..\..\Uno.Foundation\Generated\2.0.0.0";
			}

			if (type.Name.Contains("AsyncAction", StringComparison.Ordinal) ||
				type.Name.Contains("AsyncOperation", StringComparison.Ordinal) ||
				type.Name is
					"IAsyncInfo" or
					"AsyncStatus" or
					"Deferral" or
					"DeferralCompletedHandler" or
					"TypedEventHandler" or
					"FoundationContract" or
					"PropertyValue" or
					"IPropertyValue" or
					"PropertyType" or
					"IStringable" or
					"IReferenceArray")
			{
				return @"..\..\..\Uno.Foundation\Generated\2.0.0.0";
			}

			switch (type.ContainingAssembly.Name)
			{
				case "Microsoft.InteractiveExperiences.Projection":
				case "Microsoft.Windows.ApplicationModel.Resources.Projection":
				case "Microsoft.Windows.AppLifecycle.Projection":
				case "Microsoft.Windows.PushNotifications.Projection":
				case "Microsoft.Windows.System.Power.Projection":
				case "Microsoft.WindowsAppRuntime.Bootstrap.Net":
				case "Microsoft.Windows.SDK.NET":
					return @"..\..\..\Uno.UWP\Generated\3.0.0.0";

				case "WinRT.Runtime":
					return @"..\..\..\Uno.Foundation\Generated\2.0.0.0";

				case "Microsoft.WinUI":
					return @"..\..\..\Uno.UI\Generated\3.0.0.0";

				default:
					throw new InvalidOperationException($"Unknown assembly '{type.ContainingAssembly.Name}'.");
			}
#endif
		}

		protected class PlatformSymbols<T> where T : ISymbol
		{
			public T AndroidSymbol;
			public T IOSSymbol;
			public T TvOSSymbol;
			public T UnitTestsymbol;
			public T UAPSymbol;
			public T NetStdReferenceSymbol;
			public T WasmSymbol;
			public T SkiaSymbol;

			private ImplementedFor _implementedFor;
			public ImplementedFor ImplementedFor => _implementedFor;
			public ImplementedFor ImplementedForMain => ImplementedFor & ImplementedFor.Main;

			public PlatformSymbols(
				T androidType,
				T iOSType,
				T tvOSType,
				T unitTestType,
				T netStdRerefenceType,
				T wasmType,
				T skiaType,
				T uapType
			)
			{
				this.AndroidSymbol = androidType;
				this.IOSSymbol = iOSType;
				this.TvOSSymbol = tvOSType;
				this.UnitTestsymbol = unitTestType;
				this.UAPSymbol = uapType;
				this.NetStdReferenceSymbol = netStdRerefenceType;
				this.WasmSymbol = wasmType;
				this.SkiaSymbol = skiaType;

				if (IsImplemented(AndroidSymbol))
				{
					_implementedFor |= ImplementedFor.Android;
				}
				if (IsImplemented(IOSSymbol))
				{
					_implementedFor |= ImplementedFor.iOS;
				}
				if (IsImplemented(TvOSSymbol))
				{
					_implementedFor |= ImplementedFor.tvOS;
				}
				if (IsImplemented(UnitTestsymbol))
				{
					_implementedFor |= ImplementedFor.UnitTests;
				}
				if (IsImplemented(NetStdReferenceSymbol))
				{
					_implementedFor |= ImplementedFor.NetStdReference;
				}
				if (IsImplemented(WasmSymbol))
				{
					_implementedFor |= ImplementedFor.WASM;
				}
				if (IsImplemented(SkiaSymbol))
				{
					_implementedFor |= ImplementedFor.Skia;
				}
			}

			public bool HasUndefined =>
				AndroidSymbol == null
				|| IOSSymbol == null
				|| TvOSSymbol == null
				|| UnitTestsymbol == null
				|| NetStdReferenceSymbol == null
				|| WasmSymbol == null
				|| SkiaSymbol == null
				;

			public void AppendIf(IndentedStringBuilder b)
			{
				var defines = new[] {
					IsNotDefinedByUno(AndroidSymbol) ? AndroidDefine : "false",
					IsNotDefinedByUno(IOSSymbol) ? iOSDefine : "false",
					IsNotDefinedByUno(TvOSSymbol) ? tvOSDefine : "false",
					IsNotDefinedByUno(UnitTestsymbol) ? UnitTestsDefine : "false",
					IsNotDefinedByUno(WasmSymbol) ? WasmDefine : "false",
					IsNotDefinedByUno(SkiaSymbol) ? SkiaDefine : "false",
					IsNotDefinedByUno(NetStdReferenceSymbol) ? NetStdReferenceDefine : "false",
				};

				using (b.Indent(-b.CurrentLevel))
				{
					b.AppendLineInvariant($"#if {defines.JoinBy(" || ")}");
				}
			}

			public string GenerateNotImplementedList()
			{
				var defines = new[] {
					IsNotDefinedByUno(AndroidSymbol) ? $"\"{AndroidDefine}\"" : "",
					IsNotDefinedByUno(IOSSymbol) ? $"\"{iOSDefine}\"" : "",
					IsNotDefinedByUno(TvOSSymbol) ? $"\"{tvOSDefine}\"" : "",
					IsNotDefinedByUno(UnitTestsymbol) ? $"\"{UnitTestsDefine}\"" : "",
					IsNotDefinedByUno(WasmSymbol) ? $"\"{WasmDefine}\"" : "",
					IsNotDefinedByUno(SkiaSymbol) ? $"\"{SkiaDefine}\"": "",
					IsNotDefinedByUno(NetStdReferenceSymbol) ? $"\"{NetStdReferenceDefine}\"" : "",
				};

				return defines.Where(d => d.Length > 0).JoinBy(", ");
			}

			public bool IsNotImplementedInAllPlatforms()
				=> IsNotDefinedByUno(AndroidSymbol) &&
					IsNotDefinedByUno(IOSSymbol) &&
					IsNotDefinedByUno(TvOSSymbol) &&
					IsNotDefinedByUno(UnitTestsymbol) &&
					IsNotDefinedByUno(WasmSymbol) &&
					IsNotDefinedByUno(SkiaSymbol) &&
					IsNotDefinedByUno(NetStdReferenceSymbol);

			private static bool IsNotDefinedByUno(ISymbol symbol)
			{
				if (symbol == null) { return true; }
				if (symbol is not INamedTypeSymbol type) { return false; }

				var onlyGenerated = type.DeclaringSyntaxReferences.All(r => IsGeneratedFile(r.SyntaxTree.FilePath));
				return onlyGenerated;
			}

			private static bool IsGeneratedFile(string filePath)
			{
				if (filePath.EndsWith(".g.cs", StringComparison.Ordinal))
				{
					return true;
				}
				if (filePath.Contains(@"Generated\3.0.0.0"))
				{
					return true;
				}
				if (filePath.Contains(@"Generated\2.0.0.0"))
				{
					return true;
				}

				return false;
			}

			private static bool IsImplemented(ISymbol symbol)
			{
				if (symbol == null) { return false; }
				if (symbol.GetAttributes().Any(a => a.AttributeClass.Name == "NotImplementedAttribute")) { return false; }
				if (IsNotDefinedByUno(symbol)) { return false; }

				return true;
			}
		}

		protected PlatformSymbols<INamedTypeSymbol> GetAllSymbols(INamedTypeSymbol uapType)
		{
			var name = uapType.ContainingNamespace + "." + uapType.MetadataName;
			return new PlatformSymbols<INamedTypeSymbol>(
				  androidType: _androidCompilation.GetTypeByMetadataName(name),
				  iOSType: _iOSCompilation.GetTypeByMetadataName(name),
				  tvOSType: _tvOSCompilation.GetTypeByMetadataName(name),
				  unitTestType: _unitTestsCompilation.GetTypeByMetadataName(name),
				  netStdRerefenceType: _netstdReferenceCompilation.GetTypeByMetadataName(name),
				  wasmType: _wasmCompilation.GetTypeByMetadataName(name),
				  skiaType: _skiaCompilation.GetTypeByMetadataName(name),
				  uapType: uapType
			  );
		}

		protected PlatformSymbols<ISymbol> GetAllGetNonGeneratedMembers(PlatformSymbols<INamedTypeSymbol> types, string name, Func<IEnumerable<ISymbol>, ISymbol> filter, ISymbol uapSymbol = null)
		{
			var android = GetNonGeneratedMembers(types.AndroidSymbol, name);
			var ios = GetNonGeneratedMembers(types.IOSSymbol, name);
			var tvos = GetNonGeneratedMembers(types.TvOSSymbol, name);
			var unitTests = GetNonGeneratedMembers(types.UnitTestsymbol, name);
			var netStdReference = GetNonGeneratedMembers(types.NetStdReferenceSymbol, name);
			var wasm = GetNonGeneratedMembers(types.WasmSymbol, name);
			var skia = GetNonGeneratedMembers(types.SkiaSymbol, name);

			return new PlatformSymbols<ISymbol>(
				androidType: filter(android),
				iOSType: filter(ios),
				tvOSType: filter(tvos),
				unitTestType: filter(unitTests),
				netStdRerefenceType: filter(netStdReference),
				wasmType: filter(wasm),
				skiaType: filter(skia),
				uapType: uapSymbol
			);
		}

		protected PlatformSymbols<IMethodSymbol> GetAllMatchingMethods(PlatformSymbols<INamedTypeSymbol> types, IMethodSymbol method)
			=> new PlatformSymbols<IMethodSymbol>(
				androidType: FindMatchingMethod(types.AndroidSymbol, method),
				iOSType: FindMatchingMethod(types.IOSSymbol, method),
				tvOSType: FindMatchingMethod(types.TvOSSymbol, method),
				unitTestType: FindMatchingMethod(types.UnitTestsymbol, method),
				netStdRerefenceType: FindMatchingMethod(types.NetStdReferenceSymbol, method),
				wasmType: FindMatchingMethod(types.WasmSymbol, method),
				skiaType: FindMatchingMethod(types.SkiaSymbol, method),
				uapType: method
			);

		protected PlatformSymbols<IPropertySymbol> GetAllMatchingPropertyMember(PlatformSymbols<INamedTypeSymbol> types, IPropertySymbol property)
			=> new PlatformSymbols<IPropertySymbol>(
				androidType: GetMatchingPropertyMember(types.AndroidSymbol, property),
				iOSType: GetMatchingPropertyMember(types.IOSSymbol, property),
				tvOSType: GetMatchingPropertyMember(types.TvOSSymbol, property),
				unitTestType: GetMatchingPropertyMember(types.UnitTestsymbol, property),
				netStdRerefenceType: GetMatchingPropertyMember(types.NetStdReferenceSymbol, property),
				wasmType: GetMatchingPropertyMember(types.WasmSymbol, property),
				skiaType: GetMatchingPropertyMember(types.SkiaSymbol, property),
				uapType: property
			);

		protected PlatformSymbols<ISymbol> GetAllMatchingEvents(PlatformSymbols<INamedTypeSymbol> types, IEventSymbol eventMember)
			=> GetAllGetNonGeneratedMembers(types, eventMember.Name, q => q.FirstOrDefault(e => SymbolMatchingHelpers.AreMatching(eventMember, e)), eventMember);

		protected bool SkippedType(INamedTypeSymbol type)
		{
			var v = type.ToString();
			switch (v)
			{
				case "Windows.Foundation.IAsyncOperation<TResult>":
					// Skipped to include generic variance.
					return true;

				case "Windows.Foundation.Collections.ValueSet":
					// Skipped to include nullable annotations.
					return true;

				case "Windows.Foundation.Uri":
				case BaseXamlNamespace + ".Input.ICommand":
				case BaseXamlNamespace + ".Controls.UIElementCollection":
					// Skipped because the reported interfaces are mismatched.
					return true;

				case BaseXamlNamespace + ".Media.FontFamily":
				case BaseXamlNamespace + ".Controls.IconElement":
				case BaseXamlNamespace + ".Data.ICollectionView":
				case BaseXamlNamespace + ".Data.CollectionView":
					// Skipped because the reported interfaces are mismatched.
					return true;

				case "Windows.UI.ViewManagement.InputPane":
				case "Windows.UI.ViewManagement.InputPaneVisibilityEventArgs":
				case "Windows.UI.ViewManagement.InputPaneInterop":
					// Skipped because a dependency on FocusManager
					return true;

				case "Windows.ApplicationModel.Store.Preview.WebAuthenticationCoreManagerHelper":
					// Skipped because a cross layer dependency to Windows.UI.Xaml
					return true;

				case "Microsoft.UI.Xaml.Controls.XamlControlsResources":
					// Skipped because the type is placed in the Uno.UI.FluentTheme assembly
					return true;

				case "Microsoft.UI.Xaml.Data.INotifyPropertyChanged":
				case "Microsoft.UI.Xaml.Data.PropertyChangedEventArgs":
				case "Microsoft.UI.Xaml.Data.PropertyChangedEventHandler":
					// Skipped because the types are hidden from the projections in WinAppSDK
					return true;

#if HAS_UNO_WINUI
				case "Windows.UI.Text.FontWeights":
					// Skipped because the type not present WinAppSDK projection
					return true;

				case "Windows.UI.Colors":
					// Skipped because the type not present WinAppSDK projection
					return true;

				case "Microsoft.Windows.ApplicationModel.DynamicDependency.Bootstrap":
					// This class has a nested enum. So proper generation for nested types would be needed first.
					// Also it's not clear if it's useful to generate it or not.
					return true;
#else
				case "Microsoft.UI.Xaml.Automation.Peers.AnimatedVisualPlayerAutomationPeer":
				case "Microsoft.UI.Xaml.Controls.IAnimatedVisualSource":
				case "Microsoft.UI.Xaml.Controls.IAnimatedVisualSource2":
				case "Microsoft.UI.Xaml.Controls.IDynamicAnimatedVisualSource":
					// Skipped because the implementation is currently incorrectly placed in WUX namespace
					return true;
#endif
			}


			return false;
		}

		protected void BuildInterfaceImplementations(INamedTypeSymbol type, IndentedStringBuilder b, PlatformSymbols<INamedTypeSymbol> types, List<IMethodSymbol> writtenMethods)
		{
			if (type.TypeKind != TypeKind.Interface)
			{
				var implementedInterfaces = new HashSet<INamedTypeSymbol>();

				foreach (var iface in type.Interfaces.Where(i => i.DeclaredAccessibility == Accessibility.Public))
				{
					if (
						iface.MetadataName is "Windows.Foundation.IAsyncAction" or "IWinRTObject"
						|| iface.ToDisplayString() == "Windows.Foundation.IStringable"
						|| iface.OriginalDefinition.MetadataName == "Windows.Foundation.Collections.IIterator`1"
						|| iface.OriginalDefinition.MetadataName == "Windows.Foundation.IAsyncOperation`1"
					)
					{
						continue;
					}

					var enumerable = GetAllInterfaces(iface).Distinct(new NamedTypeSymbolStringComparer()).ToArray();

					foreach (var inner in enumerable)
					{
						if (implementedInterfaces.Add(inner))
						{
							BuildInterfaceImplementation(b, type, inner, types, writtenMethods);
						}
					}
				}
			}
		}

		private IEnumerable<INamedTypeSymbol> GetAllInterfaces(INamedTypeSymbol roslynInterface)
		{
			yield return roslynInterface;

			foreach (var iface in roslynInterface.Interfaces)
			{
				foreach (var inner in GetAllInterfaces(iface))
				{
					yield return inner;
				}
			}
		}

		private void BuildInterfaceImplementation(
			IndentedStringBuilder b,
			INamedTypeSymbol ownerType,
			INamedTypeSymbol ifaceSymbol,
			PlatformSymbols<INamedTypeSymbol> types,
			List<IMethodSymbol> writtenMethods
		)
		{
			b.AppendLineInvariant($"// Processing: {ifaceSymbol}");

			foreach (var method in ifaceSymbol.GetMembers().OfType<IMethodSymbol>())
			{
				var isSpecialType = method.MethodKind == MethodKind.PropertyGet
					|| method.MethodKind == MethodKind.PropertySet
					|| method.MethodKind == MethodKind.EventAdd
					|| method.MethodKind == MethodKind.EventRemove
					|| method.MethodKind == MethodKind.EventRaise
					;
				if (isSpecialType)
				{
					continue;
				}

				var isDefinedInClass = ownerType.GetMembers(method.Name).OfType<IMethodSymbol>().Any(m =>
						m.DeclaredAccessibility == Accessibility.Public
						&& m.Parameters.Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).SequenceEqual(method.Parameters.Select(p2 => p2.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))
						&& m.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
					);
				if (isDefinedInClass)
				{
					continue;
				}

				var isAlreadyGenerated = writtenMethods.Any(m => m.Name == method.Name
						&& m.DeclaredAccessibility == Accessibility.Public
						&& m.Parameters.Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).SequenceEqual(method.Parameters.Select(p2 => p2.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))
						&& m.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
					);

				if (isAlreadyGenerated || !IsNotUWPMapping(ownerType, method))
				{
					continue;
				}

				var allMethods = GetAllGetNonGeneratedMembers(types, method.Name, q => q.OfType<IMethodSymbol>().FirstOrDefault());

				if (allMethods.HasUndefined)
				{
					allMethods.AppendIf(b);
					var parms = string.Join(", ", method.Parameters.Select(p => $"{GetParameterRefKind(p)}{TransformType(p.Type)} {SanitizeParameter(p.Name)}"));
					var returnTypeName = TransformType(method.ReturnType);
					var typeAccessibility = GetMethodAccessibility(method);
					var explicitImplementation = typeAccessibility == "" ? $"global::{ifaceSymbol}." : "";

					b.AppendLineInvariant($"// DeclaringType: {ifaceSymbol}");

					b.AppendLineInvariant($"[global::Uno.NotImplemented({allMethods.GenerateNotImplementedList()})]");
					using (b.BlockInvariant($"{typeAccessibility} {returnTypeName} {explicitImplementation}{method.Name}({parms})"))
					{
						b.AppendLineInvariant($"throw new global::System.NotSupportedException();");
					}

					using (b.Indent(-b.CurrentLevel))
					{
						b.AppendLineInvariant($"#endif");
					}
				}
			}

			foreach (var property in ifaceSymbol.GetMembers().OfType<IPropertySymbol>())
			{
				var propertyTypeName = TransformType(property.Type);
				var parms = string.Join(", ", property.GetMethod?.Parameters.Select(p => $"{TransformType(p.Type)} {SanitizeParameter(p.Name)}") ?? Array.Empty<string>());
				var allProperties = GetAllMatchingPropertyMember(types, property);

				if (ownerType.GetMembers(property.Name).OfType<IPropertySymbol>().Any(p =>
					   p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
					)
					|| !IsNotUWPMapping(ownerType, property))
				{
					continue;
				}

				if (ownerType.FindImplementationForInterfaceMember(property) is IPropertySymbol { ExplicitInterfaceImplementations.Length: > 0 })
				{
					// The explicit interface implementation will be generated while generating other members of ownerType.
					// We shouldn't generate an implicit implementation.
					continue;
				}

				if (allProperties.HasUndefined)
				{
					allProperties.AppendIf(b);

					var v = property.IsIndexer ? $"public {propertyTypeName} this[{parms}]" : $"public {propertyTypeName} {property.Name}";

					b.AppendLineInvariant($"[global::Uno.NotImplemented({allProperties.GenerateNotImplementedList()})]");
					using (b.BlockInvariant(v))
					{
						if (property.GetMethod != null)
						{
							using (b.BlockInvariant($"get"))
							{
								b.AppendLineInvariant($"throw new global::System.NotSupportedException();");
							}
						}
						if (property.SetMethod != null)
						{
							using (b.BlockInvariant($"set"))
							{
								b.AppendLineInvariant($"throw new global::System.NotSupportedException();");
							}
						}
					}

					using (b.Indent(-b.CurrentLevel))
					{
						b.AppendLineInvariant($"#endif");
					}
				}
				else
				{
					b.AppendLineInvariant($"// Skipping already implement {property}");
				}
			}
		}

		private IPropertySymbol GetMatchingPropertyMember(INamedTypeSymbol androidType, IPropertySymbol property)
		{
			return GetNonGeneratedMembers(androidType, property.Name)
								.OfType<IPropertySymbol>()
								.Where(prop => prop.Parameters.Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).SequenceEqual(property.Parameters.Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))) && prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
								.FirstOrDefault();
		}

		private string GetMethodAccessibility(IMethodSymbol method)
		{
			if (
				method.ContainingType.ToString() == "System.Collections.IEnumerable"
				&& method.Name == "GetEnumerator"
			)
			{
				return "";
			}
			else
			{
				return "public";
			}
		}

		private string TransformType(ITypeSymbol typeSymbol)
		{
			var originalTypeSymbol = typeSymbol;

			if (typeSymbol is INamedTypeSymbol namedType)
			{
				return namedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
			}

			if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
			{
				return arrayTypeSymbol.ElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "[]";
			}

			return typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		}

		protected string BuildInterfaces(INamedTypeSymbol type)
		{
			var ifaces = new List<string>();

			if (HasValidBaseType(type))
			{
				ifaces.Add($"{SanitizeType(type.BaseType)}");
			}

			foreach (var iface in type.Interfaces)
			{
				if (type.Name is "Rect" or "Point" && iface.Name == "IFormattable")
				{
					// Skip for now.
					// This should be fixed in the future. Currently, just removing this condition doesn't work
					// since the generator doesn't properly generate an explicit interface implementation of IFormattable.ToString(string, System.IFormatProvider)
					continue;
				}
				if (iface.DeclaredAccessibility == Accessibility.Public
					&& iface.MetadataName != "Windows.Foundation.IStringable"
					&& iface.MetadataName != "IWinRTObject")
				{
					ifaces.Add(MapUWPTypes(SanitizeType(iface)));
				}
			}

			if (ifaces.Any())
			{
				return $" : {string.Join(", ", ifaces)}";
			}

			return "";
		}

		private static bool HasValidBaseType(INamedTypeSymbol type)
			=> type.BaseType is { } baseType && baseType.SpecialType is not (SpecialType.System_Object or SpecialType.System_ValueType or SpecialType.System_Enum) &&
				!_skipBaseTypes.Contains(type.ToString());

		protected void BuildDelegate(INamedTypeSymbol type, IndentedStringBuilder b, PlatformSymbols<INamedTypeSymbol> types)
		{
			if (types.HasUndefined)
			{
				types.AppendIf(b);

				var IMethodSymbol = type.GetMembers("Invoke").OfType<IMethodSymbol>().First();
				var members = string.Join(", ", IMethodSymbol.Parameters.Select(p => $"{SanitizeType(p.Type)} {SanitizeParameter(p.Name)}"));

				b.AppendLineInvariant($"public delegate {SanitizeType(IMethodSymbol.ReturnType)} {type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}({members});");

				using (b.Indent(-b.CurrentLevel))
				{
					b.AppendLineInvariant($"#endif");
				}
			}
			else
			{
				b.AppendLineInvariant($"// Skipping already declared delegate {type}");
			}
		}

		protected void BuildFields(INamedTypeSymbol type, IndentedStringBuilder b, PlatformSymbols<INamedTypeSymbol> types)
		{
			foreach (var field in type.GetMembers().OfType<IFieldSymbol>())
			{
				if (field.DeclaredAccessibility == Accessibility.Private)
				{
					continue;
				}

				// These are public on WinUI.
				// TODO: Verify whether we want to match the API surface for a bad public API like this.
				if (type.Name == "Point" && field.Name is "_x" or "_y")
				{
					continue;
				}

				if (type.Name == "Rect" && field.Name is "_x" or "_y" or "_width" or "_height")
				{
					continue;
				}

				if (type.Name == "Size" && field.Name is "_width" or "_height")
				{
					continue;
				}

				var allmembers = GetAllGetNonGeneratedMembers(types, field.Name, q => q.FirstOrDefault(f => SymbolMatchingHelpers.AreMatching(field, f)), field);

				if (allmembers.HasUndefined)
				{
					allmembers.AppendIf(b);

					var staticQualifier = field.IsStatic ? "static " : "";

					if (type.TypeKind == TypeKind.Enum)
					{
						var constantValue = field.ConstantValue != null ? $" = {field.ConstantValue}" : string.Empty;

						b.AppendLineInvariant($"{field.Name}{constantValue},");
					}
					else
					{
						b.AppendLineInvariant($"public {staticQualifier}{SanitizeType(field.Type)} {field.Name};");
					}

					using (b.Indent(-b.CurrentLevel))
					{
						b.AppendLineInvariant($"#endif");
					}
				}
				else
				{
					b.AppendLineInvariant($"// Skipping already declared field {field}");
				}
			}
		}

		protected void BuildEvents(INamedTypeSymbol type, IndentedStringBuilder b, PlatformSymbols<INamedTypeSymbol> types)
		{
			foreach (var eventMember in type.GetMembers().OfType<IEventSymbol>())
			{
				if (!IsNotUWPMapping(type, eventMember) || SkipEvent(eventMember))
				{
					continue;
				}

				var allMembers = GetAllMatchingEvents(types, eventMember);

				if (allMembers.HasUndefined)
				{
					allMembers.AppendIf(b);

					var staticQualifier = eventMember.AddMethod.IsStatic ? "static " : "";

					if (type.TypeKind == TypeKind.Interface)
					{
						b.AppendLineInvariant($"{staticQualifier}event {MapUWPTypes(SanitizeType(eventMember.Type))} {eventMember.Name};");
					}
					else
					{
						var notImplementedList = allMembers.GenerateNotImplementedList();
						b.AppendLineInvariant($"[global::Uno.NotImplemented({notImplementedList})]");

						string accessModifier;
						string eventName;
						if (eventMember.ExplicitInterfaceImplementations.IsEmpty)
						{
							eventName = eventMember.Name;
							accessModifier = "public ";
						}
						else
						{
							var explicitImpl = eventMember.ExplicitInterfaceImplementations.Single();
							var interfaceSymbol = (INamedTypeSymbol)explicitImpl.ContainingSymbol;
							eventName = $"{MapUWPTypes(SanitizeType(interfaceSymbol))}.{explicitImpl.Name}";
							accessModifier = string.Empty;
						}

						string declaration = $"{accessModifier}{staticQualifier}event {MapUWPTypes(SanitizeType(eventMember.Type))} {eventName}";

						using (b.BlockInvariant(declaration))
						{
							b.AppendLineInvariant($"[global::Uno.NotImplemented({notImplementedList})]");
							using (b.BlockInvariant($"add"))
							{
								BuildNotImplementedException(b, eventMember, false);
							}
							b.AppendLineInvariant($"[global::Uno.NotImplemented({notImplementedList})]");
							using (b.BlockInvariant($"remove"))
							{
								BuildNotImplementedException(b, eventMember, false);
							}
						}
					}

					using (b.Indent(-b.CurrentLevel))
					{
						b.AppendLineInvariant($"#endif");
					}
				}
				else
				{
					b.AppendLineInvariant($"// Skipping already declared event {eventMember}");
				}
			}
		}

		private void BuildNotImplementedException(IndentedStringBuilder b, ISymbol member, bool forceRaise)
		{
			var typeName = member.ContainingType.ToDisplayString();
			var memberName = member.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

			if (forceRaise)
			{
				b.AppendLineInvariant(
					$"throw new global::System.NotImplementedException(\"The member {memberName} is not implemented. For more information, visit https://aka.platform.uno/notimplemented#m={Uri.EscapeDataString(memberName)}\");"
				);
			}
			else
			{
				b.AppendLineInvariant(
					$"global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented(\"{typeName}\", \"{memberName}\");"
				);
			}
		}

		protected void BuildMethods(INamedTypeSymbol type, IndentedStringBuilder b, PlatformSymbols<INamedTypeSymbol> types, List<IMethodSymbol> writtenMethods)
		{
			foreach (var method in type.GetMembers().OfType<IMethodSymbol>())
			{
				var methods = GetAllMatchingMethods(types, method);

				var parameters = string.Join(", ", method.Parameters.Select(p => $"{GetParameterRefKind(p)}{SanitizeType(p.Type)} {SanitizeParameter(p.Name)}"));
				var staticQualifier = method.IsStatic ? "static " : "";
				var overrideQualifier = method.Name == "ToString" && method.Parameters.IsEmpty ? "override " : "";
				var virtualQualifier = method.IsVirtual ? "virtual " : "";
				var visiblity = method.DeclaredAccessibility.ToString().ToLowerInvariant();

				if (
					method.MethodKind == MethodKind.Constructor
					&& type.TypeKind != TypeKind.Interface
					&& !SkipMethod(type, method)
					&& type.Name != "DependencyObject"
					&& (
						!type.IsValueType
						|| (type.IsValueType && method.Parameters.Length != 0)
					)
				)
				{
					if (methods.HasUndefined)
					{
						methods.AppendIf(b);

						var q = from ctor in type.BaseType?.GetMembers().OfType<IMethodSymbol>()
								where ctor.MethodKind == MethodKind.Constructor
								where ctor.Parameters.Length == 0 // If none, match it we don't care if it's actually called.
								|| ctor
									.Parameters
									.Select(p => p.Type)
									.SequenceEqual(
										method
											.Parameters
											.Take(ctor.Parameters.Length)
											.Select(p => p.Type)
											, InheritanceTypeComparer.Instance
									)
								select ctor;

						var baseParamString = string.Join(", ", q.FirstOrDefault()?.Parameters.Select(p => p.Name) ?? Array.Empty<string>());

						var baseParams = type.BaseType?.SpecialType != SpecialType.System_Object && q.Any() ? $" : base({baseParamString})" : "";

						b.AppendLineInvariant($"[global::Uno.NotImplemented({methods.GenerateNotImplementedList()})]");
						using (b.BlockInvariant($"{visiblity} {type.Name}({parameters}){baseParams}"))
						{
							BuildNotImplementedException(b, method, false);
						}

						using (b.Indent(-b.CurrentLevel))
						{
							b.AppendLineInvariant($"#endif");
						}

						writtenMethods.Add(method);
					}
					else
					{
						b.AppendLineInvariant($"// Skipping already declared method {method}");
					}
				}

				if (
						method.MethodKind == MethodKind.Ordinary
						&& !SkipMethod(type, method)
						&& IsNotUWPMapping(type, method)
						&& (
							method.DeclaredAccessibility == Accessibility.Public
							|| method.DeclaredAccessibility == Accessibility.Protected
						)
					)
				{
					if (methods.HasUndefined)
					{
						methods.AppendIf(b);
						string genericParameters = string.Empty;
						if (method.TypeParameters.Length > 0)
						{
							genericParameters = $"<{string.Join(", ", method.TypeParameters.Select(p => p.Name))}>";
						}

						var declaration = $"{SanitizeType(method.ReturnType)} {method.Name}{genericParameters}({parameters})";

						if (type.TypeKind == TypeKind.Interface || type.Name == "DependencyObject")
						{
							b.AppendLineInvariant($"{declaration};");
						}
						else
						{
							b.AppendLineInvariant($"[global::Uno.NotImplemented({methods.GenerateNotImplementedList()})]");
							using (b.BlockInvariant($"{visiblity} {staticQualifier}{overrideQualifier}{virtualQualifier}{declaration}"))
							{
								var filteredName = method.Name.TrimStart("Get", StringComparison.Ordinal).TrimStart("Set", StringComparison.Ordinal);
								var isAttachedPropertyMethod =
									(method.Name.StartsWith("Get", StringComparison.Ordinal) || method.Name.StartsWith("Set", StringComparison.Ordinal))
									&& method.IsStatic
									&& type
										.GetMembers(filteredName + "Property")
										.OfType<IPropertySymbol>()
										.Where(f => SymbolEqualityComparer.Default.Equals(f.Type, _dependencyPropertySymbol))
										.Any();

								if (isAttachedPropertyMethod)
								{
									var instanceParamName = SanitizeParameter(method.Parameters.First().Name);

									if (method.Name.StartsWith("Get", StringComparison.Ordinal))
									{
										b.AppendLineInvariant($"return ({SanitizeType(method.ReturnType)}){instanceParamName}.GetValue({filteredName}Property);");

									}
									else if (method.Name.StartsWith("Set", StringComparison.Ordinal))
									{
										var valueParamName = SanitizeParameter(method.Parameters.ElementAt(1).Name);
										b.AppendLineInvariant($"{instanceParamName}.SetValue({filteredName}Property, {valueParamName});");
									}
								}
								else
								{
									bool hasReturnValue =
										method.ReturnType.SpecialType != SpecialType.System_Void
										|| method.Parameters.Any(p => p.RefKind == RefKind.Out);

									BuildNotImplementedException(b, method, hasReturnValue);
								}
							}
						}

						using (b.Indent(-b.CurrentLevel))
						{
							b.AppendLineInvariant($"#endif");
						}

						writtenMethods.Add(method);
					}
					else
					{
						b.AppendLineInvariant($"// Skipping already declared method {method}");
					}
				}
				else
				{
					b.AppendLineInvariant($"// Forced skipping of method {method}");
				}
			}
		}

		private bool SkipMethod(INamedTypeSymbol type, IMethodSymbol method)
		{
			if (method.ContainingType.Name == "Grid")
			{
				switch (method.Name)
				{
					// The base type does not match for this parameter until Uno adjusts the
					// hierarchy based on IFrameworkElement.
					case "SetRow":
					case "SetRowSpan":
					case "SetColumn":
					case "SetColumnSpan":
					case "GetRow":
					case "GetRowSpan":
					case "GetColumn":
					case "GetColumnSpan":
						return true;
				}
			}

			if (method.ContainingType.Name == "FrameworkElement")
			{
				switch (method.Name)
				{
					// Those two members are located in DependencyObject but will need to be
					// moved up.
					case "GetBindingExpression":
					case "SetBinding":
						return true;
				}
			}

#if HAS_UNO_WINUI
			if (method.ContainingType.Name == "SwapChainPanel")
			{
				switch (method.Name)
				{
					// This member uses the experimental input layer from UWP
					case "CreateCoreIndependentInputSource":
						return true;
				}
			}

			if (method.ContainingType.Name == "SwapChainPanel")
			{
				switch (method.Name)
				{
					// This member uses the experimental input layer from UWP
					case "CreateCoreIndependentInputSource":
						return true;
				}
			}

			if (method.ContainingType.Name == "VisualInteractionSource")
			{
				switch (method.Name)
				{
					// This member uses the experimental input layer from UWP
					case "TryRedirectForManipulation":
						return true;
				}
			}

			if (method.ContainingType.Name == "UIElement")
			{
				switch (method.Name)
				{
					// This member uses the experimental input layer from UWP
					case "StartDragAsync":
						return true;
				}
			}

			if (method.Name == "FromAbi")
			{
				return true;
			}

			if (method.MethodKind == MethodKind.Constructor && method.Parameters.Length == 1 &&
				method.Parameters[0].Type.Name is "IObjectReference" or "DerivedComposed")
			{
				return true;
			}
#endif

			if (method.ContainingType.Name == "GraphicsCaptureItem")
			{
				switch (method.Name)
				{
					// Will not be implemented in the UWP API set
					case "CreateFromVisual":
						return true;
				}
			}

			if (method.ContainingType.Name == "MediaPlayer")
			{
				switch (method.Name)
				{
					// Will not be implemented in the UWP API set
					case "GetSurface":
						return true;
				}
			}

			if (method.ContainingType.Name == "PalmRejectionDelayZonePreview")
			{
				switch (method.Name)
				{
					// Will not be implemented in the UWP API set
					case "CreateForVisual":
						return true;
				}
			}

			if (method.ContainingType.Name == "ScrollControllerInteractionRequestedEventArgs"
				&& method.MethodKind == MethodKind.Constructor
				&& method.Parameters.Length == 1
				&& method.Parameters[0].Type.ToDisplayString() == "Microsoft.UI.Input.Experimental.ExpPointerPoint")
			{
				// This member uses the experimental input layer from UWP
				return true;
			}

			return false;
		}

		private bool SkipEvent(IEventSymbol eventMember)
		{
			if (eventMember.ContainingType.Name == "FrameworkElement")
			{
				switch (eventMember.Name)
				{
					// Those two members are located in DependencyObject but will need to be
					// moved up.
					case "DataContextChanged":
						return true;
				}
			}
			return false;
		}

		private static string GetParameterRefKind(IParameterSymbol p)
			=> p.RefKind != RefKind.None ? $"{p.RefKind.ToString().ToLowerInvariant()} " : "";

		private bool IsNotUWPMapping(INamedTypeSymbol type, IEventSymbol eventMember)
		{
			foreach (var iface in type.Interfaces.SelectMany(GetAllInterfaces))
			{
				var uwpIface = GetUWPIFace(iface);

				if (uwpIface != null)
				{
					if (
							uwpIface == BaseXamlNamespace + ".Input.ICommand"
							&& eventMember.Name == "CanExecuteChanged"
						)
					{
						return false;
					}
				}
			}

			return true;
		}

		private bool IsNotUWPMapping(INamedTypeSymbol type, IMethodSymbol method)
		{
			foreach (var iface in type.Interfaces.SelectMany(GetAllInterfaces))
			{
				var uwpIface = GetUWPIFace(iface);

				if (uwpIface != null)
				{
					if (
						(
							uwpIface == "Windows.Foundation.Collections.IMap`2"
							&& (
								method.Name == "Clear"
								|| (method.Name == "Remove" && method.ReturnType.Name == "Boolean")
							)
						)
						||
						(
							uwpIface == "Windows.Foundation.Collections.IVector`1"
							&& method.Name == "Clear"
						)
					)
					{
						return true;
					}
					else if (
							uwpIface == "Windows.Foundation.Collections.IVectorView`1"
							&& method.Name == "Item"
						)
					{
						return false;
					}
					else
					{
						var type2 = s_referenceCompilation.GetTypeByMetadataName(uwpIface);

						INamedTypeSymbol build()
						{
							if (iface.TypeArguments.Length != 0)
							{
								return type2.Construct(iface.TypeArguments.ToArray());
							}

							return type2;
						}

						var t3 = build();

						var q = from sourceMethod in t3.GetMembers(method.Name).OfType<IMethodSymbol>()
								where sourceMethod.Parameters.Select(p => p.Type.ToDisplayString()).SequenceEqual(method.Parameters.Select(p => p.Type.ToDisplayString()))
								select sourceMethod;

						if (q.Any())
						{
							return false;
						}
					}
				}
			}

			return true;
		}

		private bool IsNotUWPMapping(INamedTypeSymbol type, IPropertySymbol property)
		{
			try
			{
				foreach (var iface in type.Interfaces.SelectMany(GetAllInterfaces))
				{
					var uwpIface = GetUWPIFace(iface);

					if (uwpIface != null)
					{
						var type2 = s_referenceCompilation.GetTypeByMetadataName(uwpIface);

						var t3 = type2.Construct(iface.TypeArguments.ToArray());

						var q = from sourceProperty in t3.GetMembers(property.Name).OfType<IPropertySymbol>()
								where SymbolEqualityComparer.Default.Equals(sourceProperty.Type, property.Type)
								select sourceProperty;

						if (q.Any())
						{
							return false;
						}
					}
				}

				return true;
			}
			catch (Exception e)
			{
				_ = e;
				return true;
			}
		}

		private string GetUWPIFace(INamedTypeSymbol iface)
		{
			switch (iface.ConstructedFrom.ToDisplayString())
			{
				case "System.Collections.Generic.IEnumerable<T>":
					return "Windows.Foundation.Collections.IIterable`1";
				case "System.Threading.Tasks.Task":
					return "Windows.Foundation.IAsyncOperation";
				case "System.Collections.Generic.IReadOnlyDictionary":
					return "Windows.Foundation.Collections.IMapView";
				case "System.Collections.Generic.IDictionary<TKey, TValue>":
					return "Windows.Foundation.Collections.IMap`2";
				case "System.Collections.Generic.IReadOnlyList<T>":
					return "Windows.Foundation.Collections.IVectorView`1";
				case "System.Collections.Generic.IList<T>":
					return "Windows.Foundation.Collections.IVector`1";
				case "System.Collections.Generic.KeyValuePair":
					return "Windows.Foundation.Collections.IKeyValuePair";
				case "System.Collections.Specialized.INotifyCollectionChanged":
					return "Windows.UI.Xaml.Interop.INotifyCollectionChanged";
#if !HAS_UNO_WINUI
				case "System.Nullable":
					return "Windows.Foundation.IReference";
				case "System.Windows.Input.ICommand":
					return BaseXamlNamespace + ".Input.ICommand";
				case "System.DateTimeOffset":
					return "Windows.Foundation.DateTime";
				case "System.EventHandler":
					return "Windows.Foundation.EventHandler";
				case "System.TimeSpan":
					return "Windows.Foundation.TimeSpan";
				case "System.Type":
					return BaseXamlNamespace + ".Interop.TypeName";
				case "System.Uri":
					return "Windows.Foundation.Uri";
#endif
			}

			return null;
		}

		protected void BuildProperties(INamedTypeSymbol type, IndentedStringBuilder b, PlatformSymbols<INamedTypeSymbol> types)
		{
			foreach (var property in type.GetMembers().OfType<IPropertySymbol>())
			{
				if (SkipProperty(property))
				{
					continue;
				}

				var allMembers = GetAllGetNonGeneratedMembers(types, property.Name, q => q?.FirstOrDefault(p => SymbolMatchingHelpers.AreMatching(property, p)));

				var staticQualifier = ((property.GetMethod?.IsStatic ?? false) || (property.SetMethod?.IsStatic ?? false)) ? "static " : "";

				if (allMembers.HasUndefined)
				{
					allMembers.AppendIf(b);

					if (type.TypeKind == TypeKind.Interface)
					{
						using (b.BlockInvariant($"{MapUWPTypes(SanitizeType(property.Type))} {property.Name}"))
						{
							if (property.GetMethod != null)
							{
								b.AppendLineInvariant($"get;");
							}

							if (property.SetMethod != null)
							{
								b.AppendLineInvariant($"set;");
							}
						}
					}
					else
					{
						b.AppendLineInvariant($"[global::Uno.NotImplemented({allMembers.GenerateNotImplementedList()})]");

						bool isDependencyPropertyDeclaration = property.IsStatic
							&& property.Name.EndsWith("Property", StringComparison.Ordinal)
							&& SymbolEqualityComparer.Default.Equals(property.Type, _dependencyPropertySymbol);

						if (isDependencyPropertyDeclaration)
						{
							var propertyName = property.Name.Substring(0, property.Name.LastIndexOf("Property", StringComparison.Ordinal));

							var getAttached = property.ContainingType.GetMembers("Get" + propertyName).OfType<IMethodSymbol>().FirstOrDefault();
							var getLocal = property.ContainingType.GetMembers(propertyName).OfType<IPropertySymbol>().FirstOrDefault();

							if (getLocal != null || getAttached != null)
							{
								var attachedModifier = getAttached != null ? "Attached" : "";
								var propertyDisplayType = MapUWPTypes((getAttached?.ReturnType ?? getLocal?.Type).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));

								b.AppendLineInvariant($"public {staticQualifier}{SanitizeType(property.Type)} {property.Name} {{{{ get; }}}} =");

								b.AppendLineInvariant($"{BaseXamlNamespace}.DependencyProperty.Register{attachedModifier}(");

								if (getAttached == null)
								{
									b.AppendLineInvariant($"\tnameof({propertyName}), typeof({propertyDisplayType}),");
								}
								else
								{
									//attached properties do not have a corresponding property
									b.AppendLineInvariant($"\t\"{propertyName}\", typeof({propertyDisplayType}),");
								}

								b.AppendLineInvariant($"\ttypeof({property.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}),");
								b.AppendLineInvariant($"\tnew {BaseXamlNamespace}.FrameworkPropertyMetadata(default({propertyDisplayType})));");
							}
							else
							{
								b.AppendLineInvariant($"// Generating stub {property.Name} which has no C# getter");
								b.AppendLineInvariant($"internal static object {property.Name} {{{{ get; }}}}");
							}
						}
						else if (
							!property.IsStatic
							&& property.ContainingType.GetMembers(property.Name + "Property").Any()
						)
						{
							using (b.BlockInvariant($"public {staticQualifier}{MapUWPTypes(SanitizeType(property.Type))} {property.Name}"))
							{
								if (property.GetMethod != null)
								{
									using (b.BlockInvariant($"get"))
									{
										b.AppendLineInvariant($"return ({MapUWPTypes(SanitizeType(property.Type))})this.GetValue({property.Name}Property);");
									}
								}

								if (property.SetMethod != null)
								{
									using (b.BlockInvariant($"set"))
									{
										b.AppendLineInvariant($"this.SetValue({property.Name}Property, value);");
									}
								}
							}
						}
						else
						{
							string accessModifier = property.ExplicitInterfaceImplementations.IsEmpty ? "public " : string.Empty;
							string propertyName;

							if (property.IsIndexer)
							{
								var parms = string.Join(", ", property.GetMethod?.Parameters.Select(p => $"{TransformType(p.Type)} {SanitizeParameter(p.Name)}") ?? Array.Empty<string>());
								propertyName = $"this[{parms}]";
							}
							else
							{
								if (property.ExplicitInterfaceImplementations.IsEmpty)
								{
									propertyName = property.Name;
								}
								else
								{
									var explicitImpl = property.ExplicitInterfaceImplementations.Single();
									var interfaceSymbol = (INamedTypeSymbol)explicitImpl.ContainingSymbol;
									propertyName = $"{MapUWPTypes(SanitizeType(interfaceSymbol))}.{explicitImpl.Name}";
								}
							}

							using (b.BlockInvariant($"{accessModifier}{staticQualifier}{MapUWPTypes(SanitizeType(property.Type))} {propertyName}"))
							{
								if (property.GetMethod != null)
								{
									using (b.BlockInvariant($"get"))
									{
										BuildNotImplementedException(b, property, true);
									}
								}

								if (property.SetMethod != null)
								{
									using (b.BlockInvariant($"set"))
									{
										BuildNotImplementedException(b, property, false);
									}
								}
							}
						}
					}

					using (b.Indent(-b.CurrentLevel))
					{
						b.AppendLineInvariant($"#endif");
					}
				}
				else
				{
					b.AppendLineInvariant($"// Skipping already declared property {property.Name}");
				}
			}
		}

		private bool SkipProperty(IPropertySymbol property)
		{
#if HAS_UNO_WINUI
			if (property.Name.StartsWith("WinRT.IWinRTObject", StringComparison.Ordinal))
			{
				// These are implementations of IWinRTObject interface, which we want to ignore.
				return true;
			}

			if (!property.ExplicitInterfaceImplementations.IsEmpty && property.ExplicitInterfaceImplementations.All(p => p.ContainingSymbol.DeclaredAccessibility != Accessibility.Public))
			{
				// For public types implementing a non-public interface explicitly, the explicit implementation should be skipped.
				return true;
			}
#endif

			if (property.ContainingType.Name == "WebView2")
			{
				switch (property.Name)
				{
					case "CoreWebView2":
						return true;
				}
			}

			if (property.ContainingType.Name == "UIElement")
			{
				switch (property.Name)
				{
					case "Opacity":
					case "OpacityProperty":
					case "Visibility":
					case "VisibilityProperty":
					case "IsHitTestVisible":
					case "IsHitTestVisibleProperty":
					case "Transitions":
					case "TransitionsProperty":
					case "RenderTransform":
					case "RenderTransformProperty":
					case "RenderTransformOrigin":
					case "RenderTransformOriginProperty":
						return true;
				}
			}

			if (property.ContainingType.Name == "FrameworkElement")
			{
				switch (property.Name)
				{
					// This is ignored until DataContext becomes an actual DP.
					case "DataContext":
					case "DataContextProperty":
						return true;
				}
			}

			if (property.ContainingType.Name == "RelativeSource")
			{
				switch (property.Name)
				{
					case "TemplatedParent":
						return true;
				}
			}

			if (property.ContainingType.Name == "ExpCompositionContent")
			{
				switch (property.Name)
				{
					case "InputSite":
						// Member uses the experimental Input layer from UAP
						return true;
				}
			}

			if (property.ContainingType.Name == "ExpCompositionContent")
			{
				switch (property.Name)
				{
					case "InputSite":
						// Member uses the experimental Input layer from UAP
						return true;
				}
			}

			if (property.ContainingType.Name == "ScrollControllerInteractionRequestedEventArgs")
			{
				switch (property.Name)
				{
					case "PointerPoint":
						// Member uses the experimental Input layer from UAP
						return true;
				}
			}

			if (property.ContainingType.Name == "CoreInkPresenterHost")
			{
				switch (property.Name)
				{
					case "RootVisual":
						// Member uses the experimental Input layer from UAP
						return true;
				}
			}

			if (property.ContainingType.Name == "AppWindowFrame")
			{
				switch (property.Name)
				{
					case "DragRegionVisuals":
						// Member uses the experimental Input layer from UAP
						return true;
				}
			}

			if (property.ContainingType.Name == "MediaPlayerSurface")
			{
				switch (property.Name)
				{
					// Will not be implemented in the UWP API set
					case "CompositionSurface":
					case "Compositor":
						return true;
				}
			}

			return false;
		}

		private object SanitizeParameter(string name)
			=> name switch
			{
				"event" => "@event",
				"object" => "@object",
				_ => name
			};

		private string SanitizeType(ITypeSymbol type)
		{
			var result = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

			return result;
		}

		private static string MapUWPTypes(string typeName)
		{
			return typeName switch
			{
				//"global::Windows.Foundation.Collections.IIterable" => "global::System.Collections.Generic.IEnumerable",
				//"global::Windows.Foundation.IAsyncOperation" => "global::System.Threading.Tasks.Task",
				//"global::Windows.Foundation.IAsyncAction" => "global::System.Threading.Tasks.Task",
				//"global::Windows.Foundation.Collections.IMapView" => "global::System.Collections.Generic.IReadOnlyDictionary",
				//"global::Windows.Foundation.Collections.IMap" => "global::System.Collections.Generic.IDictionary",
				//"global::Windows.Foundation.IReference" => "global::System.Nullable",
				//"global::Windows.Foundation.Collections.IVectorView" => "global::System.Collections.Generic.IReadOnlyList",
				//"global::Windows.Foundation.Collections.IVector" => "global::System.Collections.Generic.IList",
				//"global::Windows.Foundation.DateTime" => "global::System.DateTimeOffset",
				//"global::Windows.Foundation.EventHandler" => "global::System.EventHandler",
				//"global::Windows.Foundation.TimeSpan" => "global::System.TimeSpan",
				//"global::Windows.Foundation.Collections.IKeyValuePair" => "global::System.Collections.Generic.KeyValuePair",
				//"global::Windows.UI.Xaml.Interop.TypeName" => "global::System.Type",
				//"global::Windows.Foundation.Uri" => "global::System.Uri",
				//"global::Windows.Foundation.ICloseable" => "global::System.IDisposable",
				"global::Windows.UI.Xaml.Input.ICommand" => "global::System.Windows.Input.ICommand",
				"global::Microsoft.UI.Xaml.Input.ICommand" => "global::System.Windows.Input.ICommand",
				"global::Microsoft.UI.Xaml.Interop.INotifyCollectionChanged" => "global::System.Collections.Specialized.INotifyCollectionChanged",
				"global::Microsoft.UI.Xaml.Data.INotifyPropertyChanged" => "global::System.ComponentModel.INotifyPropertyChanged",
				"global::Microsoft.UI.Xaml.Data.PropertyChangedEventHandler" => "global::System.ComponentModel.PropertyChangedEventHandler",
				_ => typeName,
			};
		}

		private IEnumerable<ISymbol> GetNonGeneratedMembers(ITypeSymbol symbol, string name)
		{
			var current = symbol
				?.GetMembers(name)
				.Where(m => m.Locations.None(l => l.SourceTree?.FilePath?.Contains("\\Generated\\") ?? false)) ?? Array.Empty<ISymbol>();

			foreach (var memberSymbol in current)
			{
				yield return memberSymbol;
			}

			if (
				symbol?.BaseType != null
				&& !SymbolEqualityComparer.Default.Equals(symbol.BaseType, _iOSBaseSymbol)
				&& !SymbolEqualityComparer.Default.Equals(symbol.BaseType, _tvOSBaseSymbol)
				&& !SymbolEqualityComparer.Default.Equals(symbol.BaseType, _androidBaseSymbol)
			)
			{
				foreach (var memberSymbol in GetNonGeneratedMembers(symbol.BaseType, name))
				{
					yield return memberSymbol;
				}
			}
		}

		private IMethodSymbol FindMatchingMethod(ITypeSymbol symbol, IMethodSymbol sourceMethod)
		{
			var q = GetNonGeneratedMembers(symbol, sourceMethod.Name)?.OfType<IMethodSymbol>();

			if (sourceMethod?.ContainingSymbol?.Name == "RelativePanel")
			{
				return q.FirstOrDefault();
			}
			else
			{
				return q.FirstOrDefault(m => SymbolMatchingHelpers.AreMatching(sourceMethod, m));
			}
		}

		static Dictionary<(string projectFile, string targetFramework), Compilation> _projects
			= new();

		private static async Task<Compilation> LoadProject(string projectFile, string targetFramework)
		{
			var key = (projectFile, targetFramework);

			if (_projects.TryGetValue(key, out var compilation))
			{
				Console.WriteLine($"Using cached compilation for {projectFile} and {targetFramework}");
				return compilation;
			}

			compilation = await InnerLoadProject(projectFile, targetFramework);
			_projects[key] = compilation;
			var externalCompilationReferences = compilation.ExternalReferences.OfType<CompilationReference>().Select(r => r.Display).ToArray();
			string[] expectedRefs = ["Uno.Foundation", "Uno", "Uno.UI.Composition", "Uno.UI.Dispatching"];
			foreach (var expectedRef in expectedRefs)
			{
				if (!externalCompilationReferences.Contains(expectedRef))
				{
					// If you hit this, ensure projectFile was restored. If it wasn't and `obj/project.assets.json` is missing,
					// the target IncludeTransitiveProjectReferences will not be run, and we can end up with missing assemblies.
					// Another reason for hitting this is if you define UnoTargetFrameworkOverride, say to net7.0.
					// This will cause project.assets.json to have `net7.0` instead of `net7.0-platform` which
					// breaks ResolvePackageAssets target and cause it to not produce TransitiveProjectReferences
					throw new Exception($"{expectedRef} not found when loading '{projectFile} ({targetFramework})'");
				}
			}

			return compilation;
		}

		private static async Task<Compilation> LoadUWPReferenceProject(string referencesFile)
		{
			var ws = new AdhocWorkspace();

			var p = ws.AddProject("uwpref", LanguageNames.CSharp);
#if HAS_UNO_WINUI
			_winuiReferences = File.ReadAllLines(referencesFile).Select(reference => MetadataReference.CreateFromFile(reference));
			// Add .NET 7 ref assemblies to make sure things like System.Object are properly resolved and are not error symbols.
			p = p.AddMetadataReferences(_winuiReferences.Concat(Basic.Reference.Assemblies.Net70.References.All));
#else
			p = p.AddMetadataReferences(File.ReadAllLines(referencesFile).Select(reference => MetadataReference.CreateFromFile(reference)));
#endif
			return await p.GetCompilationAsync();
		}

		private static async Task<Compilation> InnerLoadProject(string projectFile, string targetFramework)
		{
			var projectFileName = Path.GetFileName(projectFile);
			Console.WriteLine($"Loading for {targetFramework}: {projectFileName}");

			var properties = new Dictionary<string, string>
			{
				// { "VisualStudioVersion", "15.0" },
				// Important to load with Release.
				// The projects should be restored for the generator to function properly.
				// The BuildSyncGenerator target in Uno.UI.Build.csproj will restore for Release.
				{ "Configuration", "Release" },
				//{ "BuildingInsideVisualStudio", "true" },
				{ "SkipUnoResourceGeneration", "true" }, // Required to avoid loading a non-existent task
				{ "DocsGeneration", "true" }, // Detect that source generation is running
				{ "LangVersion", CSharpLangVersion },
				{ "NoBuild", "True" },
				{ "RunAnalyzers", "false" },
				{ "SyncGeneratorRunning", "true" }
				//{ "DesignTimeBuild", "true" },
				//{ "UseHostCompilerIfAvailable", "false" },
				//{ "UseSharedCompilation", "false" },
			};

			if (targetFramework != null)
			{
				properties.Add("TargetFramework", targetFramework);
			}

			var ws = MSBuildWorkspace.Create(properties);

			ws.LoadMetadataForReferencedProjects = true;

			ws.WorkspaceFailed +=
				(s, e) => Console.WriteLine(e.Diagnostic.ToString());

			// NOTE: msbuildLogger doesn't work in 4.9
			// https://github.com/dotnet/roslyn/issues/72202
			// https://github.com/dotnet/roslyn/discussions/71950
			var project = await ws.OpenProjectAsync(projectFile, msbuildLogger: new BinaryLogger() { Parameters = Path.Combine(Directory.GetCurrentDirectory(), "binlogs", $"{projectFileName}_{targetFramework}.binlog") });

			var metadataLessProjects = ws
				.CurrentSolution
				.Projects
				.Where(p => p.MetadataReferences.None())
				.ToArray();

			if (metadataLessProjects.Length > 0)
			{
				// In this case, this may mean that Rolsyn failed to execute some msbuild task that loads the
				// references in a UWA project (or NuGet 3.0+ with project.json, more specifically). For these
				// projects, references are materialized through a task using a output parameter that injects
				// "References" nodes. If this task fails, no references are loaded, and simple type resolution
				// such "int?" may fail.

				// Additionally, it may happen that projects are loaded using the callee's Configuration/Platform, which
				// may not exist in all projects. This can happen if the project does not have a proper
				// fallback mechanism in place.

				SourceGeneration.Host.ProjectLoader.LoadProjectDetails(projectFile, "Debug");

				throw new InvalidOperationException(
					$"The project(s) {metadataLessProjects.Select(p => p.Name).JoinBy(",")} did not provide any metadata reference. " +
					"This may be due to an invalid path, such as $(SolutionDir) being used in the csproj; try using relative paths instead." +
					"This may also be related to a missing default configuration directive. Refer to the Uno.SourceGenerator Readme.md file for more details."
				);
			}

			return await project.GetCompilationAsync();
		}

		public static IEnumerable<INamedTypeSymbol> GetNamespaceTypes(INamespaceSymbol sym)
		{
			foreach (var child in sym.GetTypeMembers())
			{
				yield return child;
			}

			foreach (var ns in sym.GetNamespaceMembers())
			{
				foreach (var child2 in GetNamespaceTypes(ns))
				{
					yield return child2;
				}
			}
		}

		private static void RegisterAssemblyLoader()
		{
			// Force assembly loader to consider siblings, when running in a separate appdomain.
			ResolveEventHandler localResolve = (s, e) =>
			{
				if (e.Name == "Mono.Runtime")
				{
					// Roslyn 2.0 and later checks for the presence of the Mono runtime
					// through this check.
					return null;
				}

				var assembly = new AssemblyName(e.Name);
				var basePath = Path.GetDirectoryName(new Uri(typeof(Generator).Assembly.Location).LocalPath);

				Console.WriteLine($"Searching for [{assembly}] from [{basePath}]");

				// Ignore resource assemblies for now, we'll have to adjust this
				// when adding globalization.
				if (assembly.Name.EndsWith(".resources", StringComparison.Ordinal))
				{
					return null;
				}

				// Lookup for the highest version matching assembly in the current app domain.
				// There may be an existing one that already matches, even though the
				// fusion loader did not find an exact match.
				var loadedAsm = (
									from asm in AppDomain.CurrentDomain.GetAssemblies()
									where asm.GetName().Name == assembly.Name
									orderby asm.GetName().Version descending
									select asm
								).ToArray();

				if (loadedAsm.Length > 1)
				{
					var duplicates = loadedAsm
						.Skip(1)
						.Where(a => a.GetName().Version == loadedAsm[0].GetName().Version)
						.ToArray();

					if (duplicates.Length != 0)
					{
						Console.WriteLine($"Selecting first occurrence of assembly [{e.Name}] which can be found at [{duplicates.Select(d => d.Location).JoinBy("; ")}]");
					}

					return loadedAsm[0];
				}
				else if (loadedAsm.Length == 1)
				{
					return loadedAsm[0];
				}

				Assembly LoadAssembly(string filePath)
				{
					if (File.Exists(filePath))
					{
						try
						{
							var output = Assembly.LoadFrom(filePath);

							Console.WriteLine($"Loaded [{output.GetName()}] from [{output.Location}]");

							return output;
						}
						catch (Exception ex)
						{
							Console.WriteLine($"Failed to load [{assembly}] from [{filePath}]", ex);
							return null;
						}
					}
					else
					{
						return null;
					}
				}

				var paths = new[] {
					Path.Combine(basePath, assembly.Name + ".dll"),
					Path.Combine(MSBuildBasePath, assembly.Name + ".dll"),
				};

				return paths
					.Select(LoadAssembly)
					.Where(p => p != null)
					.FirstOrDefault();
			};

			AppDomain.CurrentDomain.AssemblyResolve += localResolve;
			AppDomain.CurrentDomain.TypeResolve += localResolve;
		}
	}
}
