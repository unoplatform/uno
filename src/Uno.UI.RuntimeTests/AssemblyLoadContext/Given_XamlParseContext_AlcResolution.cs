#if HAS_UNO
#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.AssemblyLoadContext;

/// <summary>
/// Covers how <see cref="XamlParseContext.AssemblyLoadContext"/>'s lazy by-<c>AssemblyName</c>
/// resolution behaves when one library is loaded into more than one ALC — the normal shape for
/// anything a host and the apps it hosts both reference — and what the resolver does with the answer.
///
/// Why that matters: <c>ResourceResolver.TryTopLevelRetrieval</c> keys its entire priority order on
/// that ALC. A context that resolves to its owning secondary ALC queries that app's
/// <c>Application.Resources</c> first (step 1). A context that resolves to the default ALC, or to no
/// ALC at all, is treated as a host context: <c>Application.Current</c> is queried first (step 3),
/// with secondary apps reached only by the last-resort scan that runs when the host has NO match. So
/// for XAML the secondary app owns, a host resource sharing a key wins at parse time — no warning, no
/// unresolved-resource log.
///
/// A parse context only carries its ALC when the XAML codegen ran with
/// <c>UnoEnableAlcAppSupport</c>: <c>XamlCodeGeneration.BuildTopLevelResourceDictionary</c> emits the
/// <c>AssemblyLoadContext =</c> initializer under that flag alone, and every other build emits
/// <c>AssemblyName</c> by itself. A library shipped prebuilt — as any packaged control library is —
/// therefore always takes the lazy scan, however the consuming app was built. The name identifies the
/// assembly, not the copy, so when several copies are loaded no answer is right for every caller.
///
/// The contract these tests pin:
/// <list type="bullet">
/// <item><see cref="When_AssemblyIsLoadedInHostAndSecondaryAlc_Then_ParseContextIsAmbiguous"/>: a
/// name-only context for a multiply-loaded assembly reports itself ambiguous and resolves to no ALC,
/// rather than latching whichever copy the scan meets first.</item>
/// <item><see cref="When_ParseContextCarriesOnlyAssemblyName_Then_HostShadowsOwningAppResource"/> and
/// <see cref="When_OwningAppHoldsTheKeyInAMergedDictionary_Then_OnlyTheStampDecides"/>: at the
/// top-level lookup, that ambiguous context is still answered by the host — the resolver cannot
/// attribute it to an app — while a stamped context reads the owning app's value.</item>
/// <item><see cref="When_AmbiguousContextResolvesToHostAtParseTime_Then_LoadingUnderAlcContentHostRestoresOwningAppValue"/>
/// and <see cref="When_XamlReaderLoadsUnderAlcContentHost_Then_StaticResourceReadsOwningAppValue"/>:
/// the parse-time answer is provisional. <c>ResourceResolver.ApplyResource</c> keeps the
/// <c>StaticResourceLoading</c> reason for a null or ambiguous context while secondary apps exist, so
/// the load-time tree walk re-resolves under <c>AlcContentHost</c>, whose projection of the owning
/// app's dictionaries sits ahead of the host's.</item>
/// </list>
/// </summary>
[TestClass]
[RunsOnUIThread]
[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Types manipulated here have been marked earlier")]
[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Types manipulated here have been marked earlier")]
[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Types manipulated here have been marked earlier")]
[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Types manipulated here have been marked earlier")]
public class Given_XamlParseContext_AlcResolution
{
	/// <summary>Owned by this test class alone, so no other suite's seeded value can answer the lookup.</summary>
	private const string SharedKey = "XamlParseContextAlcResolutionColor";

	/// <summary>The value the OWNING secondary app defines — what correct resolution must return.</summary>
	private static readonly Windows.UI.Color AppValue = Windows.UI.Color.FromArgb(0xFF, 0x11, 0x22, 0x33);

	/// <summary>The value the HOST defines under the same key — the shadowing value.</summary>
	private static readonly Windows.UI.Color HostValue = Windows.UI.Color.FromArgb(0xFF, 0xAA, 0xBB, 0xCC);

	/// <summary>A third value, defined only in the dictionary that declares the XAML being parsed.</summary>
	private static readonly Windows.UI.Color DeclaringDictionaryValue = Windows.UI.Color.FromArgb(0xFF, 0x44, 0x55, 0x66);

	/// <summary>A key holding a <see cref="Style"/> in both the host and the owning app, for the template-part tests.</summary>
	private const string StyleKey = "XamlParseContextAlcResolutionButtonStyle";

	// The ALC standing in for a hosted app: it owns the registered Application, and it holds its own
	// private copy of the probe assembly whose name the parse context carries.
	private TestAssemblyLoadContext? _ownerAlc;

	// Simple name of an assembly that is loaded in BOTH the default ALC and _ownerAlc. Discovered at
	// run time rather than hard-coded — see PickHostLoadedProbeAssemblyName for the constraints.
	private string? _probeAssemblyName;

	private bool _seededHostValue;
	private bool _hadPriorHostValue;
	private object? _priorHostValue;
	private bool _usedWindowContent;

	[TestCleanup]
	public async Task Cleanup()
	{
		if (_usedWindowContent)
		{
			// Release the elements that hold owner-ALC values before the ALC is unloaded.
			TestServices.WindowHelper.WindowContent = new Border();
			await TestServices.WindowHelper.WaitForIdle();
			_usedWindowContent = false;
		}

		if (_seededHostValue)
		{
			if (_hadPriorHostValue)
			{
				Application.Current.Resources[SharedKey] = _priorHostValue;
			}
			else
			{
				Application.Current.Resources.Remove(SharedKey);
			}

			_seededHostValue = false;
		}

		_ownerAlc?.Unload();
		_ownerAlc = null;
		_probeAssemblyName = null;

		// Matches Given_AlcContentHost.Cleanup: the flag is a one-way latch gating all secondary-app
		// resource fallback, so leaving it set makes every later non-ALC test consult this run's
		// now-stale registrations.
		Application.HasSecondaryApps = false;
	}

	/// <summary>
	/// The mechanism. With one assembly loaded in both the default ALC and the app's, a context
	/// carrying only <c>AssemblyName</c> cannot tell which copy it belongs to — the name is all it
	/// has to go on. It must say so, by reporting ambiguity and resolving to no ALC, rather than take
	/// the first copy the scan meets: that is the host's, and it would silently turn the app's XAML
	/// into a host lookup.
	/// </summary>
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_AssemblyIsLoadedInHostAndSecondaryAlc_Then_ParseContextIsAmbiguous()
	{
		await SetUpDuplicatedAssemblyAsync();

		var nameOnlyContext = new XamlParseContext
		{
			AssemblyName = _probeAssemblyName,
		};

		Assert.IsTrue(
			nameOnlyContext.IsAssemblyLoadContextAmbiguous,
			$"'{_probeAssemblyName}' is loaded in two ALCs, so a name-only context must report the " +
			$"ambiguity instead of guessing a copy.{DescribeLoadedCopies()}");

		Assert.IsNull(
			nameOnlyContext.AssemblyLoadContext,
			"An ambiguous context must not resolve to any ALC. Resolving to one means the scan latched " +
			$"a copy it cannot justify.{DescribeLoadedCopies()}");

		// Control: an assembly with a single loaded copy is not ambiguous and resolves to that copy.
		// Uno.UI is shared with the owner ALC by TestAssemblyLoadContext, so only the host's copy exists.
		var singleCopyContext = new XamlParseContext
		{
			AssemblyName = typeof(Application).Assembly.GetName().Name,
		};

		Assert.IsFalse(singleCopyContext.IsAssemblyLoadContextAmbiguous, "Control: a single-copy assembly is unambiguous.");
		Assert.AreSame(
			global::System.Runtime.Loader.AssemblyLoadContext.Default,
			singleCopyContext.AssemblyLoadContext,
			"Control: a single-copy assembly resolves to the ALC that holds it.");

		// Control: an explicit stamp is never ambiguous, whatever is loaded.
		var stampedContext = new XamlParseContext
		{
			AssemblyName = _probeAssemblyName,
			AssemblyLoadContext = _ownerAlc,
		};

		Assert.IsFalse(stampedContext.IsAssemblyLoadContextAmbiguous, "Control: a stamped context is unambiguous.");
		Assert.AreSame(_ownerAlc, stampedContext.AssemblyLoadContext, "Control: a stamped context resolves to its stamp.");
	}

	/// <summary>
	/// The correction, in the shape generated XAML takes. A name-only ambiguous context is answered by
	/// the host at parse time — that part is unchanged — but <c>ApplyResource</c> keeps the
	/// <c>StaticResourceLoading</c> reason, so when the element loads under an <c>AlcContentHost</c>
	/// projecting the owning app's dictionaries, the tree walk finds the app's value first and applies it.
	/// </summary>
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_AmbiguousContextResolvesToHostAtParseTime_Then_LoadingUnderAlcContentHostRestoresOwningAppValue()
	{
		var ownerApp = await SetUpDuplicatedAssemblyAsync();

		var nameOnlyContext = new XamlParseContext { AssemblyName = _probeAssemblyName };

		Assert.IsTrue(
			ResourceResolver.ShouldDeferStaticResourceToLoading(nameOnlyContext),
			"A name-only context for a multiply-loaded assembly must be deferred to load time while secondary apps exist.");
		Assert.IsFalse(
			ResourceResolver.ShouldDeferStaticResourceToLoading(new XamlParseContext { AssemblyLoadContext = global::System.Runtime.Loader.AssemblyLoadContext.Default }),
			"A context stamped with the default ALC is a host lookup and is final at parse time.");
		Assert.IsFalse(
			ResourceResolver.ShouldDeferStaticResourceToLoading(new XamlParseContext { AssemblyLoadContext = _ownerAlc }),
			"A context stamped with a secondary ALC is attributed to its app at parse time and is final.");

		var host = new AlcContentHost { SourceApplicationOverride = ownerApp };
		var target = new Border { Width = 50, Height = 50 };
		host.Content = target;

		// What the generated code emits for Tag="{StaticResource Key}" in the library's XAML.
		ResourceResolver.ApplyResource(
			target,
			FrameworkElement.TagProperty,
			SharedKey,
			isThemeResourceExtension: false,
			isHotReloadSupported: false,
			fromXamlParser: true,
			nameOnlyContext);

		Assert.IsInstanceOfType<Windows.UI.Color>(target.Tag);
		Assert.AreEqual(
			HostValue,
			(Windows.UI.Color)target.Tag,
			"Parse time: the resolver cannot attribute the lookup to an app, so the host answers provisionally.");

		_usedWindowContent = true;
		TestServices.WindowHelper.WindowContent = host;
		await TestServices.WindowHelper.WaitForLoaded(target);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsInstanceOfType<Windows.UI.Color>(target.Tag);
		Assert.AreEqual(
			AppValue,
			(Windows.UI.Color)target.Tag,
			"Load time: under an AlcContentHost projecting the owning app's dictionaries, the tree walk " +
			$"must replace the provisional host value with the app's.{DescribeLoadedCopies()}");
	}

	/// <summary>
	/// The correction must survive a resource refresh that runs before the element has a parent. Generated
	/// template code calls <c>CreationComplete</c> on each part while it is still detached; applying its style
	/// there refreshes every resource binding with nothing in scope. That refresh must not settle the provisional
	/// lookup, or nothing is left for the load-time walk. Outside an <c>AlcContentHost</c> the walk finds nothing
	/// in scope and falls back to the host's style.
	/// </summary>
	[TestMethod]
	[DataRow(true)]
	[DataRow(false)]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_TemplatePartCompletesBeforeItIsParented_Then_LoadTimeScopeDecidesItsStyle(bool underAlcContentHost)
	{
		var ownerApp = await SetUpDuplicatedAssemblyAsync();
		var (appStyle, hostStyle) = SeedStyles(ownerApp);

		try
		{
			var button = CreateTemplatePartCompletedWhileDetached();

			Assert.AreSame(hostStyle, button.Style, "State before loading: the provisional host value stands.");

			await LoadAsync(button, underAlcContentHost ? ownerApp : null);

			Assert.AreSame(
				underAlcContentHost ? appStyle : hostStyle,
				button.Style,
				underAlcContentHost
					? $"A refresh while detached must not settle the provisional lookup: under the AlcContentHost the owning app's style must win.{DescribeLoadedCopies()}"
					: $"Outside an AlcContentHost the load-time walk must fall back to the host's style.{DescribeLoadedCopies()}");
		}
		finally
		{
			Application.Current.Resources.Remove(StyleKey);
		}
	}

	/// <summary>
	/// The same, one level down: a brush owned by a detached template part is refreshed with its owner, so it
	/// must also keep its provisional binding until it enters the live tree.
	/// </summary>
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_TemplatePartBrushCompletesBeforeItIsParented_Then_LoadingUnderAlcContentHostRestoresOwningAppValue()
	{
		var ownerApp = await SetUpDuplicatedAssemblyAsync();

		var brush = new SolidColorBrush();
		ResourceResolver.ApplyResource(
			brush,
			SolidColorBrush.ColorProperty,
			SharedKey,
			isThemeResourceExtension: false,
			isHotReloadSupported: false,
			fromXamlParser: true,
			new XamlParseContext { AssemblyName = _probeAssemblyName });

		// What generated template code does to a part: parse it, then complete it while detached, which applies its style.
		var border = new Border { IsParsing = true, Width = 50, Height = 50, Background = brush, Style = new Style(typeof(Border)) };
		border.CreationComplete();

		Assert.AreEqual(HostValue, brush.Color, "State before loading: the provisional host value stands.");

		await LoadAsync(border, ownerApp);

		Assert.AreEqual(
			AppValue,
			brush.Color,
			$"A brush refreshed with its detached owner must keep its provisional lookup for the load-time walk.{DescribeLoadedCopies()}");
	}

	/// <summary>
	/// Only the top-level lookup is provisional. A key found in the dictionary that declares the XAML is where a
	/// {StaticResource} resolves by definition, so the load-time walk must not replace it with an ancestor's value.
	/// </summary>
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_DeclaringDictionaryDefinesTheKey_Then_ParseTimeValueIsFinalUnderAlcContentHost()
	{
		var ownerApp = await SetUpDuplicatedAssemblyAsync();

		var declaringDictionary = new ResourceDictionary { [SharedKey] = DeclaringDictionaryValue };
		var target = new Border { Width = 50, Height = 50 };

		ResourceResolver.PushSourceToScope(declaringDictionary);
		try
		{
			ResourceResolver.ApplyResource(
				target,
				FrameworkElement.TagProperty,
				SharedKey,
				isThemeResourceExtension: false,
				isHotReloadSupported: false,
				fromXamlParser: true,
				new XamlParseContext { AssemblyName = _probeAssemblyName });
		}
		finally
		{
			ResourceResolver.PopSourceFromScope();
		}

		Assert.AreEqual(DeclaringDictionaryValue, (Windows.UI.Color)target.Tag, "Parse time: the declaring dictionary answers first.");

		await LoadAsync(target, ownerApp);

		Assert.AreEqual(
			DeclaringDictionaryValue,
			(Windows.UI.Color)target.Tag,
			$"A value from the declaring dictionary is final: the AlcContentHost's projection must not replace it.{DescribeLoadedCopies()}");
	}

	/// <summary>
	/// The template-part correction through a real <c>ControlTemplate</c>. <c>XamlReader.Load</c> passes no parse
	/// context, so every {StaticResource} in the template is provisional while secondary apps exist.
	/// </summary>
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_XamlReaderTemplateLoadsUnderAlcContentHost_Then_TemplatePartsReadOwningAppValues()
	{
		var ownerApp = await SetUpDuplicatedAssemblyAsync();
		var (appStyle, _) = SeedStyles(ownerApp);

		try
		{
			var control = (ContentControl)XamlReader.Load(
				"<ContentControl xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>" +
				"<ContentControl.Template><ControlTemplate TargetType='ContentControl'>" +
				"<Border Width='100' Height='50'><Border.Background>" +
				$"<SolidColorBrush Color='{{StaticResource {SharedKey}}}' /></Border.Background>" +
				$"<Button Style='{{StaticResource {StyleKey}}}' Content='Primary' />" +
				"</Border></ControlTemplate></ContentControl.Template></ContentControl>");

			await LoadAsync(control, ownerApp);

			var border = control.FindFirstChild<Border>();
			var button = control.FindFirstChild<Button>();
			Assert.IsNotNull(border, "Sanity: the template must have been applied.");
			Assert.IsNotNull(button, "Sanity: the template must have been applied.");

			Assert.AreSame(appStyle, button.Style, $"A template part's style must come from the owning app.{DescribeLoadedCopies()}");
			Assert.AreEqual(
				AppValue,
				((SolidColorBrush)border.Background).Color,
				$"A template part's brush must take the owning app's value.{DescribeLoadedCopies()}");
		}
		finally
		{
			Application.Current.Resources.Remove(StyleKey);
		}
	}

	/// <summary>
	/// The same correction for XAML materialised through <c>XamlReader.Load</c>, which passes no parse
	/// context at all. That is how a designer creates and edits elements inside a hosted app; without
	/// the deferral, every <c>{StaticResource}</c> it writes is a host lookup.
	/// </summary>
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_XamlReaderLoadsUnderAlcContentHost_Then_StaticResourceReadsOwningAppValue()
	{
		var ownerApp = await SetUpDuplicatedAssemblyAsync();

		Assert.IsTrue(
			ResourceResolver.ShouldDeferStaticResourceToLoading(context: null),
			"A lookup with no parse context cannot be attributed to an app and must be deferred while secondary apps exist.");

		var target = (Border)XamlReader.Load(
			$"<Border xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
			$"xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
			$"Width='50' Height='50' Tag='{{StaticResource {SharedKey}}}' />");

		Assert.IsInstanceOfType<Windows.UI.Color>(target.Tag);
		Assert.AreEqual(
			HostValue,
			(Windows.UI.Color)target.Tag,
			"Parse time: XamlReader passes no context, so the host answers provisionally.");

		var host = new AlcContentHost { SourceApplicationOverride = ownerApp, Content = target };

		_usedWindowContent = true;
		TestServices.WindowHelper.WindowContent = host;
		await TestServices.WindowHelper.WaitForLoaded(target);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsInstanceOfType<Windows.UI.Color>(target.Tag);
		Assert.AreEqual(
			AppValue,
			(Windows.UI.Color)target.Tag,
			"Load time: a XamlReader-built element under an AlcContentHost must take the owning app's value " +
			$"once loaded.{DescribeLoadedCopies()}");
	}

	/// <summary>
	/// The consequence, with its own control. One key, one resolver, one registered secondary app,
	/// read through two parse contexts that differ ONLY in whether the ALC is stamped — and they
	/// return different values: the stamped context reads the owning app's, the name-only context
	/// reads the host's.
	/// </summary>
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_ParseContextCarriesOnlyAssemblyName_Then_HostShadowsOwningAppResource()
	{
		var ownerApp = await SetUpDuplicatedAssemblyAsync();

		Assert.IsTrue(
			ownerApp.Resources.TryGetValue(SharedKey, out var seeded) && Equals(seeded, AppValue),
			"Sanity: the owning secondary app must hold the key before the lookups run.");
		Assert.IsTrue(
			Application.Current.Resources.TryGetValue(SharedKey, out var hostSeeded) && Equals(hostSeeded, HostValue),
			"Sanity: the host must hold a DIFFERENT value under the same key, or the two lookups below " +
			"cannot be told apart.");

		var key = new SpecializedResourceDictionary.ResourceKey(SharedKey);

		// Control — what the codegen emits WITH UnoEnableAlcAppSupport. The ALC is stamped, so
		// TryTopLevelRetrieval takes step 1 and the owning app answers.
		var stampedContext = new XamlParseContext
		{
			AssemblyLoadContext = _ownerAlc,
		};

		Assert.IsTrue(
			ResourceResolver.TryTopLevelRetrieval(key, stampedContext, out var stampedValue),
			"Control lookup should succeed — both the host and the owning app define the key.");
		Assert.IsInstanceOfType<Windows.UI.Color>(stampedValue);
		Assert.AreEqual(
			AppValue,
			(Windows.UI.Color)stampedValue,
			"Control: a parse context with its ALC stamped must read the OWNING app's value. Reading " +
			"the host's here means step 1 of TryTopLevelRetrieval (contextApp.Resources before " +
			"Application.Current) is broken, and the assertion below would prove nothing about the stamp.");

		// Subject — what the codegen emits WITHOUT the flag: the name alone, so the scan lands on the
		// host's copy and the lookup is treated as a host context.
		var nameOnlyContext = new XamlParseContext
		{
			AssemblyName = _probeAssemblyName,
		};

		Assert.IsTrue(
			ResourceResolver.TryTopLevelRetrieval(key, nameOnlyContext, out var nameOnlyValue),
			"Subject lookup should succeed — the host defines the key.");
		Assert.IsInstanceOfType<Windows.UI.Color>(nameOnlyValue);
		Assert.AreEqual(
			HostValue,
			(Windows.UI.Color)nameOnlyValue,
			"A name-only parse context read the owning app's value at the TOP-LEVEL lookup. The resolver " +
			"has no owning app to attribute an ambiguous context to, so the host must answer here; the " +
			"correction belongs to the load-time walk (see the AlcContentHost tests), not to this lookup. " +
			$"If the resolver learned to attribute ambiguous contexts, revisit the whole contract.{DescribeLoadedCopies()}");
	}

	/// <summary>
	/// Same contrast, but with the owning app's key where Uno.Themes actually puts it: inside a
	/// dictionary merged into <c>Application.Resources</c>, not a direct entry. This is the traversal
	/// the real case depends on — <c>TryTopLevelRetrieval</c> calls
	/// <c>contextApp.Resources.TryGetValue(key, shouldCheckSystem: false)</c>, which reaches merged
	/// dictionaries only via <c>GetFromMerged</c>. If that path did NOT reach the key, the host would
	/// win for every context shape, stamped or not, and the ALC stamp would be irrelevant to the
	/// reported symptom. It does reach it, so the stamp is the whole difference.
	/// </summary>
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_OwningAppHoldsTheKeyInAMergedDictionary_Then_OnlyTheStampDecides()
	{
		var ownerApp = await SetUpDuplicatedAssemblyAsync(seedViaMergedDictionary: true);

		// ContainsKeyLocal probes _values only. The public ContainsKey traverses merged dictionaries,
		// theme dictionaries AND system resources, so it cannot distinguish "direct entry" from
		// "reachable" — which is exactly the distinction this test is built on.
		Assert.IsFalse(
			ownerApp.Resources.ContainsKeyLocal(SharedKey),
			"Sanity: the key must NOT be a direct entry — the point of this test is the merged-dictionary " +
			"traversal that Uno.Themes' semantic styles actually take.");
		Assert.IsTrue(
			ownerApp.Resources.TryGetValue(SharedKey, out var viaMerged) && Equals(viaMerged, AppValue),
			"Sanity: the key must still be reachable from Application.Resources through GetFromMerged.");

		var key = new SpecializedResourceDictionary.ResourceKey(SharedKey);

		var stampedContext = new XamlParseContext { AssemblyLoadContext = _ownerAlc };
		Assert.IsTrue(
			ResourceResolver.TryTopLevelRetrieval(key, stampedContext, out var stampedValue),
			"Stamped lookup should succeed.");
		Assert.IsInstanceOfType<Windows.UI.Color>(stampedValue);
		Assert.AreEqual(
			AppValue,
			(Windows.UI.Color)stampedValue,
			"A stamped context must read the owning app's value even when that value lives in a MERGED " +
			"dictionary. Reading the host's here would mean step 1 never reaches a theme's styles, and " +
			"the host would shadow the app regardless of the ALC stamp.");

		var nameOnlyContext = new XamlParseContext { AssemblyName = _probeAssemblyName };
		Assert.IsTrue(
			ResourceResolver.TryTopLevelRetrieval(key, nameOnlyContext, out var nameOnlyValue),
			"Name-only lookup should succeed — the host defines the key.");
		Assert.IsInstanceOfType<Windows.UI.Color>(nameOnlyValue);
		Assert.AreEqual(
			HostValue,
			(Windows.UI.Color)nameOnlyValue,
			$"The merged-dictionary shape must behave exactly as the direct-entry one.{DescribeLoadedCopies()}");
	}

	/// <summary>
	/// Builds the secondary-ALC fixture: an <c>AlcTestApp.AppC</c> registered for
	/// <see cref="_ownerAlc"/> with <see cref="SharedKey"/> seeded to <see cref="AppValue"/> against a
	/// host entry of <see cref="HostValue"/>, plus a private copy — inside that same ALC — of an
	/// assembly the host already has loaded, whose name <see cref="_probeAssemblyName"/> carries.
	/// </summary>
	// Seeds StyleKey with a different Style in the owning app and in the host; the caller removes the host entry.
	private static (Style App, Style Host) SeedStyles(Application ownerApp)
	{
		var appStyle = new Style(typeof(Button));
		var hostStyle = new Style(typeof(Button));
		ownerApp.Resources[StyleKey] = appStyle;
		Application.Current.Resources[StyleKey] = hostStyle;
		return (appStyle, hostStyle);
	}

	// What generated template code emits for <Button Style="{StaticResource StyleKey}" />: parse, apply the
	// resource, then complete the part while it is still detached.
	private Button CreateTemplatePartCompletedWhileDetached()
	{
		var button = new Button { IsParsing = true, Content = "Primary" };
		ResourceResolver.ApplyResource(
			button,
			FrameworkElement.StyleProperty,
			StyleKey,
			isThemeResourceExtension: false,
			isHotReloadSupported: false,
			fromXamlParser: true,
			new XamlParseContext { AssemblyName = _probeAssemblyName });
		button.CreationComplete();

		Assert.IsNull(button.Parent, "Sanity: CreationComplete must run while the element has no parent.");
		return button;
	}

	// Loads the element under an AlcContentHost projecting sourceApp, or under a plain Border when sourceApp is null.
	private async Task LoadAsync(FrameworkElement element, Application? sourceApp)
	{
		_usedWindowContent = true;
		TestServices.WindowHelper.WindowContent = sourceApp is null
			? new Border { Child = element }
			: new AlcContentHost { SourceApplicationOverride = sourceApp, Content = element };
		await TestServices.WindowHelper.WaitForLoaded(element);
		await TestServices.WindowHelper.WaitForIdle();
	}

	private async Task<Application> SetUpDuplicatedAssemblyAsync(bool seedViaMergedDictionary = false)
	{
		var alcAppPath = await BuildAlcAppAsync();
		Assert.IsNotNull(alcAppPath, "AlcApp build should succeed.");
		Assert.IsTrue(File.Exists(alcAppPath), $"AlcApp assembly should exist at {alcAppPath}.");

		var alcAppDirectory = Path.GetDirectoryName(alcAppPath)!;

		_ownerAlc = new TestAssemblyLoadContext(alcAppDirectory);
		var ownerAssembly = _ownerAlc.LoadFromAssemblyPath(alcAppPath!);
		Assert.IsNotNull(ownerAssembly, "AlcApp assembly should load into the owner ALC.");

		var appType = ownerAssembly.GetType("AlcTestApp.AppC");
		Assert.IsNotNull(appType, "AlcTestApp.AppC should be discoverable in the owner ALC.");

		var ownerApp = ConstructApplicationOffThread(appType!);

		Assert.AreSame(
			_ownerAlc,
			global::System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(ownerApp.GetType().Assembly),
			"Sanity: the registered app's type must live in the owner ALC.");
		Assert.AreSame(
			ownerApp,
			Application.GetForAssemblyLoadContext(_ownerAlc!),
			"Sanity: constructing an Application inside a secondary ALC must register it for that ALC.");
		Assert.IsTrue(
			Application.HasSecondaryApps,
			"Sanity: registering a secondary app must latch HasSecondaryApps, which gates the entire " +
			"ALC-aware branch of TryTopLevelRetrieval.");

		_probeAssemblyName = LoadPrivateCopyOfAHostAssembly(alcAppDirectory);

		if (seedViaMergedDictionary)
		{
			// Mirrors how a theme package lands: the key is not on Application.Resources itself, it is
			// in a dictionary merged into it.
			var themeLike = new ResourceDictionary();
			themeLike[SharedKey] = AppValue;
			ownerApp.Resources.MergedDictionaries.Add(themeLike);
		}
		else
		{
			ownerApp.Resources[SharedKey] = AppValue;
		}

		_hadPriorHostValue = Application.Current.Resources.TryGetValue(SharedKey, out _priorHostValue);
		Application.Current.Resources[SharedKey] = HostValue;
		_seededHostValue = true;

		return ownerApp;
	}

	/// <summary>
	/// Finds an assembly that the host already has loaded in the default ALC and that the owner ALC
	/// can load its own private copy of, then loads that copy and returns the shared simple name.
	/// </summary>
	/// <remarks>
	/// Three constraints, and each one is what makes the two tests deterministic rather than dependent
	/// on which ALC tests ran before this one:
	/// <list type="number">
	/// <item>The host's copy must be the FIRST match in <c>AppDomain.CurrentDomain.GetAssemblies()</c>,
	/// so the lazy scan's answer is fixed. Candidates are taken in enumeration order and a name is
	/// skipped once seen, so the chosen name's first copy is by construction the default-ALC one.</item>
	/// <item>The file must exist in the AlcApp output folder, since <c>TestAssemblyLoadContext</c>
	/// resolves private copies from there.</item>
	/// <item>The load must actually produce a private copy — <c>TestAssemblyLoadContext</c> shares
	/// whole namespaces (Uno.*, Microsoft.UI.*, …) back to the default ALC, and a shared resolution
	/// would leave one copy and nothing to mis-resolve. The outcome is verified rather than predicted
	/// from the name, so this stays correct if that sharing policy changes.</item>
	/// </list>
	/// Discovering the assembly instead of naming one keeps the test from breaking when AlcApp's
	/// package references change.
	/// </remarks>
	private string LoadPrivateCopyOfAHostAssembly(string alcAppDirectory)
	{
		var seen = new HashSet<string>(StringComparer.Ordinal);
		var rejected = new List<string>();

		foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			var name = assembly.IsDynamic ? null : assembly.GetName().Name;
			if (string.IsNullOrEmpty(name) || !seen.Add(name!))
			{
				// Already seen means an earlier copy exists, so this one is not what the scan would find.
				continue;
			}

			if (global::System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(assembly)
				!= global::System.Runtime.Loader.AssemblyLoadContext.Default)
			{
				continue;
			}

			if (!File.Exists(Path.Combine(alcAppDirectory, name + ".dll")))
			{
				continue;
			}

			Assembly privateCopy;
			try
			{
				privateCopy = _ownerAlc!.LoadFromAssemblyName(new AssemblyName(name!));
			}
			catch (Exception ex)
			{
				rejected.Add($"{name} (load failed: {ex.GetType().Name})");
				continue;
			}

			if (global::System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(privateCopy) != _ownerAlc)
			{
				// Shared back to the default ALC — one copy, nothing to mis-resolve.
				rejected.Add($"{name} (resolved shared, not private)");
				continue;
			}

			return name!;
		}

		Assert.Fail(
			"No assembly is loaded in the default ALC that the owner ALC can also load privately from " +
			$"'{alcAppDirectory}', so the duplicate-name precondition cannot be established. " +
			$"Rejected candidates: {(rejected.Count == 0 ? "<none>" : string.Join(", ", rejected))}.");
		throw new InvalidOperationException("Unreachable — Assert.Fail throws.");
	}

	/// <summary>
	/// Constructs the <see cref="Application"/> on a dedicated thread, mirroring
	/// <c>Given_AlcContentHost.RegisterSiblingAlcApplicationAsync</c>: the base constructor registers
	/// the instance from the ALC of its own type, and running off the UI thread keeps the construction
	/// clear of the host's dispatcher/window initialization.
	/// </summary>
	private static Application ConstructApplicationOffThread(Type appType)
	{
		Application? app = null;
		Exception? constructionError = null;

		var thread = new System.Threading.Thread(() =>
		{
			try
			{
				app = (Application?)Activator.CreateInstance(appType);
			}
			catch (Exception ex)
			{
				constructionError = ex;
			}
		})
		{
			IsBackground = true,
			Name = $"AlcApp-{appType.Name}",
		};

		thread.Start();
		thread.Join();

		if (constructionError is not null)
		{
			throw new InvalidOperationException(
				$"Failed to construct {appType.FullName} in the owner ALC: {constructionError.Message}",
				constructionError);
		}

		Assert.IsNotNull(app, $"{appType.FullName} instance should be constructed.");
		return app!;
	}

	/// <summary>
	/// Appends every loaded copy of the probe assembly and its ALC to a failure message. A failure in
	/// these tests is almost always about WHICH copy the scan picked, so the answer belongs in the
	/// message rather than in a debugger session.
	/// </summary>
	private string DescribeLoadedCopies()
	{
		var copies = AppDomain.CurrentDomain.GetAssemblies()
			.Where(a => !a.IsDynamic && string.Equals(a.GetName().Name, _probeAssemblyName, StringComparison.Ordinal))
			.Select(a => global::System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(a))
			.Select(alc => alc is null
				? "<none>"
				: alc == global::System.Runtime.Loader.AssemblyLoadContext.Default ? "Default" : $"{alc.Name}#{alc.GetHashCode():X8}");

		return $"\nProbe assembly: '{_probeAssemblyName}'. Loaded copies, in enumeration order: " +
			$"{string.Join(", ", copies)}." +
			$"\nOwner ALC: {_ownerAlc?.Name}#{_ownerAlc?.GetHashCode():X8}.";
	}

	private static string GetAlcAppFolder()
	{
		var basePath = Path.GetDirectoryName(Application.Current.GetType().Assembly.Location)!;

		var searchPaths = new[]
		{
			Path.Combine(basePath, "..", "..", "..", "..", "..", "Uno.UI.RuntimeTests", "Tests", "AssemblyLoadContext", "AlcApp"),
			Path.Combine(basePath, "..", "..", "src", "Uno.UI.RuntimeTests", "Tests", "AssemblyLoadContext", "AlcApp"),
			Path.Combine(basePath, "..", "..", ".."), // CI
		};

		var folder = searchPaths.FirstOrDefault(p => File.Exists(Path.Combine(p, "Uno.UI.RuntimeTests.AlcApp.csproj")));

		if (folder is null)
		{
			throw new InvalidOperationException("Unable to find AlcApp folder in " + string.Join(", ", searchPaths));
		}

		return folder;
	}

	/// <summary>
	/// Builds the AlcApp test project and returns the path to the compiled assembly. Mirrors
	/// <c>Given_AlcContentHost.BuildAlcAppAsync</c> so this class runs on its own, without depending
	/// on another test class having run first.
	/// </summary>
	private async Task<string?> BuildAlcAppAsync()
	{
		var alcAppProjectPath = Path.Combine(GetAlcAppFolder(), "Uno.UI.RuntimeTests.AlcApp.csproj");
		Assert.IsTrue(File.Exists(alcAppProjectPath), $"AlcApp project should exist at {alcAppProjectPath}");

		var targetFramework =
#if NET11_0
			"net11.0";
#elif NET10_0
			"net10.0";
#elif NET9_0
			"net9.0";
#else
#error This .NET version is not yet supported by the test project build script.
#endif

		const string Configuration = "Debug";

		var alcAppDir = Path.GetDirectoryName(alcAppProjectPath)!;
		var assemblyPath = Path.Combine(alcAppDir, "bin", Configuration, targetFramework, "Uno.UI.RuntimeTests.AlcApp.dll");

		var startInfo = new System.Diagnostics.ProcessStartInfo
		{
			FileName = "dotnet",
			Arguments = $"build \"{alcAppProjectPath}\" -c {Configuration} -f {targetFramework}",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
			WorkingDirectory = alcAppDir,
		};

		using var process = System.Diagnostics.Process.Start(startInfo);
		Assert.IsNotNull(process, "dotnet build process should start");

		var outputTask = process.StandardOutput.ReadToEndAsync();
		var errorTask = process.StandardError.ReadToEndAsync();

		await Task.WhenAll(outputTask, errorTask);
		await process.WaitForExitAsync();

		if (process.ExitCode != 0)
		{
			Assert.Fail($"AlcApp build failed with exit code {process.ExitCode}.\nOutput: {outputTask.Result}\nError: {errorTask.Result}");
		}

		Assert.IsTrue(File.Exists(assemblyPath),
			$"AlcApp assembly should exist after build at {assemblyPath}.\nBuild output: {outputTask.Result}");

		return assemblyPath;
	}
}

#endif
