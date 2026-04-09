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
public sealed class FrameworkElementAndroidMixinGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		// PostInitializationOutput makes code visible to both the XAML generator
		// (which needs to see property types) and DependencyPropertyGenerator
		// (which processes [GeneratedDependencyProperty] attributes).
		// Platform filtering is handled by #if __ANDROID__ in the generated code.
		context.RegisterPostInitializationOutput(static ctx =>
		{
			ctx.AddSource("FrameworkElementMixins.Android.g.cs", GenerateFrameworkElementMixins());
		});

		// EffectiveViewport partials need AdditionalFiles, so they use RegisterSourceOutput.
		var platformProvider = context.AnalyzerConfigOptionsProvider.Select(static (options, ct) =>
		{
			options.GlobalOptions.TryGetValue("build_property.DefineConstantsProperty", out var constants);
			return constants?.Contains("__ANDROID__") == true;
		});

		var evpFileProvider = context.AdditionalTextsProvider
			.Where(static file => file.Path.EndsWith("FrameworkElement.EffectiveViewport.cs", StringComparison.OrdinalIgnoreCase))
			.Select(static (file, ct) => file.GetText(ct)?.ToString())
			.Collect();

		var combined = platformProvider.Combine(evpFileProvider);

		context.RegisterSourceOutput(combined, static (ctx, data) =>
		{
			var (isAndroid, evpFiles) = data;
			if (!isAndroid)
			{
				return;
			}

			var evpContent = evpFiles.FirstOrDefault(f => f != null);
			if (evpContent != null)
			{
				foreach (var target in EffectiveViewportTargets)
				{
					var transformed = TransformEffectiveViewport(evpContent, target.FullTypeName, "true");
					ctx.AddSource($"{target.ClassName}.FrameworkElement.EffectiveViewport.g.cs", transformed);
				}
			}
		});
	}

	#region EffectiveViewport Partial Generation

	private sealed class EvpTarget
	{
		public string FullTypeName { get; }
		public string ClassName => FullTypeName.Split('.').Last();

		public EvpTarget(string fullTypeName)
		{
			FullTypeName = fullTypeName;
		}
	}

	private static readonly EvpTarget[] EffectiveViewportTargets = new[]
	{
		new EvpTarget("Microsoft.UI.Xaml.Controls.NativeListViewBase"),
		new EvpTarget("Microsoft.UI.Xaml.Controls.NativePagedView"),
		new EvpTarget("Microsoft.UI.Xaml.Controls.NativeScrollContentPresenter"),
		new EvpTarget("Uno.UI.Xaml.Controls.NativeRefreshControl"),
		new EvpTarget("Microsoft.UI.Xaml.Controls.TextBoxView"),
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

	#region IFrameworkElement Implementation Generation

	private sealed class MixinParams
	{
		public string NamespaceName { get; set; } = "";
		public string ClassName { get; set; } = "";
		public string HasNewWidthHeight { get; set; } = "";
		public string HasNewMinWidthHeight { get; set; } = "";
		public string HasNewMaxWidthHeight { get; set; } = "";
		public bool OverridesAttachedToWindow { get; set; }
		public bool IsFocusable { get; set; }
		public bool IsUnoMotionTarget { get; set; }
		public bool OverridesOnLayout { get; set; } = true;
		public bool CallsBaseOnLayout { get; set; } = true;
		public bool IsFrameworkElement => ClassName == "FrameworkElement";
		public bool HasAutomationPeer { get; set; }
		public bool HasLayouter { get; set; }
		public string LoadingInvokeArgument { get; set; } = "this";
	}

	private static readonly MixinParams[] Mixins = new[]
	{
		new MixinParams
		{
			NamespaceName = "Microsoft.UI.Xaml",
			ClassName = "FrameworkElement",
			HasNewWidthHeight = "new",
			OverridesAttachedToWindow = true,
			OverridesOnLayout = false,
			IsUnoMotionTarget = true,
			HasLayouter = true,
		},
		new MixinParams
		{
			NamespaceName = "Microsoft.UI.Xaml.Controls",
			ClassName = "NativeListViewBase",
			HasNewWidthHeight = "new",
			IsUnoMotionTarget = true,
			LoadingInvokeArgument = "null",
		},
		new MixinParams
		{
			NamespaceName = "Microsoft.UI.Xaml.Controls",
			ClassName = "NativePagedView",
			HasNewWidthHeight = "new",
			HasLayouter = true,
			LoadingInvokeArgument = "null",
		},
		new MixinParams
		{
			NamespaceName = "Microsoft.UI.Xaml.Controls",
			ClassName = "NativeScrollContentPresenter",
			HasNewWidthHeight = "new",
			CallsBaseOnLayout = false,
			HasLayouter = true,
			LoadingInvokeArgument = "null",
		},
		new MixinParams
		{
			NamespaceName = "Uno.UI.Xaml.Controls",
			ClassName = "NativeRefreshControl",
			HasNewWidthHeight = "new",
			IsUnoMotionTarget = false,
			HasLayouter = true,
			LoadingInvokeArgument = "null",
		},
		new MixinParams
		{
			NamespaceName = "Microsoft.UI.Xaml.Controls",
			ClassName = "TextBoxView",
			HasNewWidthHeight = "new",
			HasNewMinWidthHeight = "new",
			HasNewMaxWidthHeight = "new",
			LoadingInvokeArgument = "null",
		},
		new MixinParams
		{
			NamespaceName = "Uno.UI.Controls.Legacy",
			ClassName = "GridView",
			HasNewWidthHeight = "new",
			LoadingInvokeArgument = "null",
		},
		new MixinParams
		{
			NamespaceName = "Uno.UI.Controls.Legacy",
			ClassName = "ListView",
			HasNewWidthHeight = "new",
			LoadingInvokeArgument = "null",
		},
		new MixinParams
		{
			NamespaceName = "Uno.UI.Controls.Legacy",
			ClassName = "HorizontalGridView",
			HasNewWidthHeight = "new",
			LoadingInvokeArgument = "null",
		},
		new MixinParams
		{
			NamespaceName = "Uno.UI.Controls.Legacy",
			ClassName = "HorizontalListView",
			HasNewWidthHeight = "new",
			LoadingInvokeArgument = "null",
		},
	};

	private static string GenerateFrameworkElementMixins()
	{
		var sb = new StringBuilder();
		sb.AppendLine("// <auto-generated>");
		sb.AppendLine("// This file was generated by FrameworkElementAndroidMixinGenerator. Do not edit manually.");
		sb.AppendLine("// </auto-generated>");
		sb.AppendLine();
		sb.AppendLine("#if __ANDROID__");
		sb.AppendLine();
		sb.AppendLine("#pragma warning disable 108");
		sb.AppendLine("#pragma warning disable 109 // The member does not hide an accessible member. The new keyword is not required.");
		sb.AppendLine();
		sb.AppendLine("using Android.Runtime;");
		sb.AppendLine("using Android.Views;");
		sb.AppendLine("using Android.Views.Accessibility;");
		sb.AppendLine("using Uno.Diagnostics.Eventing;");
		sb.AppendLine("using Uno.Disposables;");
		sb.AppendLine("using Uno.Extensions;");
		sb.AppendLine("using Uno.Foundation.Logging;");
		sb.AppendLine("using Uno.UI;");
		sb.AppendLine("using Uno.UI.DataBinding;");
		sb.AppendLine("using Uno.UI.Extensions;");
		sb.AppendLine("using Uno.UI.Helpers;");
		sb.AppendLine("using Uno.UI.Media;");
		sb.AppendLine("using System;");
		sb.AppendLine("using System.ComponentModel;");
		sb.AppendLine("using System.Runtime.CompilerServices;");
		sb.AppendLine("using Windows.Foundation;");
		sb.AppendLine("using Microsoft.UI.Xaml;");
		sb.AppendLine("using Microsoft.UI.Xaml.Automation;");
		sb.AppendLine("using Microsoft.UI.Xaml.Automation.Peers;");
		sb.AppendLine("using Microsoft.UI.Xaml.Automation.Provider;");
		sb.AppendLine("using Microsoft.UI.Xaml.Data;");
		sb.AppendLine("using Microsoft.UI.Xaml.Media;");
		sb.AppendLine("using Microsoft.UI.Xaml.Media.Animation;");
		sb.AppendLine("using global::Microsoft.UI.Xaml.Input;");
		sb.AppendLine("using Point = Windows.Foundation.Point;");
		sb.AppendLine("using Action = global::System.Action;");
		sb.AppendLine("using Uno.UI.Xaml;");
		sb.AppendLine();

		foreach (var mixin in Mixins)
		{
			GenerateAndroidMixin(sb, mixin);
		}

		sb.AppendLine("#endif");
		return sb.ToString();
	}

	private static void GenerateAndroidMixin(StringBuilder sb, MixinParams m)
	{
		var isfe = m.IsFrameworkElement;
		var cn = m.ClassName;

		sb.AppendLine($"namespace {m.NamespaceName}");
		sb.AppendLine("{");
		sb.AppendLine($"\tpartial class {cn} : IFrameworkElement, IXUidProvider, IFrameworkElementInternal");
		sb.AppendLine("\t{");
		sb.AppendLine($"\t\tprivate readonly static IEventProvider _trace = Tracing.Get(FrameworkElement.TraceProvider.Id);");
		sb.AppendLine();

		if (!isfe)
		{
			sb.AppendLine("\t\tpublic event DependencyPropertyChangedEventHandler IsEnabledChanged;");
			sb.AppendLine();
		}

		sb.AppendLine("\t\tpublic event TypedEventHandler<FrameworkElement, object> Loading;");
		sb.AppendLine();
		sb.AppendLine("\t\tpublic event RoutedEventHandler Loaded;");
		sb.AppendLine();
		sb.AppendLine("\t\tpublic event RoutedEventHandler Unloaded;");
		sb.AppendLine();
		sb.AppendLine("\t\tpublic event SizeChangedEventHandler SizeChanged;");
		sb.AppendLine();
		sb.AppendLine("\t\tstring IXUidProvider.Uid { get; set; }");
		sb.AppendLine();
		sb.AppendLine($"\t\tbool IFrameworkElementInternal.HasLayouter => {(m.HasLayouter ? "true" : "false")};");
		sb.AppendLine();

		// FindName
		sb.AppendLine("\t\tpublic object FindName(string name)");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tvar viewGroup = ((object)this) as ViewGroup;");
		sb.AppendLine();
		sb.AppendLine("\t\t\tif (viewGroup != null)");
		sb.AppendLine("\t\t\t{");
		sb.AppendLine("\t\t\t\treturn IFrameworkElementHelper.FindName(this, viewGroup, name);");
		sb.AppendLine("\t\t\t}");
		sb.AppendLine("\t\t\telse");
		sb.AppendLine("\t\t\t{");
		sb.AppendLine("\t\t\t\treturn null;");
		sb.AppendLine("\t\t\t}");
		sb.AppendLine("\t\t}");
		sb.AppendLine();

		// AdjustArrange
		sb.AppendLine("\t\tpartial void AdjustArrangePartial(ref Size size);");
		sb.AppendLine();
		sb.AppendLine("\t\tSize IFrameworkElement.AdjustArrange(Size size)");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tAdjustArrangePartial(ref size);");
		sb.AppendLine();
		sb.AppendLine("\t\t\treturn size;");
		sb.AppendLine("\t\t}");
		sb.AppendLine();

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

		if (!isfe)
		{
			// Parent
			sb.AppendLine("\t\t/// <summary>");
			sb.AppendLine("\t\t/// Gets the parent of this FrameworkElement in the object tree.");
			sb.AppendLine("\t\t/// </summary>");
			sb.AppendLine("\t\tpublic new DependencyObject Parent => ((IDependencyObjectStoreProvider)this).Store.Parent as DependencyObject;");
			sb.AppendLine();

			// IsRenderingSuspended
			sb.AppendLine("\t\t// This is also defined in UIElement for actual UIElement hierarchy");
			sb.AppendLine("\t\tinternal bool IsRenderingSuspended { get; set; }");
			sb.AppendLine();

			// LayoutUpdated
			sb.AppendLine("\t\tpublic event EventHandler<object> LayoutUpdated;");
			sb.AppendLine();
			sb.AppendLine("\t\tinternal virtual void OnLayoutUpdated()");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\tif (LayoutUpdated != null)");
			sb.AppendLine("\t\t\t{");
			sb.AppendLine("\t\t\t\tLayoutUpdated(this, new RoutedEventArgs(this));");
			sb.AppendLine("\t\t\t}");
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

		// Background DP
		sb.AppendLine("\t\t#region Background Dependency Property");
		sb.AppendLine();
		sb.AppendLine("\t\t[GeneratedDependencyProperty(DefaultValue = null, Options=FrameworkPropertyMetadataOptions.ValueInheritsDataContext, ChangedCallback = true)]");
		sb.AppendLine($"\t\tpublic static DependencyProperty BackgroundProperty {{ get ; }} = CreateBackgroundProperty();");
		sb.AppendLine();
		sb.AppendLine("\t\tpublic new Brush Background");
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
		sb.AppendLine("\t\t\t_backgroundBrushChangedSubscription = Brush.SetupBrushChanged(newValue, ref _brushChanged, () => SetBackgroundColor(Brush.GetFallbackColor(newValue)));");
		sb.AppendLine("\t\t}");
		sb.AppendLine("\t\t#endregion");
		sb.AppendLine();

		if (!isfe)
		{
			// Opacity DP
			sb.AppendLine("\t\t#region Opacity Dependency Property");
			sb.AppendLine($"\t\tpublic double Opacity");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\tget { return (double)this.GetValue(OpacityProperty); }");
			sb.AppendLine("\t\t\tset { this.SetValue(OpacityProperty, value); }");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
			sb.AppendLine($"\t\t// Using a DependencyProperty as the backing store for Opacity.  This enables animation, styling, binding, etc...");
			sb.AppendLine($"\t\tpublic static DependencyProperty OpacityProperty {{ get ; }} =");
			sb.AppendLine($"\t\t\tDependencyProperty.Register(\"Opacity\", typeof(double), typeof({cn}), new FrameworkPropertyMetadata(defaultValue: 1.0, propertyChangedCallback: (s, a) => (({cn})s).OnOpacityChanged()));");
			sb.AppendLine();
			sb.AppendLine("\t\tprivate void OnOpacityChanged()");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\tAlpha = IsRenderingSuspended ? 0 : (float)Opacity;");
			sb.AppendLine("\t\t}");
			sb.AppendLine("\t\t#endregion");
			sb.AppendLine();

			// Style DP
			sb.AppendLine("\t\t#region Style DependencyProperty");
			sb.AppendLine();
			sb.AppendLine("\t\tpublic Style Style");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\tget { return (Style)GetValue(StyleProperty); }");
			sb.AppendLine("\t\t\tset { SetValue(StyleProperty, value); }");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
			sb.AppendLine($"\t\tpublic static DependencyProperty StyleProperty {{ get ; }} =");
			sb.AppendLine("\t\t\tDependencyProperty.Register(");
			sb.AppendLine("\t\t\t\t\"Style\",");
			sb.AppendLine("\t\t\t\ttypeof(Style),");
			sb.AppendLine($"\t\t\t\ttypeof({cn}),");
			sb.AppendLine("\t\t\t\tnew FrameworkPropertyMetadata(");
			sb.AppendLine("\t\t\t\t\tdefaultValue: null,");
			sb.AppendLine("\t\t\t\t\toptions: FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext,");
			sb.AppendLine($"\t\t\t\t\tpropertyChangedCallback: (s, e) => (({cn})s)?.OnStyleChanged((Style)e.OldValue, (Style)e.NewValue)");
			sb.AppendLine("\t\t\t\t)");
			sb.AppendLine("\t\t\t);");
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
			sb.AppendLine("\t\tpublic virtual bool IsEnabled");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\tget => GetIsEnabledValue();");
			sb.AppendLine("\t\t\tset => SetIsEnabledValue(value);");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
			sb.AppendLine("\t\tprivate void OnIsEnabledChanged(DependencyPropertyChangedEventArgs e)");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\tvar newValue = (bool)e.NewValue;");
			sb.AppendLine();
			if (m.IsUnoMotionTarget)
			{
				sb.AppendLine("\t\t\tbase.SetNativeIsEnabled(newValue);");
			}
			sb.AppendLine("\t\t\tEnabled = newValue;");
			sb.AppendLine();
			if (m.IsFocusable)
			{
				sb.AppendLine("\t\t\tFocusable = newValue;");
				sb.AppendLine("\t\t\tFocusableInTouchMode = newValue;");
			}
			sb.AppendLine();
			sb.AppendLine("\t\t\tIsEnabledChanged?.Invoke(this, e);");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
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
			sb.AppendLine("\t\t\tDependencyProperty.Register(");
			sb.AppendLine("\t\t\t\t\"IsHitTestVisible\",");
			sb.AppendLine("\t\t\t\ttypeof(bool),");
			sb.AppendLine($"\t\t\t\ttypeof({cn}),");
			sb.AppendLine("\t\t\t\tnew FrameworkPropertyMetadata(");
			sb.AppendLine("\t\t\t\t\ttrue,");
			sb.AppendLine($"\t\t\t\t\t(s, e) => (s as {cn}).OnIsHitTestVisibleChanged((bool)e.OldValue, (bool)e.NewValue)");
			sb.AppendLine("\t\t\t\t)");
			sb.AppendLine("\t\t\t);");
			sb.AppendLine();
			sb.AppendLine("\t\tprotected virtual void OnIsHitTestVisibleChanged(bool oldValue, bool newValue)");
			sb.AppendLine("\t\t{");
			sb.AppendLine();
			sb.AppendLine("\t\t}");
			sb.AppendLine();

			if (!m.IsUnoMotionTarget)
			{
				sb.AppendLine("\t\tpublic override bool DispatchTouchEvent(MotionEvent e)");
				sb.AppendLine("\t\t{");
				sb.AppendLine("\t\t\tif (IsHitTestVisible && IsEnabled && HitCheck())");
				sb.AppendLine("\t\t\t{");
				sb.AppendLine("\t\t\t\treturn base.DispatchTouchEvent(e);");
				sb.AppendLine("\t\t\t}");
				sb.AppendLine();
				sb.AppendLine("\t\t\treturn false;");
				sb.AppendLine("\t\t}");
				sb.AppendLine();
				sb.AppendLine("\t\tprotected virtual bool HitCheck()");
				sb.AppendLine("\t\t{");
				sb.AppendLine("\t\t\tvar hitCheck = true;");
				sb.AppendLine("\t\t\tHitCheckOverridePartial(ref hitCheck);");
				sb.AppendLine();
				sb.AppendLine("\t\t\treturn hitCheck;");
				sb.AppendLine("\t\t}");
			}

			sb.AppendLine();
			sb.AppendLine("\t\tpartial void HitCheckOverridePartial(ref bool hitCheck);");
			sb.AppendLine();
			sb.AppendLine("\t\t#endregion");
			sb.AppendLine();
		}

		if (!isfe)
		{
			// Visibility DP
			sb.AppendLine("\t\t#region Visibility Dependency Property");
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
			sb.AppendLine("\t\t\tvar newNativeVisibility = newValue == Visibility.Visible ? global::Android.Views.ViewStates.Visible : global::Android.Views.ViewStates.Gone;");
			sb.AppendLine();
			sb.AppendLine("\t\t\tvar bindableView = ((object)this) as Uno.UI.Controls.BindableView;");
			sb.AppendLine();
			sb.AppendLine("\t\t\tif (bindableView != null)");
			sb.AppendLine("\t\t\t{");
			sb.AppendLine("\t\t\t\t// This cast is different for performance reasons. See the");
			sb.AppendLine("\t\t\t\t// UnoViewGroup java class for more details.");
			sb.AppendLine("\t\t\t\tbindableView.Visibility = newNativeVisibility;");
			sb.AppendLine("\t\t\t\tbindableView.RequestLayout();");
			sb.AppendLine("\t\t\t}");
			sb.AppendLine("\t\t\telse");
			sb.AppendLine("\t\t\t{");
			sb.AppendLine("\t\t\t\t((View)this).Visibility = newNativeVisibility;");
			sb.AppendLine("\t\t\t\t((View)this).RequestLayout();");
			sb.AppendLine("\t\t\t}");
			sb.AppendLine();
			sb.AppendLine("\t\t\tOnVisibilityChangedPartial(oldValue, newValue);");
			sb.AppendLine();
			sb.AppendLine("\t\t}");
			sb.AppendLine();
			sb.AppendLine("\t\tpartial void OnVisibilityChangedPartial(Visibility oldValue, Visibility newValue);");
			sb.AppendLine();
			sb.AppendLine("\t\tpublic new Visibility Visibility");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\tget { return (Visibility)this.GetValue(VisibilityProperty); }");
			sb.AppendLine("\t\t\tset { this.SetValue(VisibilityProperty, value); }");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
			sb.AppendLine("\t\t#endregion");
			sb.AppendLine();
		}

		// Name DP
		sb.AppendLine("\t\t#region Name Dependency Property");
		sb.AppendLine();
		sb.AppendLine("\t\tprivate void OnNameChanged(string oldValue, string newValue)");
		sb.AppendLine("\t\t{");
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
		sb.AppendLine("\t\t[GeneratedDependencyProperty(DefaultValue = HorizontalAlignment.Stretch, Options = FrameworkPropertyMetadataOptions.AffectsArrange, ChangedCallbackName = nameof(OnGenericPropertyUpdated))]");
		sb.AppendLine($"\t\tpublic static DependencyProperty HorizontalAlignmentProperty {{ get ; }} = CreateHorizontalAlignmentProperty();");
		sb.AppendLine();
		sb.AppendLine("\t\tpublic HorizontalAlignment HorizontalAlignment");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tget => GetHorizontalAlignmentValue();");
		sb.AppendLine("\t\t\tset => SetHorizontalAlignmentValue(value);");
		sb.AppendLine("\t\t}");
		sb.AppendLine("\t\t#endregion");
		sb.AppendLine();

		// VerticalAlignment DP
		sb.AppendLine("\t\t#region VerticalAlignment Dependency Property");
		sb.AppendLine("\t\t[GeneratedDependencyProperty(DefaultValue = VerticalAlignment.Stretch, Options = FrameworkPropertyMetadataOptions.AffectsArrange, ChangedCallbackName = nameof(OnGenericPropertyUpdated))]");
		sb.AppendLine($"\t\tpublic static DependencyProperty VerticalAlignmentProperty {{ get ; }} = CreateVerticalAlignmentProperty();");
		sb.AppendLine();
		sb.AppendLine("\t\tpublic VerticalAlignment VerticalAlignment");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tget => GetVerticalAlignmentValue();");
		sb.AppendLine("\t\t\tset => SetVerticalAlignmentValue(value);");
		sb.AppendLine("\t\t}");
		sb.AppendLine("\t\t#endregion");
		sb.AppendLine();

		// Width/Height DPs
		GenerateSizeDependencyProperty(sb, "Width", "double.NaN", m.HasNewWidthHeight);
		GenerateSizeDependencyProperty(sb, "Height", "double.NaN", m.HasNewWidthHeight);
		GenerateSizeDependencyProperty(sb, "MinWidth", "0.0", m.HasNewMinWidthHeight);
		GenerateSizeDependencyProperty(sb, "MinHeight", "0.0", m.HasNewMinWidthHeight);
		GenerateSizeDependencyProperty(sb, "MaxWidth", "double.PositiveInfinity", m.HasNewMaxWidthHeight);
		GenerateSizeDependencyProperty(sb, "MaxHeight", "double.PositiveInfinity", m.HasNewMaxWidthHeight);

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
			sb.AppendLine("\t\t\t\tview.OnRenderTransformSet();");
			sb.AppendLine("\t\t\t}");
			sb.AppendLine("\t\t\telse");
			sb.AppendLine("\t\t\t{");
			sb.AppendLine("\t\t\t\t// Sanity");
			sb.AppendLine("\t\t\t\tview._renderTransform = null;");
			sb.AppendLine("\t\t\t}");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
			sb.AppendLine("\t\tprivate NativeRenderTransformAdapter _renderTransform;");
			sb.AppendLine("\t\tprivate bool _renderTransformRegisteredParentChanged;");
			sb.AppendLine("\t\tprivate static void RenderTransformOnParentChanged(object dependencyObject, object _, DependencyObjectParentChangedEventArgs args)");
			sb.AppendLine($"\t\t\t=> (({cn})dependencyObject)._renderTransform?.UpdateParent(args.PreviousParent, args.NewParent);");
			sb.AppendLine("\t\tprivate void OnRenderTransformSet()");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\t// On first Transform set, we register to the parent changed, so we can enable or disable the static transformations on it");
			sb.AppendLine("\t\t\tif (!_renderTransformRegisteredParentChanged)");
			sb.AppendLine("\t\t\t{");
			sb.AppendLine("\t\t\t\t((IDependencyObjectStoreProvider)this).Store.RegisterSelfParentChangedCallback(RenderTransformOnParentChanged);");
			sb.AppendLine("\t\t\t\t_renderTransformRegisteredParentChanged = true;");
			sb.AppendLine("\t\t\t}");
			sb.AppendLine("\t\t}");
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

		// IsLoaded
		sb.AppendLine("\t\t/// <summary>");
		sb.AppendLine("\t\t/// Determines if the view is currently loaded (attached to the a window)");
		sb.AppendLine("\t\t/// </summary>");
		sb.AppendLine("\t\tpublic bool IsLoaded { get; private set;}");
		sb.AppendLine();

		// OnPostLoading
		sb.AppendLine("\t\tprivate protected virtual void OnPostLoading() {}");
		sb.AppendLine();

		// OnLoading
		sb.AppendLine("\t\tinternal virtual void OnLoading()");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tOnLoadingPartial();");
		sb.AppendLine($"\t\t\tLoading?.Invoke({m.LoadingInvokeArgument}, null);");
		sb.AppendLine("\t\t\tOnPostLoading();");
		sb.AppendLine("\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\tpartial void OnLoadingPartial();");
		sb.AppendLine();

		// OnLoaded
		sb.AppendLine("\t\tprivate protected virtual void OnLoaded()");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tIsLoaded = true;");
		sb.AppendLine();
		sb.AppendLine("\t\t\tOnLoadedPartial();");
		sb.AppendLine();
		sb.AppendLine("\t\t\tLoaded?.Invoke(this, new RoutedEventArgs(this));");
		sb.AppendLine("\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\tpartial void OnLoadedPartial();");
		sb.AppendLine();

		// OnUnloaded
		sb.AppendLine("\t\tprivate protected virtual void OnUnloaded()");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tIsLoaded = false;");
		sb.AppendLine();
		sb.AppendLine("\t\t\tOnUnloadedPartial();");
		sb.AppendLine();
		sb.AppendLine("\t\t\tUnloaded?.Invoke(this, new RoutedEventArgs(this));");
		sb.AppendLine("\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\tpartial void OnUnloadedPartial();");
		sb.AppendLine();

		// OnGenericPropertyUpdated
		sb.AppendLine("\t\tprivate void OnGenericPropertyUpdated(DependencyPropertyChangedEventArgs args)");
		sb.AppendLine("\t\t{");
		if (isfe)
		{
			sb.AppendLine("\t\t\tOnGenericPropertyUpdatedPartial(args);");
		}
		sb.AppendLine("\t\t\tthis.InvalidateMeasure();");
		sb.AppendLine("\t\t}");
		sb.AppendLine();

		// OnLayout (conditional)
		if (m.OverridesOnLayout)
		{
			sb.AppendLine("\t\tprivate Size _actualSize;");
			sb.AppendLine("\t\tprotected override void OnLayout(bool changed, int left, int top, int right, int bottom)");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\tvar newSize = new Size(right - left, bottom - top).PhysicalToLogicalPixels();");
			sb.AppendLine("\t\t\tvar previousSize = _actualSize;");
			sb.AppendLine();
			if (m.CallsBaseOnLayout)
			{
				sb.AppendLine("\t\t\tbase.OnLayout(changed, left, top, right, bottom);");
			}
			sb.AppendLine();
			sb.AppendLine("\t\t\tOnLayoutPartial(changed, left, top, right, bottom);");
			sb.AppendLine();
			sb.AppendLine("\t\t\t_actualSize = newSize;");
			sb.AppendLine();
			sb.AppendLine("\t\t\tif (newSize != previousSize)");
			sb.AppendLine("\t\t\t{");
			sb.AppendLine("\t\t\t\tSizeChanged?.Invoke(this, new SizeChangedEventArgs(this, previousSize, newSize));");
			sb.AppendLine("\t\t\t\t_renderTransform?.UpdateSize(newSize); // TODO: Move ** BEFORE ** base.OnLayout() ???");
			sb.AppendLine("\t\t\t}");
			sb.AppendLine("\t\t}");
			sb.AppendLine();
			sb.AppendLine("\t\tpartial void OnLayoutPartial(bool changed, int left, int top, int right, int bottom);");
			sb.AppendLine();
		}

		if (!isfe)
		{
			// BaseUri
			sb.AppendLine("\t\tprivate static readonly Uri _defaultUri = new Uri(\"ms-appx:///\");");
			sb.AppendLine("\t\tpublic Uri BaseUri { get; internal set; } = _defaultUri;");
			sb.AppendLine();
		}

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
		sb.AppendLine("\t\tpublic virtual string GetAccessibilityInnerText()");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tif (GetAccessibilityInnerTextOverride() is string innerText)");
		sb.AppendLine("\t\t\t{");
		sb.AppendLine("\t\t\t\treturn innerText;");
		sb.AppendLine("\t\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\t\treturn null;");
		sb.AppendLine("\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\tpublic AutomationPeer GetAutomationPeer()");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tif (_automationPeer == null)");
		sb.AppendLine("\t\t\t{");
		sb.AppendLine("\t\t\t\t_automationPeer = OnCreateAutomationPeer();");
		sb.AppendLine("\t\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\t\treturn _automationPeer;");
		sb.AppendLine("\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\tpublic override void OnInitializeAccessibilityNodeInfo(AccessibilityNodeInfo info)");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tbase.OnInitializeAccessibilityNodeInfo(info);");
		sb.AppendLine("\t\t\tGetAutomationPeer()?.OnInitializeAccessibilityNodeInfo(info);");
		sb.AppendLine("\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\tpublic override void SendAccessibilityEvent([GeneratedEnum] EventTypes eventType)");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tbase.SendAccessibilityEvent(eventType);");
		sb.AppendLine("\t\t\tGetAutomationPeer()?.SendAccessibilityEvent(eventType);");
		sb.AppendLine("\t\t}");
		sb.AppendLine();
		sb.AppendLine("\t\t#endregion");

		sb.AppendLine("\t}");
		sb.AppendLine("}");
		sb.AppendLine();
	}

	private static void GenerateSizeDependencyProperty(StringBuilder sb, string name, string defaultValue, string newModifier)
	{
		sb.AppendLine($"\t\t#region {name} Dependency Property");
		sb.AppendLine($"\t\t[GeneratedDependencyProperty(DefaultValue = {defaultValue}, Options=FrameworkPropertyMetadataOptions.AutoConvert, ChangedCallbackName = nameof(OnGenericPropertyUpdated))]");
		sb.AppendLine($"\t\tpublic static DependencyProperty {name}Property {{ get ; }} = Create{name}Property();");
		sb.AppendLine();
		sb.AppendLine($"\t\tpublic {newModifier} double {name}");
		sb.AppendLine("\t\t{");
		sb.AppendLine($"\t\t\tget => Get{name}Value();");
		sb.AppendLine($"\t\t\tset => Set{name}Value(value);");
		sb.AppendLine("\t\t}");
		sb.AppendLine($"\t\t#endregion");
		sb.AppendLine();
	}

	#endregion
}
