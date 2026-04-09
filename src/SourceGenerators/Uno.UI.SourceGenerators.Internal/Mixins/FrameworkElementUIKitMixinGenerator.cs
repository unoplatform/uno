#nullable enable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Uno.UI.SourceGenerators.Mixins;

[Generator]
public sealed class FrameworkElementUIKitMixinGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		// PostInitializationOutput makes code visible to both the XAML generator
		// and DependencyPropertyGenerator. Platform filtering via #if __APPLE_UIKIT__.
		context.RegisterPostInitializationOutput(static ctx =>
		{
			ctx.AddSource("FrameworkElementMixins.UIKit.g.cs", GenerateFrameworkElementMixins());
		});

		// EffectiveViewport partials need AdditionalFiles, so they use RegisterSourceOutput.
		var platformProvider = context.AnalyzerConfigOptionsProvider.Select(static (options, ct) =>
		{
			options.GlobalOptions.TryGetValue("build_property.DefineConstantsProperty", out var constants);
			return constants?.Contains("__APPLE_UIKIT__") == true;
		});

		var evpFileProvider = context.AdditionalTextsProvider
			.Where(static file => file.Path.EndsWith("FrameworkElement.EffectiveViewport.cs", StringComparison.OrdinalIgnoreCase))
			.Select(static (file, ct) => file.GetText(ct)?.ToString())
			.Collect();

		var combined = platformProvider.Combine(evpFileProvider);

		context.RegisterSourceOutput(combined, static (ctx, data) =>
		{
			var (isAppleUIKit, evpFiles) = data;
			if (!isAppleUIKit)
			{
				return;
			}

			var evpContent = evpFiles.FirstOrDefault(f => f != null);
			if (evpContent != null)
			{
				foreach (var target in EffectiveViewportTargetsAlways)
				{
					var transformed = TransformEffectiveViewport(evpContent, target.FullTypeName, "true");
					ctx.AddSource($"{target.ClassName}.FrameworkElement.EffectiveViewport.g.cs", transformed);
				}
				foreach (var target in EffectiveViewportTargetsNoTvOS)
				{
					var transformed = TransformEffectiveViewport(evpContent, target.FullTypeName, "!__TVOS__");
					ctx.AddSource($"{target.ClassName}.FrameworkElement.EffectiveViewport.g.cs", transformed);
				}
			}
		});
	}

	#region EffectiveViewport

	private sealed class EvpTarget
	{
		public string FullTypeName { get; }
		public string ClassName => FullTypeName.Split('.').Last();

		public EvpTarget(string fullTypeName)
		{
			FullTypeName = fullTypeName;
		}
	}

	private static readonly EvpTarget[] EffectiveViewportTargetsAlways = new[]
	{
		new EvpTarget("Microsoft.UI.Xaml.Controls.NativeListViewBase"),
		new EvpTarget("Microsoft.UI.Xaml.Controls.NativeScrollContentPresenter"),
		new EvpTarget("Microsoft.UI.Xaml.Controls.MultilineTextBoxView"),
		new EvpTarget("Microsoft.UI.Xaml.Controls.SinglelineTextBoxView"),
	};

	private static readonly EvpTarget[] EffectiveViewportTargetsNoTvOS = new[]
	{
		new EvpTarget("Microsoft.UI.Xaml.Controls.Picker"),
	};

	private static string TransformEffectiveViewport(string content, string fullTypeName, string ifCondition)
	{
		var parts = fullTypeName.Split('.');
		var spaceName = string.Join(".", parts.Take(parts.Length - 1));
		var className = parts.Last();

		var result = "#define TEMPLATED\r\n" + content;
		result = "#define IS_NATIVE_ELEMENT\r\n" + result;
		result = "#if " + ifCondition + "\r\n" + result + "\r\n#endif\r\n";
		result = Regex.Replace(result, @"^using _This\s*=.*$", $"using _This = {fullTypeName};", RegexOptions.Multiline);
		result = Regex.Replace(result, @"^namespace [a-zA-Z0-9_\.\s]+$", $"namespace {spaceName}", RegexOptions.Multiline);
		result = Regex.Replace(result, @"partial class [a-zA-Z0-9_]+", $"partial class {className}", RegexOptions.Multiline);

		return result;
	}

	#endregion

	#region MixinParams

	private sealed class MixinParams
	{
		public string NamespaceName { get; set; } = "";
		public string ClassName { get; set; } = "";
		public bool DefineSetNeedsLayout { get; set; } = true;
		public bool DefineLayoutSubviews { get; set; } = true;
		public bool IsUIControl { get; set; }
		public bool HasAttachedToWindow { get; set; } = true;
		public bool OverridesAttachedToWindow { get; set; }
		public bool IsNewBackground { get; set; }
		public bool IsFrameworkElement => ClassName == "FrameworkElement";
		public bool HasAutomationPeer { get; set; }
		public bool HasLayouter { get; set; }
		public string LoadingInvokeArgument { get; set; } = "this";
		public bool TvOS { get; set; } = true;
	}

	private static readonly MixinParams[] Mixins = new[]
	{
		new MixinParams
		{
			NamespaceName = "Microsoft.UI.Xaml",
			ClassName = "FrameworkElement",
			DefineSetNeedsLayout = false,
			DefineLayoutSubviews = false,
			HasAttachedToWindow = false,
			OverridesAttachedToWindow = true,
			HasLayouter = true,
		},
		new MixinParams
		{
			NamespaceName = "Microsoft.UI.Xaml.Controls",
			ClassName = "NativeListViewBase",
			HasAttachedToWindow = false,
			OverridesAttachedToWindow = true,
			DefineSetNeedsLayout = false,
			DefineLayoutSubviews = false,
			LoadingInvokeArgument = "null",
		},
		new MixinParams
		{
			NamespaceName = "Microsoft.UI.Xaml.Controls",
			ClassName = "NativeScrollContentPresenter",
			HasAttachedToWindow = false,
			OverridesAttachedToWindow = true,
			DefineSetNeedsLayout = false,
			DefineLayoutSubviews = false,
			LoadingInvokeArgument = "null",
		},
		new MixinParams
		{
			NamespaceName = "Microsoft.UI.Xaml.Controls",
			ClassName = "MultilineTextBoxView",
			IsUIControl = false,
			HasAttachedToWindow = true,
			OverridesAttachedToWindow = false,
			IsNewBackground = false,
			LoadingInvokeArgument = "null",
		},
		new MixinParams
		{
			NamespaceName = "Microsoft.UI.Xaml.Controls",
			ClassName = "SinglelineTextBoxView",
			IsUIControl = true,
			HasAttachedToWindow = true,
			OverridesAttachedToWindow = false,
			IsNewBackground = true,
			LoadingInvokeArgument = "null",
		},
		new MixinParams
		{
			NamespaceName = "Uno.UI.Controls.Legacy",
			ClassName = "ListViewBase",
			HasAttachedToWindow = false,
			OverridesAttachedToWindow = true,
			DefineSetNeedsLayout = false,
			DefineLayoutSubviews = false,
			LoadingInvokeArgument = "null",
		},
		new MixinParams
		{
			NamespaceName = "Microsoft.UI.Xaml.Controls",
			ClassName = "Picker",
			HasAttachedToWindow = true,
			LoadingInvokeArgument = "null",
			TvOS = false,
		},
	};

	#endregion

	private static string GenerateFrameworkElementMixins()
	{
		var sb = new StringBuilder();
		sb.AppendLine("// <auto-generated>");
		sb.AppendLine("// This file was generated by FrameworkElementUIKitMixinGenerator. Do not edit manually.");
		sb.AppendLine("// </auto-generated>");
		sb.AppendLine();
		sb.AppendLine("#if __APPLE_UIKIT__");
		sb.AppendLine();
		sb.AppendLine("#pragma warning disable 414");
		sb.AppendLine();
		sb.AppendLine("using System;");
		sb.AppendLine("using Uno.Disposables;");
		sb.AppendLine("using System.ComponentModel;");
		sb.AppendLine("using System.Runtime.CompilerServices;");
		sb.AppendLine("using Uno.UI.DataBinding;");
		sb.AppendLine("using Uno.Extensions;");
		sb.AppendLine("using Uno.UI;");
		sb.AppendLine("using Uno.UI.Helpers;");
		sb.AppendLine("using Uno.UI.Media;");
		sb.AppendLine("using Microsoft.UI.Xaml;");
		sb.AppendLine("using Microsoft.UI.Xaml.Data;");
		sb.AppendLine("using UIKit;");
		sb.AppendLine("using CoreGraphics;");
		sb.AppendLine("using Microsoft.UI.Xaml.Media;");
		sb.AppendLine("using Microsoft.UI.Xaml.Media.Animation;");
		sb.AppendLine("using Uno.Foundation.Logging;");
		sb.AppendLine("using Windows.Foundation;");
		sb.AppendLine("using Microsoft.UI.Xaml.Automation.Peers;");
		sb.AppendLine("using Microsoft.UI.Xaml.Automation.Provider;");
		sb.AppendLine("using Microsoft.UI.Xaml.Automation;");
		sb.AppendLine("using Uno.UI.Xaml;");
		sb.AppendLine("using ObjCRuntime;");
		sb.AppendLine();

		foreach (var mixin in Mixins)
		{
			GenerateUIKitMixin(sb, mixin);
		}

		sb.AppendLine("#endif");
		return sb.ToString();
	}

	private static void GenerateUIKitMixin(StringBuilder sb, MixinParams m)
	{
		var isfe = m.IsFrameworkElement;
		var cn = m.ClassName;

		if (!m.TvOS)
		{
			sb.AppendLine("#if !__TVOS__");
		}
		sb.AppendLine();
		sb.AppendLine($"namespace {m.NamespaceName}");
		sb.AppendLine("{");
		sb.AppendLine($"\tpartial class {cn} : IFrameworkElement, IXUidProvider, IFrameworkElementInternal");
		sb.AppendLine("\t{");
		sb.AppendLine("\t\tstring IXUidProvider.Uid { get; set; }");
		sb.AppendLine();
		sb.AppendLine($"\t\tbool IFrameworkElementInternal.HasLayouter => {(m.HasLayouter ? "true" : "false")};");
		sb.AppendLine();

		if (!isfe)
		{
			sb.AppendLine("\t\t/// <summary>");
			sb.AppendLine("\t\t/// Gets the parent of this FrameworkElement in the object tree.");
			sb.AppendLine("\t\t/// </summary>");
			sb.AppendLine("\t\tpublic DependencyObject Parent => ((IDependencyObjectStoreProvider)this).Store.Parent as DependencyObject;");
			sb.AppendLine();
		}

		// HasAttachedToWindow
		if (m.HasAttachedToWindow)
		{
			sb.AppendLine("\t\tpartial void OnAttachedToWindowPartial()");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\tOnLoading();");
			sb.AppendLine("\t\t\tOnLoaded();");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
			sb.AppendLine("\t\tpartial void OnDetachedFromWindowPartial()");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\tOnUnloaded();");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
		}

		// OverridesAttachedToWindow
		if (m.OverridesAttachedToWindow)
		{
			sb.AppendLine("\t\tprivate UIWindow _currentWindow;");
			sb.AppendLine();
			sb.AppendLine("\t\tpublic override void MovedToWindow()");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\tbase.MovedToWindow();");
			sb.AppendLine();
			sb.AppendLine("\t\t\ttry");
			sb.AppendLine("\t\t\t{");
			sb.AppendLine("\t\t\t\tvar newWindow = Window;");
			sb.AppendLine("\t\t\t\tvar superView = Superview;");
			sb.AppendLine();
			sb.AppendLine("\t\t\t\tif(_currentWindow != newWindow)");
			sb.AppendLine("\t\t\t\t{");
			sb.AppendLine("\t\t\t\t\tif(newWindow != null)");
			sb.AppendLine("\t\t\t\t\t{");
			sb.AppendLine("\t\t\t\t\t\tif(_superViewRef?.GetTarget() == null && superView != null)");
			sb.AppendLine("\t\t\t\t\t\t{");
			sb.AppendLine("\t\t\t\t\t\t\t_superViewRef = new WeakReference<UIView>(superView);");
			sb.AppendLine("\t\t\t\t\t\t\tSyncBinder(superView, newWindow);");
			sb.AppendLine("\t\t\t\t\t\t\t((IDependencyObjectStoreProvider)this).Store.Parent = superView;");
			sb.AppendLine("\t\t\t\t\t\t}");
			sb.AppendLine();
			sb.AppendLine("\t\t\t\t\t\tOnLoading();");
			sb.AppendLine("\t\t\t\t\t\tOnLoaded();");
			sb.AppendLine("\t\t\t\t\t}");
			sb.AppendLine("\t\t\t\t\telse");
			sb.AppendLine("\t\t\t\t\t{");
			sb.AppendLine("\t\t\t\t\t\tOnUnloaded();");
			sb.AppendLine("\t\t\t\t\t}");
			sb.AppendLine();
			sb.AppendLine("\t\t\t\t\t_currentWindow = newWindow;");
			sb.AppendLine("\t\t\t\t}");
			sb.AppendLine("\t\t\t}");
			sb.AppendLine("\t\t\tcatch(Exception e)");
			sb.AppendLine("\t\t\t{");
			sb.AppendLine("\t\t\t\t// On iOS, this handler is critical in the context of newWindow == null. If an");
			sb.AppendLine("\t\t\t\t// exception is raised for a tree of UIView instances the complete chain of OnUnloaded");
			sb.AppendLine("\t\t\t\t// will be interrupted, creating a memory leak as the controls that would have been unloaded");
			sb.AppendLine("\t\t\t\t// will not unbind properly from their respective parents.");
			sb.AppendLine();
			sb.AppendLine("\t\t\t\tthis.Log().Error($\"Failed to process MoveToWindow for {GetType()}\", e);");
			sb.AppendLine("\t\t\t}");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
		}

		// WillMoveToSuperview
		sb.AppendLine("\t\t// WillMoveToSuperview may not be called if the element is moved into Window immediately.");
		sb.AppendLine("\t\tprivate WeakReference<UIView> _superViewRef;");
		sb.AppendLine();
		sb.AppendLine("\t\tpublic override void WillMoveToSuperview(UIView newsuper)");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tbase.WillMoveToSuperview(newsuper);");
		sb.AppendLine();
		sb.AppendLine("\t\t\ttry");
		sb.AppendLine("\t\t\t{");
		sb.AppendLine("\t\t\t\tif(BinderReferenceHolder.IsEnabled)");
		sb.AppendLine("\t\t\t\t{");
		sb.AppendLine("\t\t\t\t\tif(newsuper != null)");
		sb.AppendLine("\t\t\t\t\t{");
		sb.AppendLine("\t\t\t\t\t\tBinderReferenceHolder.AddNativeReference(this, newsuper);");
		sb.AppendLine("\t\t\t\t\t}");
		sb.AppendLine("\t\t\t\t\telse");
		sb.AppendLine("\t\t\t\t\t{");
		sb.AppendLine("\t\t\t\t\t\tBinderReferenceHolder.RemoveNativeReference(this, _superViewRef.GetTarget() as global::Foundation.NSObject);");
		sb.AppendLine("\t\t\t\t\t}");
		sb.AppendLine("\t\t\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\t\t\t_superViewRef = new WeakReference<UIView>(newsuper);");
		sb.AppendLine();
		sb.AppendLine("\t\t\t\tWillMoveToSuperviewPartial(newsuper);");
		sb.AppendLine("\t\t\t\tSyncBinder(newsuper, Window);");
		sb.AppendLine("\t\t\t\t((IDependencyObjectStoreProvider)this).Store.Parent = newsuper;");
		sb.AppendLine("\t\t\t}");
		sb.AppendLine("\t\t\tcatch(Exception e)");
		sb.AppendLine("\t\t\t{");
		sb.AppendLine("\t\t\t\tApplication.Current.RaiseRecoverableUnhandledExceptionOrLog(e, this);");
		sb.AppendLine("\t\t\t}");
		sb.AppendLine("\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\tpartial void WillMoveToSuperviewPartial(UIView newsuper);");
		sb.AppendLine();
		sb.AppendLine("\t\tprivate void SyncBinder(UIView superview, UIWindow window)");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tif(superview == null && window == null)");
		sb.AppendLine("\t\t\t{");
		sb.AppendLine("\t\t\t\tClearBindings();");
		sb.AppendLine("\t\t\t}");
		sb.AppendLine("\t\t}");
		sb.AppendLine();

		// Events
		sb.AppendLine("\t\tpublic event TypedEventHandler<FrameworkElement, object> Loading;");
		sb.AppendLine();
		sb.AppendLine("\t\tpublic event RoutedEventHandler Loaded;");
		sb.AppendLine();

		if (!isfe)
		{
			sb.AppendLine("\t\tpublic event DependencyPropertyChangedEventHandler IsEnabledChanged;");
			sb.AppendLine();
		}

		sb.AppendLine("\t\tpublic event RoutedEventHandler Unloaded;");
		sb.AppendLine();
		sb.AppendLine("\t\tpublic event SizeChangedEventHandler SizeChanged;");
		sb.AppendLine();

		// LayoutSubviews / SetNeedsLayout
		if (m.DefineLayoutSubviews || m.DefineSetNeedsLayout)
		{
			sb.AppendLine("\t\tprivate bool _layoutRequested = false;");
			sb.AppendLine();
		}

		if (m.DefineLayoutSubviews)
		{
			sb.AppendLine("\t\tpublic override void LayoutSubviews()");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\t_layoutRequested = false;");
			sb.AppendLine("\t\t\tbase.LayoutSubviews();");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
		}

		if (m.DefineSetNeedsLayout)
		{
			sb.AppendLine("\t\tpublic override void SetNeedsLayout()");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\t// Reminder: Skipping the call to SetNeedsLayout may cause some controls");
			sb.AppendLine("\t\t\t// to layout incorrectly (like SinglelineTextBoxView) when Window == null");
			sb.AppendLine("\t\t\t// and that they are re-attached to the Window.");
			sb.AppendLine();
			sb.AppendLine("\t\t\tbase.SetNeedsLayout();");
			sb.AppendLine();
			sb.AppendLine("\t\t\tSetNeedsLayoutPartial();");
			sb.AppendLine();
			sb.AppendLine("\t\t\tif (!_layoutRequested)");
			sb.AppendLine("\t\t\t{");
			sb.AppendLine("\t\t\t\t_layoutRequested = true;");
			sb.AppendLine("\t\t\t\tSetSuperviewNeedsLayout();");
			sb.AppendLine("\t\t\t}");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
			sb.AppendLine("\t\tpartial void SetNeedsLayoutPartial();");
			sb.AppendLine();
		}

		// SetSuperviewNeedsLayout
		sb.AppendLine("\t\tpublic virtual void SetSuperviewNeedsLayout()");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\t// Resolve the property only once, to avoid paying the cost of the interop.");
		sb.AppendLine("\t\t\tvar actualSuperview = Superview;");
		sb.AppendLine();
		sb.AppendLine("\t\t\tif (actualSuperview != null)");
		sb.AppendLine("\t\t\t{");
		sb.AppendLine("\t\t\t\tactualSuperview.SetNeedsLayout();");
		sb.AppendLine("\t\t\t}");
		sb.AppendLine("\t\t}");
		sb.AppendLine();

		// AdjustArrange
		sb.AppendLine("\t\tpartial void AdjustArrangePartial(ref Size size);");
		sb.AppendLine("\t\tpublic virtual Size AdjustArrange(Size size)");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tAdjustArrangePartial(ref size);");
		sb.AppendLine();
		sb.AppendLine("\t\t\treturn size;");
		sb.AppendLine("\t\t}");
		sb.AppendLine();

		// FindName
		sb.AppendLine("\t\tpublic object FindName (string name)");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\treturn IFrameworkElementHelper.FindName (this, this, name);");
		sb.AppendLine("\t\t}");
		sb.AppendLine();

		// Name DP
		sb.AppendLine("\t\t#region Name Dependency Property");
		sb.AppendLine();
		sb.AppendLine("\t\tprivate void OnNameChanged(string oldValue, string newValue) {");
		sb.AppendLine("\t\t\tif (FrameworkElementHelper.IsUiAutomationMappingEnabled)");
		sb.AppendLine("\t\t\t{");
		sb.AppendLine("\t\t\t\tMicrosoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(this, newValue);");
		sb.AppendLine("\t\t\t}");
		sb.AppendLine("\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\t[GeneratedDependencyProperty(DefaultValue = \"\", ChangedCallback = true)]");
		sb.AppendLine($"\t\tpublic static DependencyProperty NameProperty {{ get ; }} = CreateNameProperty();");
		sb.AppendLine();
		sb.AppendLine("\t\tpublic virtual string Name");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tget => GetNameValue();");
		sb.AppendLine("\t\t\tset => SetNameValue(value);");
		sb.AppendLine("\t\t}");
		sb.AppendLine("\t\t#endregion");
		sb.AppendLine();

		// Margin DP
		sb.AppendLine("\t\t#region Margin Dependency Property");
		sb.AppendLine("\t\t[GeneratedDependencyProperty(ChangedCallbackName = nameof(OnGenericPropertyUpdated))]");
		sb.AppendLine($"\t\tpublic static DependencyProperty MarginProperty {{ get ; }} = CreateMarginProperty();");
		sb.AppendLine();
		sb.AppendLine("\t\tpublic Thickness Margin");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tget => GetMarginValue();");
		sb.AppendLine("\t\t\tset => SetMarginValue(value);");
		sb.AppendLine("\t\t}");
		sb.AppendLine("\t\tprivate static Thickness GetMarginDefaultValue() => Thickness.Empty;");
		sb.AppendLine("\t\t#endregion");
		sb.AppendLine();

		// HorizontalAlignment DP
		sb.AppendLine("\t\t#region HorizontalAlignment Dependency Property");
		sb.AppendLine();
		sb.AppendLine("\t\t[GeneratedDependencyProperty(DefaultValue = HorizontalAlignment.Stretch, Options = FrameworkPropertyMetadataOptions.AffectsArrange, ChangedCallbackName = nameof(OnGenericPropertyUpdated))]");
		sb.AppendLine($"\t\tpublic static DependencyProperty HorizontalAlignmentProperty {{ get ; }} = CreateHorizontalAlignmentProperty();");
		sb.AppendLine();
		sb.Append("\t\tpublic");
		if (m.IsUIControl) sb.Append(" new");
		sb.AppendLine(" HorizontalAlignment HorizontalAlignment");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tget => GetHorizontalAlignmentValue();");
		sb.AppendLine("\t\t\tset => SetHorizontalAlignmentValue(value);");
		sb.AppendLine("\t\t}");
		sb.AppendLine("\t\t#endregion");
		sb.AppendLine();

		// VerticalAlignment DP
		sb.AppendLine("\t\t#region VerticalAlignment Dependency Property");
		sb.AppendLine();
		sb.AppendLine("\t\t[GeneratedDependencyProperty(DefaultValue = VerticalAlignment.Stretch, Options = FrameworkPropertyMetadataOptions.AffectsArrange, ChangedCallbackName = nameof(OnGenericPropertyUpdated))]");
		sb.AppendLine($"\t\tpublic static DependencyProperty VerticalAlignmentProperty {{ get ; }} = CreateVerticalAlignmentProperty();");
		sb.AppendLine();
		sb.Append("\t\tpublic");
		if (m.IsUIControl) sb.Append(" new");
		sb.AppendLine(" VerticalAlignment VerticalAlignment");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tget => GetVerticalAlignmentValue();");
		sb.AppendLine("\t\t\tset => SetVerticalAlignmentValue(value);");
		sb.AppendLine("\t\t}");
		sb.AppendLine("\t\t#endregion");
		sb.AppendLine();

		// Width/Height/Min/Max DPs
		GenerateSizeDependencyProperty(sb, "Width");
		GenerateSizeDependencyProperty(sb, "Height");
		GenerateSizeDependencyProperty(sb, "MinWidth", "0.0");
		GenerateSizeDependencyProperty(sb, "MinHeight", "0.0");
		GenerateSizeDependencyProperty(sb, "MaxWidth");
		GenerateSizeDependencyProperty(sb, "MaxHeight");

		// ActualWidth/Height
		sb.AppendLine("\t\tpublic double ActualWidth => GetActualWidth();");
		sb.AppendLine("\t\tpublic double ActualHeight => GetActualHeight();");
		sb.AppendLine();

		if (!isfe)
		{
			sb.AppendLine("\t\tprivate protected virtual double GetActualWidth() => _actualSize.Width;");
			sb.AppendLine("\t\tprivate protected virtual double GetActualHeight() => _actualSize.Height;");
			sb.AppendLine();
		}

		// Frame override
		sb.AppendLine("\t\tprivate Size _actualSize;");
		sb.AppendLine("\t\tpublic override CGRect Frame");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tget { return base.Frame; }");
		sb.AppendLine("\t\t\tset");
		sb.AppendLine("\t\t\t{");
		sb.AppendLine("\t\t\t\ttry");
		sb.AppendLine("\t\t\t\t{");
		sb.AppendLine("\t\t\t\t\tvar previousSize = _actualSize;");
		sb.AppendLine("\t\t\t\t\t_actualSize = value.Size.ToFoundationSize().PhysicalToLogicalPixels();");
		sb.AppendLine();
		if (isfe)
		{
			sb.AppendLine("\t\t\t\t\tRenderSize = _actualSize;");
			sb.AppendLine();
		}
		sb.AppendLine("\t\t\t\t\tCGAffineTransform? oldTransform = null;");
		sb.AppendLine("\t\t\t\t\tif (!Transform.IsIdentity)");
		sb.AppendLine("\t\t\t\t\t{");
		sb.AppendLine("\t\t\t\t\t\t// If UIView.Transform is not identity, then modifying the frame will give undefined behavior. (https://developer.apple.com/library/ios/documentation/UIKit/Reference/UIView_Class/#//apple_ref/occ/instp/UIView/transform)");
		sb.AppendLine("\t\t\t\t\t\t// We reapply the transform based on the new size straight after.");
		sb.AppendLine("\t\t\t\t\t\toldTransform = Transform;");
		sb.AppendLine("\t\t\t\t\t\tTransform = CGAffineTransform.MakeIdentity();");
		sb.AppendLine("\t\t\t\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\t\t\t\tbase.Frame = value;");
		sb.AppendLine("\t\t\t\t\tAppliedFrame = value;");
		sb.AppendLine();
		sb.AppendLine("\t\t\t\t\tif (previousSize != _actualSize)");
		sb.AppendLine("\t\t\t\t\t{");
		sb.AppendLine("\t\t\t\t\t\tSizeChanged?.Invoke(this, new SizeChangedEventArgs(this, previousSize, _actualSize));");
		sb.AppendLine();
		sb.AppendLine("\t\t\t\t\t\tif (_renderTransform != null)");
		sb.AppendLine("\t\t\t\t\t\t{");
		sb.AppendLine("\t\t\t\t\t\t\t// This will set the updated Transform");
		sb.AppendLine("\t\t\t\t\t\t\t_renderTransform.UpdateSize(_actualSize);");
		sb.AppendLine("\t\t\t\t\t\t}");
		sb.AppendLine("\t\t\t\t\t\telse if (oldTransform.HasValue)");
		sb.AppendLine("\t\t\t\t\t\t{");
		sb.AppendLine("\t\t\t\t\t\t\t// We grudgingly support setting the native transform directly without going through RenderTransform.");
		sb.AppendLine("\t\t\t\t\t\t\tTransform = oldTransform.Value;");
		sb.AppendLine("\t\t\t\t\t\t}");
		sb.AppendLine();
		if (isfe)
		{
			sb.AppendLine("\t\t\t\t\t\tif (Superview != null && !(Superview is DependencyObject))");
			sb.AppendLine("\t\t\t\t\t\t{");
			sb.AppendLine("\t\t\t\t\t\t\t// If this FrameworkElement has a native parent, then it probably wasn't measured prior to having its Frame changed.");
			sb.AppendLine("\t\t\t\t\t\t\t// Set RequiresMeasure flag so that this branch of the visual tree will be measured before being arranged, this is");
			sb.AppendLine("\t\t\t\t\t\t\t// required for some views (eg Image) to display correctly.");
			sb.AppendLine("\t\t\t\t\t\t\tSetLayoutFlags(LayoutFlag.MeasureDirty);");
			sb.AppendLine("\t\t\t\t\t\t}");
		}
		sb.AppendLine("\t\t\t\t\t}");
		sb.AppendLine("\t\t\t\t\telse if (oldTransform.HasValue)");
		sb.AppendLine("\t\t\t\t\t{");
		sb.AppendLine("\t\t\t\t\t\t// We grudgingly support setting the native transform directly without going through RenderTransform.");
		sb.AppendLine("\t\t\t\t\t\tTransform = oldTransform.Value;");
		sb.AppendLine("\t\t\t\t\t}");
		sb.AppendLine("\t\t\t\t}");
		sb.AppendLine("\t\t\t\tcatch (Exception ex)");
		sb.AppendLine("\t\t\t\t{");
		sb.AppendLine("\t\t\t\t\t// The app must not crash if any managed exception happens in the");
		sb.AppendLine("\t\t\t\t\t// native override");
		sb.AppendLine("\t\t\t\t\tApplication.Current.RaiseRecoverableUnhandledException(ex);");
		sb.AppendLine("\t\t\t\t}");
		sb.AppendLine("\t\t\t}");
		sb.AppendLine("\t\t}");
		sb.AppendLine();

		// AppliedFrame
		sb.AppendLine("\t\t/// <summary>");
		sb.AppendLine("\t\t/// The frame applied to this child when last arranged by its parent. This may differ from the current UIView.Frame if a RenderTransform is set.");
		sb.AppendLine("\t\t/// </summary>");
		sb.AppendLine("\t\tpublic Rect AppliedFrame { get; private set; }");
		sb.AppendLine();

		if (!isfe)
		{
			// LayoutUpdated, IsRenderingSuspended
			sb.AppendLine("\t\tpublic event EventHandler<object> LayoutUpdated;");
			sb.AppendLine();
			sb.AppendLine("\t\tinternal virtual void OnLayoutUpdated ()");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\tif (LayoutUpdated != null) {");
			sb.AppendLine("\t\t\t\tLayoutUpdated (this, EventArgs.Empty);");
			sb.AppendLine("\t\t\t}");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
			sb.AppendLine("\t\t// This is also defined in UIElement for actual UIElement hierarchy");
			sb.AppendLine("\t\tinternal bool IsRenderingSuspended { get; set; }");
			sb.AppendLine();

			// Style DP
			sb.AppendLine("\t\t#region Style DependencyProperty");
			sb.AppendLine();
			sb.AppendLine("\t\t[GeneratedDependencyProperty(DefaultValue = null, ChangedCallback = true, Options = FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext)]");
			sb.AppendLine("\t\tpublic static DependencyProperty StyleProperty { get; } = CreateStyleProperty();");
			sb.AppendLine();
			sb.AppendLine("\t\tpublic Style Style");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\tget => GetStyleValue();");
			sb.AppendLine("\t\t\tset => SetStyleValue(value);");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
			sb.AppendLine("\t\tprotected virtual void OnStyleChanged(Style oldValue, Style newValue)");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\tOnStyleChanged(oldValue, newValue, DependencyPropertyValuePrecedences.ExplicitStyle);");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
			sb.AppendLine("\t\tprivate void OnStyleChanged(Style oldStyle, Style newStyle, DependencyPropertyValuePrecedences precedence)");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\tif (oldStyle == newStyle)");
			sb.AppendLine("\t\t\t{");
			sb.AppendLine("\t\t\t\t// Nothing to do");
			sb.AppendLine("\t\t\t\treturn;");
			sb.AppendLine("\t\t\t}");
			sb.AppendLine();
			sb.AppendLine("\t\t\toldStyle?.ClearInvalidProperties(this, newStyle, precedence);");
			sb.AppendLine();
			sb.AppendLine("\t\t\tnewStyle?.ApplyTo(this, precedence);");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
			sb.AppendLine("\t\t#endregion");
			sb.AppendLine();

			// IsParsing / CreationComplete
			sb.AppendLine("\t\t[EditorBrowsable(EditorBrowsableState.Never)]");
			sb.AppendLine("\t\tpublic bool IsParsing { get; set; }");
			sb.AppendLine();
			sb.AppendLine("\t\t[EditorBrowsable(EditorBrowsableState.Never)]");
			sb.AppendLine("\t\tpublic void CreationComplete()");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\tIsParsing = false;");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
		}

		// SuspendRendering / ResumeRendering
		sb.AppendLine("\t\tinternal void SuspendRendering()");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tif(!IsRenderingSuspended)");
		sb.AppendLine("\t\t\t{");
		sb.AppendLine("\t\t\t\tIsRenderingSuspended = true;");
		sb.AppendLine();
		sb.AppendLine("\t\t\t\tAlpha = 0;");
		sb.AppendLine("\t\t\t\tSuspendBindings();");
		sb.AppendLine("\t\t\t}");
		sb.AppendLine("\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\tinternal void ResumeRendering()");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tif(IsRenderingSuspended)");
		sb.AppendLine("\t\t\t{");
		sb.AppendLine("\t\t\t\tIsRenderingSuspended = false;");
		sb.AppendLine();
		sb.AppendLine("\t\t\t\tAlpha = (float)Opacity;");
		sb.AppendLine("\t\t\t\tResumeBindings();");
		sb.AppendLine("\t\t\t}");
		sb.AppendLine("\t\t}");
		sb.AppendLine();

		// RenderPhase / ApplyBindingPhase
		sb.AppendLine("\t\t/// <summary>");
		sb.AppendLine("\t\t/// An optional render phase, see x:Bind.");
		sb.AppendLine("\t\t/// </summary>");
		sb.AppendLine("\t\tpublic int? RenderPhase { get; set; }");
		sb.AppendLine();
		sb.AppendLine("\t\tpublic void ApplyBindingPhase(int phase)");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tvoid ApplyChildren()");
		sb.AppendLine("\t\t\t{");
		sb.AppendLine("\t\t\t\tif(this is Uno.UI.Controls.IShadowChildrenProvider provider)");
		sb.AppendLine("\t\t\t\t{");
		sb.AppendLine("\t\t\t\t\tforeach (var child in provider.ChildrenShadow)");
		sb.AppendLine("\t\t\t\t\t{");
		sb.AppendLine("\t\t\t\t\t\t(child as IFrameworkElement)?.ApplyBindingPhase(phase);");
		sb.AppendLine("\t\t\t\t\t}");
		sb.AppendLine("\t\t\t\t}");
		sb.AppendLine("\t\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\t\tif (RenderPhase.HasValue)");
		sb.AppendLine("\t\t\t{");
		sb.AppendLine("\t\t\t\tif (RenderPhase <= phase)");
		sb.AppendLine("\t\t\t\t{");
		sb.AppendLine("\t\t\t\t\tApplyChildren();");
		sb.AppendLine("\t\t\t\t\tResumeRendering();");
		sb.AppendLine("\t\t\t\t}");
		sb.AppendLine("\t\t\t\telse");
		sb.AppendLine("\t\t\t\t{");
		sb.AppendLine("\t\t\t\t\tSuspendRendering();");
		sb.AppendLine("\t\t\t\t}");
		sb.AppendLine("\t\t\t}");
		sb.AppendLine("\t\t\telse");
		sb.AppendLine("\t\t\t{");
		sb.AppendLine("\t\t\t\tApplyChildren();");
		sb.AppendLine("\t\t\t}");
		sb.AppendLine("\t\t}");
		sb.AppendLine();

		// MovedToSuperview
		sb.AppendLine("\t\tpublic override void MovedToSuperview()");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tbase.MovedToSuperview();");
		sb.AppendLine("\t\t\tOnMovedToSuperview();");
		sb.AppendLine();
		sb.AppendLine("\t\t\tSetNeedsLayout();");
		sb.AppendLine("\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\tpartial void OnMovedToSuperview();");
		sb.AppendLine();

		// IsLoaded
		sb.AppendLine("\t\t/// <summary>");
		sb.AppendLine("\t\t/// Indicates if the view is currently loaded.");
		sb.AppendLine("\t\t/// </summary>");
		sb.AppendLine("\t\tpublic bool IsLoaded { get; private set; }");
		sb.AppendLine();

		// OnPostLoading / OnLoading / OnLoaded / OnUnloaded
		sb.AppendLine("\t\tprivate protected virtual void OnPostLoading() {}");
		sb.AppendLine();
		sb.AppendLine("\t\tinternal virtual void OnLoading()");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tOnLoadingPartial();");
		sb.AppendLine($"\t\t\tLoading?.Invoke({m.LoadingInvokeArgument}, null);");
		sb.AppendLine("\t\t\tOnPostLoading();");
		sb.AppendLine("\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\tpartial void OnLoadingPartial();");
		sb.AppendLine();
		sb.AppendLine("\t\tprivate protected virtual void OnLoaded()");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tIsLoaded = true;");
		sb.AppendLine();
		sb.AppendLine("\t\t\tSetNeedsLayout();");
		sb.AppendLine("\t\t\tOnLoadedPartial();");
		sb.AppendLine();
		sb.AppendLine("\t\t\tLoaded?.Invoke(this, new RoutedEventArgs(this));");
		sb.AppendLine("\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\tpartial void OnLoadedPartial();");
		sb.AppendLine();
		sb.AppendLine("\t\tprivate protected virtual void OnUnloaded()");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tIsLoaded = false;");
		sb.AppendLine();
		sb.AppendLine("\t\t\tOnUnloadedPartial();");
		sb.AppendLine();
		sb.AppendLine("\t\t\tif (Unloaded != null)");
		sb.AppendLine("\t\t\t{");
		sb.AppendLine("\t\t\t\tUnloaded(this, new RoutedEventArgs(this));");
		sb.AppendLine("\t\t\t}");
		sb.AppendLine("\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\tpartial void OnUnloadedPartial();");
		sb.AppendLine();

		if (!isfe)
		{
			// IsEnabled DP
			sb.AppendLine("\t\t#region IsEnabled Dependency Property");
			sb.AppendLine();
			sb.AppendLine("\t\t[GeneratedDependencyProperty(");
			sb.AppendLine("\t\t\tDefaultValue = true,");
			sb.AppendLine("\t\t\tOptions = FrameworkPropertyMetadataOptions.Inherits,");
			sb.AppendLine("\t\t\tChangedCallback = true");
			sb.AppendLine("\t\t)]");
			sb.AppendLine($"\t\tpublic static DependencyProperty IsEnabledProperty {{ get ; }} = CreateIsEnabledProperty();");
			sb.AppendLine();
			sb.AppendLine("\t\tpublic bool IsEnabled");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\tget => GetIsEnabledValue();");
			sb.AppendLine("\t\t\tset => SetIsEnabledValue(value);");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
			sb.AppendLine("\t\tprivate void OnIsEnabledChanged(DependencyPropertyChangedEventArgs e)");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\tvar newValue = (bool)e.NewValue;");
			sb.AppendLine("\t\t\tUserInteractionEnabled = newValue; // Set iOS native equivalent");
			sb.AppendLine();
			sb.AppendLine("\t\t\tIsEnabledChanged?.Invoke(this, e);");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
			sb.AppendLine("\t\t#endregion");
			sb.AppendLine();
		}

		if (!isfe)
		{
			// Opacity DP
			sb.AppendLine("\t\t#region Opacity Dependency Property");
			sb.AppendLine();
			sb.AppendLine("\t\tpublic double Opacity");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\tget { return (double)this.GetValue(OpacityProperty); }");
			sb.AppendLine("\t\t\tset { this.SetValue(OpacityProperty, value); }");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
			sb.AppendLine($"\t\tpublic static DependencyProperty OpacityProperty {{ get ; }} =");
			sb.AppendLine($"\t\t\tDependencyProperty.Register(\"Opacity\", typeof(double), typeof({cn}), new FrameworkPropertyMetadata(1.0, (s, a) => (({cn})s).OnOpacityChanged(a)));");
			sb.AppendLine();
			sb.AppendLine("\t\tprivate void OnOpacityChanged(DependencyPropertyChangedEventArgs args)");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\t// Don't update the internal value if the value is being animated.");
			sb.AppendLine("\t\t\t// The value is being animated by the platform itself.");
			sb.AppendLine("\t\t\tif (!(args.NewPrecedence == DependencyPropertyValuePrecedences.Animations && args.BypassesPropagation))");
			sb.AppendLine("\t\t\t{");
			sb.AppendLine("\t\t\t\tAlpha = IsRenderingSuspended ? 0 : (nfloat)Opacity;");
			sb.AppendLine("\t\t\t}");
			sb.AppendLine("\t\t}");
			sb.AppendLine("\t\t#endregion");
			sb.AppendLine();

			// Visibility DP
			sb.AppendLine("\t\t#region Visibility Dependency Property");
			sb.AppendLine();
			sb.AppendLine("\t\t/// <summary>");
			sb.AppendLine("\t\t/// Sets the visibility of the current view");
			sb.AppendLine("\t\t/// </summary>");
			sb.AppendLine("\t\tpublic Visibility Visibility");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\tget { return (Visibility)this.GetValue(VisibilityProperty); }");
			sb.AppendLine("\t\t\tset { this.SetValue(VisibilityProperty, value); }");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
			sb.AppendLine($"\t\tpublic static DependencyProperty VisibilityProperty {{ get ; }} =");
			sb.AppendLine("\t\t\tDependencyProperty.Register(");
			sb.AppendLine("\t\t\t\t\"Visibility\",");
			sb.AppendLine("\t\t\t\ttypeof(Visibility),");
			sb.AppendLine($"\t\t\t\ttypeof({cn}),");
			sb.AppendLine("\t\t\t\tnew FrameworkPropertyMetadata(");
			sb.AppendLine("\t\t\t\t\tVisibility.Visible,");
			sb.AppendLine($"\t\t\t\t\t(s, e) => (s as {cn}).OnVisibilityChanged((Visibility)e.OldValue, (Visibility)e.NewValue)");
			sb.AppendLine("\t\t\t\t)");
			sb.AppendLine("\t\t\t);");
			sb.AppendLine();
			sb.AppendLine("\t\tprotected virtual void OnVisibilityChanged(Visibility oldValue, Visibility newValue)");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\tvar newVisibility = (Visibility)newValue;");
			sb.AppendLine();
			sb.AppendLine("\t\t\tif (base.Hidden != newVisibility.IsHidden())");
			sb.AppendLine("\t\t\t{");
			sb.AppendLine("\t\t\t\tbase.Hidden = newVisibility.IsHidden();");
			sb.AppendLine("\t\t\t\tthis.SetNeedsLayout();");
			sb.AppendLine();
			sb.AppendLine("\t\t\t\tif (newVisibility == Visibility.Visible)");
			sb.AppendLine("\t\t\t\t{");
			sb.AppendLine("\t\t\t\t\t// This recursively invalidates the layout of all subviews");
			sb.AppendLine("\t\t\t\t\t// to ensure LayoutSubviews is called and views get updated.");
			sb.AppendLine("\t\t\t\t\t// Failing to do this can cause some views to remain collapsed.");
			sb.AppendLine("\t\t\t\t\tSetSubviewsNeedLayout();");
			sb.AppendLine("\t\t\t\t}");
			sb.AppendLine("\t\t\t}");
			sb.AppendLine();
			sb.AppendLine("\t\t\tOnVisibilityChangedPartial(oldValue, newValue);");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
			sb.AppendLine("\t\tpartial void OnVisibilityChangedPartial(Visibility oldValue, Visibility newValue);");
			sb.AppendLine();
			sb.AppendLine("\t\tpublic override bool Hidden");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\tget");
			sb.AppendLine("\t\t\t{");
			sb.AppendLine("\t\t\t\treturn base.Hidden;");
			sb.AppendLine("\t\t\t}");
			sb.AppendLine("\t\t\tset");
			sb.AppendLine("\t\t\t{");
			sb.AppendLine("\t\t\t\t// Only set the Visility property, the Hidden property is updated");
			sb.AppendLine("\t\t\t\t// in the property changed handler as there are actions associated with");
			sb.AppendLine("\t\t\t\t// the change.");
			sb.AppendLine("\t\t\t\tVisibility = value ? Visibility.Collapsed : Visibility.Visible;");
			sb.AppendLine("\t\t\t}");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
			sb.AppendLine("\t\tpublic void SetSubviewsNeedLayout()");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\tbase.SetNeedsLayout();");
			sb.AppendLine("\t\t\tforeach (var view in this.GetChildren())");
			sb.AppendLine("\t\t\t{");
			sb.AppendLine("\t\t\t\t(view as IFrameworkElement)?.SetSubviewsNeedLayout();");
			sb.AppendLine("\t\t\t}");
			sb.AppendLine("\t\t}");
			sb.AppendLine("\t\t#endregion");
			sb.AppendLine();
		}

		if (!isfe)
		{
			// IsHitTestVisible DP
			sb.AppendLine("\t\t#region IsHitTestVisible Dependency Property");
			sb.AppendLine();
			sb.AppendLine("\t\tpublic bool IsHitTestVisible");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\tget { return (bool)this.GetValue(IsHitTestVisibleProperty); }");
			sb.AppendLine("\t\t\tset { this.SetValue(IsHitTestVisibleProperty, value); }");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
			sb.AppendLine($"\t\tpublic static DependencyProperty IsHitTestVisibleProperty {{ get ; }} =");
			sb.AppendLine($"\t\t\tDependencyProperty.Register(\"IsHitTestVisible\", typeof(bool), typeof({cn}), new FrameworkPropertyMetadata(true, (s, e) => (s as {cn}).OnIsHitTestVisibleChanged(e)));");
			sb.AppendLine();
			sb.AppendLine("\t\tprivate void OnIsHitTestVisibleChanged(DependencyPropertyChangedEventArgs e)");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
			sb.AppendLine("\t\t#endregion");
			sb.AppendLine();

			// Tag DP
			sb.AppendLine("\t\t#region Tag Dependency Property");
			sb.AppendLine();
			sb.AppendLine("\t\tpublic new object Tag");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\tget { return this.GetValue(TagProperty); }");
			sb.AppendLine("\t\t\tset { this.SetValue(TagProperty, value); }");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
			sb.AppendLine($"\t\tpublic static DependencyProperty TagProperty {{ get ; }} =");
			sb.AppendLine($"\t\t\tDependencyProperty.Register(\"Tag\", typeof(object), typeof({cn}), new FrameworkPropertyMetadata(null, (s, e) => (s as {cn}).OnTagChanged(e)));");
			sb.AppendLine();
			sb.AppendLine("\t\tprivate void OnTagChanged(DependencyPropertyChangedEventArgs e)");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
			sb.AppendLine("\t\t#endregion");
			sb.AppendLine();
		}

		// Transitions DP
		sb.AppendLine("\t\t#region Transitions Dependency Property");
		sb.AppendLine();
		sb.AppendLine("\t\tpublic TransitionCollection Transitions");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tget { return (TransitionCollection)this.GetValue(TransitionsProperty); }");
		sb.AppendLine("\t\t\tset { this.SetValue(TransitionsProperty, value); }");
		sb.AppendLine("\t\t}");
		sb.AppendLine();
		sb.AppendLine($"\t\tpublic static DependencyProperty TransitionsProperty {{ get ; }} =");
		sb.AppendLine($"\t\t\tDependencyProperty.Register(\"Transitions\", typeof(TransitionCollection), typeof({cn}), new FrameworkPropertyMetadata(null, OnTransitionsChanged));");
		sb.AppendLine();
		sb.AppendLine("\t\tprivate static void OnTransitionsChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tvar frameworkElement = dependencyObject as IFrameworkElement;");
		sb.AppendLine();
		sb.AppendLine("\t\t\tif (frameworkElement != null)");
		sb.AppendLine("\t\t\t{");
		sb.AppendLine("\t\t\t\tvar oldValue = (TransitionCollection)args.OldValue;");
		sb.AppendLine();
		sb.AppendLine("\t\t\t\tif (oldValue != null)");
		sb.AppendLine("\t\t\t\t{");
		sb.AppendLine("\t\t\t\t\tforeach (var item in oldValue)");
		sb.AppendLine("\t\t\t\t\t{");
		sb.AppendLine("\t\t\t\t\t\titem.DetachFromElement(frameworkElement);");
		sb.AppendLine("\t\t\t\t\t}");
		sb.AppendLine("\t\t\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\t\t\tvar newValue = (TransitionCollection)args.NewValue;");
		sb.AppendLine();
		sb.AppendLine("\t\t\t\tif (newValue != null)");
		sb.AppendLine("\t\t\t\t{");
		sb.AppendLine("\t\t\t\t\tforeach (var item in newValue)");
		sb.AppendLine("\t\t\t\t\t{");
		sb.AppendLine("\t\t\t\t\t\titem.AttachToElement(frameworkElement);");
		sb.AppendLine("\t\t\t\t\t}");
		sb.AppendLine("\t\t\t\t}");
		sb.AppendLine("\t\t\t}");
		sb.AppendLine("\t\t}");
		sb.AppendLine("\t\t#endregion");
		sb.AppendLine();

		if (!isfe)
		{
			// RenderTransform DP
			sb.AppendLine("\t\t#region RenderTransform Dependency Property");
			sb.AppendLine();
			sb.AppendLine("\t\t/// <summary>");
			sb.AppendLine("\t\t/// This is a Transformation for a UIElement.  It binds the Render Transform to the View");
			sb.AppendLine("\t\t/// </summary>");
			sb.AppendLine("\t\tpublic Transform RenderTransform");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\tget => (Transform)this.GetValue(RenderTransformProperty);");
			sb.AppendLine("\t\t\tset => this.SetValue(RenderTransformProperty, value);");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
			sb.AppendLine("\t\t/// <summary>");
			sb.AppendLine("\t\t/// Backing dependency property for <see cref=\"RenderTransform\"/>");
			sb.AppendLine("\t\t/// </summary>");
			sb.AppendLine($"\t\tpublic static DependencyProperty RenderTransformProperty {{ get ; }} =");
			sb.AppendLine($"\t\t\tDependencyProperty.Register(\"RenderTransform\", typeof(Transform), typeof({cn}), new FrameworkPropertyMetadata(null, OnRenderTransformChanged));");
			sb.AppendLine();
			sb.AppendLine("\t\tprivate static void OnRenderTransformChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)");
			sb.AppendLine("\t\t{");
			sb.AppendLine($"\t\t\tvar view = ({cn})dependencyObject;");
			sb.AppendLine();
			sb.AppendLine("\t\t\tview._renderTransform?.Dispose();");
			sb.AppendLine();
			sb.AppendLine("\t\t\tif (args.NewValue is Transform transform)");
			sb.AppendLine("\t\t\t{");
			sb.AppendLine("\t\t\t\tview._renderTransform = new NativeRenderTransformAdapter(view, transform, view.RenderTransformOrigin);");
			sb.AppendLine("\t\t\t}");
			sb.AppendLine("\t\t\telse");
			sb.AppendLine("\t\t\t{");
			sb.AppendLine("\t\t\t\t// Sanity");
			sb.AppendLine("\t\t\t\tview._renderTransform = null;");
			sb.AppendLine("\t\t\t}");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
			sb.AppendLine("\t\tprivate NativeRenderTransformAdapter _renderTransform;");
			sb.AppendLine("\t\t#endregion");
			sb.AppendLine();

			// RenderTransformOrigin DP
			sb.AppendLine("\t\t#region RenderTransformOrigin Dependency Property");
			sb.AppendLine();
			sb.AppendLine("\t\t/// <summary>");
			sb.AppendLine("\t\t/// This is a Transformation for a UIElement.  It binds the Render Transform to the View");
			sb.AppendLine("\t\t/// </summary>");
			sb.AppendLine("\t\tpublic Point RenderTransformOrigin");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\tget => (Point)this.GetValue(RenderTransformOriginProperty);");
			sb.AppendLine("\t\t\tset => this.SetValue(RenderTransformOriginProperty, value);");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
			sb.AppendLine("\t\t/// <summary>");
			sb.AppendLine("\t\t/// Backing dependency property for <see cref=\"RenderTransformOrigin\"/>");
			sb.AppendLine("\t\t/// </summary>");
			sb.AppendLine($"\t\tpublic static DependencyProperty RenderTransformOriginProperty {{ get ; }} =");
			sb.AppendLine($"\t\t\tDependencyProperty.Register(\"RenderTransformOrigin\", typeof(Point), typeof({cn}), new FrameworkPropertyMetadata(default(Point), OnRenderTransformOriginChanged));");
			sb.AppendLine();
			sb.AppendLine("\t\tprivate static void OnRenderTransformOriginChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)");
			sb.AppendLine("\t\t{");
			sb.AppendLine($"\t\t\tvar view = (({cn})dependencyObject);");
			sb.AppendLine("\t\t\tvar point = (Point)args.NewValue;");
			sb.AppendLine();
			sb.AppendLine("\t\t\tview._renderTransform?.UpdateOrigin(point);");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
			sb.AppendLine("\t\t#endregion");
			sb.AppendLine();
		}

		// Background DP
		sb.AppendLine("\t\t#region Background Dependency Property");
		sb.AppendLine();
		sb.AppendLine("\t\t[GeneratedDependencyProperty(DefaultValue = null, Options=FrameworkPropertyMetadataOptions.ValueInheritsDataContext, ChangedCallback = true)]");
		sb.AppendLine($"\t\tpublic static DependencyProperty BackgroundProperty {{ get ; }} = CreateBackgroundProperty();");
		sb.AppendLine();
		sb.AppendLine($"\t\tpublic {(m.IsNewBackground ? "new " : "")}Brush Background");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tget => GetBackgroundValue();");
		sb.AppendLine("\t\t\tset => SetBackgroundValue(value);");
		sb.AppendLine("\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\tAction _brushChanged;");
		sb.AppendLine("\t\tIDisposable _backgroundBrushChangedSubscription;");
		sb.AppendLine();
		sb.AppendLine("\t\tprotected virtual void OnBackgroundChanged(DependencyPropertyChangedEventArgs e)");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tvar newValue = e.NewValue as Brush;");
		sb.AppendLine();
		sb.AppendLine("\t\t\t_backgroundBrushChangedSubscription?.Dispose();");
		sb.AppendLine("\t\t\t_backgroundBrushChangedSubscription = Brush.SetupBrushChanged(newValue, ref _brushChanged, () => Layer.BackgroundColor = Brush.GetFallbackColor(newValue));");
		sb.AppendLine("\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\t#endregion");
		sb.AppendLine();

		// HitTest
		sb.AppendLine("\t\tpublic override UIView HitTest(CGPoint point, UIEvent uievent)");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\t//If IsHitTestVisible is false, ignore children");
		sb.AppendLine("\t\t\tif (!IsHitTestVisible ||");
		if (isfe)
		{
			sb.AppendLine("\t\t\t\tthis is Microsoft.UI.Xaml.Controls.Control { IsEnabled: false }");
		}
		else
		{
			sb.AppendLine("\t\t\t\t!IsEnabled");
		}
		sb.AppendLine("\t\t\t)");
		sb.AppendLine("\t\t\t{");
		sb.AppendLine("\t\t\t\treturn null;");
		sb.AppendLine("\t\t\t}");
		if (isfe)
		{
			sb.AppendLine("\t\t\tif (Clip?.Rect is Rect rect && !rect.Contains(point))");
			sb.AppendLine("\t\t\t{");
			sb.AppendLine("\t\t\t\t// Clipped region must not block touch to the rest of the visual tree");
			sb.AppendLine("\t\t\t\treturn null;");
			sb.AppendLine("\t\t\t}");
		}
		sb.AppendLine();
		sb.AppendLine("\t\t\tvar viewHit = base.HitTest(point, uievent);");
		sb.AppendLine();
		sb.AppendLine($"\t\t\tvar hitCheck = (viewHit as {cn})?.IsViewHit() ?? true;");
		sb.AppendLine("\t\t\treturn hitCheck ? viewHit : null;");
		sb.AppendLine("\t\t}");
		sb.AppendLine();

		if (!isfe)
		{
			sb.AppendLine("\t\tpartial void HitCheckOverridePartial(ref bool hitCheck);");
			sb.AppendLine();
			sb.AppendLine("\t\tprotected virtual bool IsViewHit()");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\tvar hitCheck =");
			sb.AppendLine("\t\t\t\ttrue;");
			sb.AppendLine("\t\t\tHitCheckOverridePartial(ref hitCheck);");
			sb.AppendLine();
			sb.AppendLine("\t\t\treturn  hitCheck;");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
		}

		// OnGenericPropertyUpdated
		sb.AppendLine("\t\tprivate void OnGenericPropertyUpdated(DependencyPropertyChangedEventArgs args)");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tSetNeedsLayout();");
		sb.AppendLine("\t\t}");
		sb.AppendLine();

		if (!isfe)
		{
			sb.AppendLine("\t\tprivate static readonly Uri _defaultUri = new Uri(\"ms-appx:///\");");
			sb.AppendLine("\t\tpublic Uri BaseUri { get; internal set; } = _defaultUri;");
			sb.AppendLine();
		}

		// GetDependencyPropertyValue / SetDependencyPropertyValue (iOS-specific)
		sb.AppendLine("\t\t/// <summary>");
		sb.AppendLine("\t\t/// Provides a native value for the dependency property with the given name on the current instance. If the value is a primitive type,");
		sb.AppendLine("\t\t/// its native representation is returned. Otherwise, the <see cref=\"object.ToString\"/> implementation is used/returned instead.");
		sb.AppendLine("\t\t/// </summary>");
		sb.AppendLine("\t\t/// <param name=\"dependencyPropertyName\">The name of the target dependency property</param>");
		sb.AppendLine("\t\t/// <returns>The content of the target dependency property (its actual value if it is a primitive type ot its <see cref=\"object.ToString\"/> representation otherwise</returns>");
		sb.AppendLine("\t\t[global::Foundation.Export(\"getDependencyPropertyValue:\")]");
		sb.AppendLine("\t\tpublic global::Foundation.NSString GetDependencyPropertyValue(global::Foundation.NSString dependencyPropertyName)");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tvar dpValue = UIElement.GetDependencyPropertyValueInternal(this, dependencyPropertyName);");
		sb.AppendLine("\t\t\tif (dpValue == null)");
		sb.AppendLine("\t\t\t{");
		sb.AppendLine("\t\t\t\treturn null;");
		sb.AppendLine("\t\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\t\t// If all else fails, just return the string representation of the DP's value");
		sb.AppendLine("\t\t\treturn new global::Foundation.NSString(dpValue.ToString());");
		sb.AppendLine("\t\t}");
		sb.AppendLine();
		sb.AppendLine();
		sb.AppendLine("\t\t/// <summary>");
		sb.AppendLine("\t\t/// Sets the specified dependency property value using the format \"name|value\"");
		sb.AppendLine("\t\t/// </summary>");
		sb.AppendLine("\t\t/// <param name=\"dependencyPropertyNameAndvalue\">The name and value of the property</param>");
		sb.AppendLine("\t\t/// <returns>The currenty set value at the Local precedence</returns>");
		sb.AppendLine("\t\t[global::Foundation.Export(\"setDependencyPropertyValue:\")]");
		sb.AppendLine("\t\tpublic global::Foundation.NSString SetDependencyPropertyValue(global::Foundation.NSString dependencyPropertyNameAndValue)");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\t\treturn new global::Foundation.NSString(UIElement.SetDependencyPropertyValueInternal(this, dependencyPropertyNameAndValue) ?? \"\");");
		sb.AppendLine("\t\t}");
		sb.AppendLine();

		// AutomationPeer
		sb.AppendLine("\t\t#region AutomationPeer");
		sb.AppendLine();
		sb.AppendLine("\t\tprivate AutomationPeer _automationPeer;");
		sb.AppendLine();

		if (!m.HasAutomationPeer)
		{
			sb.AppendLine("\t\tprivate AutomationPeer OnCreateAutomationPeerOverride()");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\treturn null;");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
			sb.AppendLine("\t\tprivate string GetAccessibilityInnerTextOverride()");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\treturn null;");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
		}

		sb.AppendLine("\t\tprotected");
		if (!isfe)
		{
			sb.AppendLine("\t\tvirtual");
		}
		else
		{
			sb.AppendLine("\t\toverride");
		}
		sb.AppendLine("\t\tAutomationPeer OnCreateAutomationPeer()");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tif (OnCreateAutomationPeerOverride() is AutomationPeer automationPeer)");
		sb.AppendLine("\t\t\t{");
		sb.AppendLine("\t\t\t\treturn automationPeer;");
		sb.AppendLine("\t\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\t\tif (AutomationProperties.GetName(this) is string name && !string.IsNullOrEmpty(name))");
		sb.AppendLine("\t\t\t{");
		sb.AppendLine("\t\t\t\treturn new FrameworkElementAutomationPeer(this);");
		sb.AppendLine("\t\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\t\treturn null;");
		sb.AppendLine("\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\tpublic virtual string GetAccessibilityInnerText() // TODO: internal");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tif (GetAccessibilityInnerTextOverride() is string innerText)");
		sb.AppendLine("\t\t\t{");
		sb.AppendLine("\t\t\t\treturn innerText;");
		sb.AppendLine("\t\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\t\treturn null;");
		sb.AppendLine("\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\tpublic AutomationPeer GetAutomationPeer() // TODO: internal");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tif (_automationPeer == null)");
		sb.AppendLine("\t\t\t{");
		sb.AppendLine("\t\t\t\t_automationPeer = OnCreateAutomationPeer();");
		sb.AppendLine("\t\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\t\treturn _automationPeer;");
		sb.AppendLine("\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\tpublic override bool AccessibilityActivate()");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\treturn GetAutomationPeer()?.AccessibilityActivate() ?? false;");
		sb.AppendLine("\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\tpublic override bool IsAccessibilityElement");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tget => GetAutomationPeer()?.UpdateAccessibilityElement() ?? false;");
		sb.AppendLine("\t\t\tset => base.IsAccessibilityElement = value;");
		sb.AppendLine("\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\t#endregion");

		sb.AppendLine("\t}");
		sb.AppendLine("}");
		sb.AppendLine();
		if (!m.TvOS)
		{
			sb.AppendLine("#endif");
		}
		sb.AppendLine();
	}

	private static void GenerateSizeDependencyProperty(StringBuilder sb, string name, string defaultValue = "double.NaN")
	{
		sb.AppendLine($"\t\t#region {name} Dependency Property");
		sb.AppendLine($"\t\t[GeneratedDependencyProperty(DefaultValue = {defaultValue}, Options=FrameworkPropertyMetadataOptions.AutoConvert, ChangedCallbackName = nameof(OnGenericPropertyUpdated))]");
		sb.AppendLine($"\t\tpublic static DependencyProperty {name}Property {{ get ; }} = Create{name}Property();");
		sb.AppendLine();
		sb.AppendLine($"\t\tpublic double {name}");
		sb.AppendLine("\t\t{");
		sb.AppendLine($"\t\t\tget => Get{name}Value();");
		sb.AppendLine($"\t\t\tset => Set{name}Value(value);");
		sb.AppendLine("\t\t}");
		sb.AppendLine($"\t\t#endregion");
		sb.AppendLine();
	}
}
