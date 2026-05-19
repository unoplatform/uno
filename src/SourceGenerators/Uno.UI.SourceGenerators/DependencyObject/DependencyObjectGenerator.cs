#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Roslyn;
using Uno.UI.SourceGenerators.Helpers;
using Uno.UI.SourceGenerators.XamlGenerator;

namespace Uno.UI.SourceGenerators.DependencyObject
{
	[Generator]
	public partial class DependencyObjectGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			var compilationCtx = context.CompilationProvider.Select(static (compilation, _) =>
				DependencyObjectCompilationContext.FromCompilation(compilation));

			var configCtx = context.AnalyzerConfigOptionsProvider.Select(static (options, _) =>
				DependencyObjectConfig.FromOptions(options));

			var candidates = context.SyntaxProvider.CreateSyntaxProvider(
					predicate: static (node, _) => node is ClassDeclarationSyntax,
					transform: static (ctx, ct) => TransformCandidate(ctx, ct))
				.Where(static d => d is not null)
				.Select(static (d, _) => d!);

			var combined = candidates.Combine(compilationCtx).Combine(configCtx);

			context.RegisterSourceOutput(combined, static (spc, tuple) =>
			{
				var ((typeData, compCtx), config) = tuple;
				EmitSource(spc, typeData, compCtx, config);
			});
		}

		private static DependencyObjectTypeData? TransformCandidate(GeneratorSyntaxContext ctx, CancellationToken ct)
		{
			if (ctx.SemanticModel.GetDeclaredSymbol(ctx.Node, ct) is not INamedTypeSymbol typeSymbol)
			{
				return null;
			}

			if (typeSymbol.TypeKind != TypeKind.Class)
			{
				return null;
			}

			// For partial classes, only emit from the first declaration so AddSource is called once per symbol.
			if (typeSymbol.DeclaringSyntaxReferences.Length > 1
				&& typeSymbol.DeclaringSyntaxReferences[0].GetSyntax(ct) != ctx.Node)
			{
				return null;
			}

			var comp = ctx.SemanticModel.Compilation;
			var dependencyObjectSymbol = comp.GetTypeByMetadataName(XamlConstants.Types.DependencyObject);
			if (dependencyObjectSymbol is null)
			{
				return null;
			}

			var isDependencyObject = typeSymbol.Interfaces.Any(t => SymbolEqualityComparer.Default.Equals(t, dependencyObjectSymbol))
				&& (typeSymbol.BaseType?.AllInterfaces.None(t => SymbolEqualityComparer.Default.Equals(t, dependencyObjectSymbol)) ?? true);

			if (!isDependencyObject)
			{
				return null;
			}

			var iosViewSymbol = comp.GetTypeByMetadataName("UIKit.UIView");
			var macosViewSymbol = comp.GetTypeByMetadataName("AppKit.NSView");
			var androidViewSymbol = comp.GetTypeByMetadataName("Android.Views.View");
			var androidActivitySymbol = comp.GetTypeByMetadataName("Android.App.Activity");
			var androidFragmentSymbol = comp.GetTypeByMetadataName("AndroidX.Fragment.App.Fragment");
			var unoViewGroupSymbol = comp.GetTypeByMetadataName("Uno.UI.UnoViewGroup");
			var bindableAttributeSymbol = comp.GetTypeByMetadataName("Microsoft.UI.Xaml.Data.BindableAttribute");
			var iFrameworkElementSymbol = comp.GetTypeByMetadataName(XamlConstants.Types.IFrameworkElement);
			var frameworkElementSymbol = comp.GetTypeByMetadataName("Microsoft.UI.Xaml.FrameworkElement");

			DependencyObjectDiagnosticInfo? diagnosticInfo = null;
			if (typeSymbol.Is(iosViewSymbol))
			{
				diagnosticInfo = new DependencyObjectDiagnosticInfo("UIKit.UIView", typeSymbol.Locations.FirstOrDefault() ?? Location.None);
			}
			else if (typeSymbol.Is(androidViewSymbol))
			{
				diagnosticInfo = new DependencyObjectDiagnosticInfo("Android.Views.View", typeSymbol.Locations.FirstOrDefault() ?? Location.None);
			}
			else if (typeSymbol.Is(macosViewSymbol))
			{
				diagnosticInfo = new DependencyObjectDiagnosticInfo("AppKit.NSView", typeSymbol.Locations.FirstOrDefault() ?? Location.None);
			}

			var containingHeaders = ImmutableArray.CreateBuilder<string>();
			for (var outer = typeSymbol.ContainingType; outer is not null; outer = outer.ContainingType)
			{
				containingHeaders.Insert(0, outer.GetDeclarationHeaderFromNamedTypeSymbol(null));
			}

			var selfHeader = typeSymbol.GetDeclarationHeaderFromNamedTypeSymbol(null);

			var ns = typeSymbol.ContainingNamespace is { IsGlobalNamespace: false } namespaceSymbol
				? namespaceSymbol.ToString()
				: null;

			bool HasNoUserMethod(string name)
				=> typeSymbol.GetMethodsWithName(name).None(IsNotDependencyObjectGeneratorSourceFile);

			var hasUserEquals = !HasNoUserMethod("Equals");
			var baseHasSealedEquals = typeSymbol.BaseType?.GetMethodsWithName("Equals").Any(m => m.IsSealed) ?? false;
			var hasOnPropertyChanged2OneParam = typeSymbol.GetMethodsWithName("OnPropertyChanged2").Any(m => m.Parameters.Length == 1);

			var flags = DependencyObjectTypeFlags.None;
			if (typeSymbol.IsSealed)
			{
				flags |= DependencyObjectTypeFlags.IsSealed;
			}
			if (typeSymbol.Is(iosViewSymbol))
			{
				flags |= DependencyObjectTypeFlags.IsIosView;
			}
			if (typeSymbol.Is(macosViewSymbol))
			{
				flags |= DependencyObjectTypeFlags.IsMacosView;
			}
			if (typeSymbol.Is(androidViewSymbol))
			{
				flags |= DependencyObjectTypeFlags.IsAndroidView;
			}
			if (typeSymbol.Is(androidActivitySymbol))
			{
				flags |= DependencyObjectTypeFlags.IsAndroidActivity;
			}
			if (typeSymbol.Is(androidFragmentSymbol))
			{
				flags |= DependencyObjectTypeFlags.IsAndroidFragment;
			}
			if (typeSymbol.Is(unoViewGroupSymbol))
			{
				flags |= DependencyObjectTypeFlags.IsUnoViewGroup;
			}
			if (typeSymbol.Is(frameworkElementSymbol))
			{
				flags |= DependencyObjectTypeFlags.IsFrameworkElement;
			}
			if (frameworkElementSymbol.Is(typeSymbol))
			{
				flags |= DependencyObjectTypeFlags.FrameworkElementInheritsFromThis;
			}
			if (typeSymbol.Interfaces.Any(t => SymbolEqualityComparer.Default.Equals(t, iFrameworkElementSymbol)))
			{
				flags |= DependencyObjectTypeFlags.ImplementsIFrameworkElement;
			}
			if (bindableAttributeSymbol is not null && typeSymbol.FindAttribute(bindableAttributeSymbol) is null)
			{
				flags |= DependencyObjectTypeFlags.NeedsBindableAttribute;
			}
			if (HasNoUserMethod("ToString"))
			{
				flags |= DependencyObjectTypeFlags.HasNoUserToString;
			}
			if (HasNoUserMethod("WillMoveToSuperview"))
			{
				flags |= DependencyObjectTypeFlags.HasNoUserWillMoveToSuperview;
			}
			if (HasNoUserMethod("ViewWillMoveToSuperview"))
			{
				flags |= DependencyObjectTypeFlags.HasNoUserViewWillMoveToSuperview;
			}
			if (HasNoUserMethod("OnAttachedToWindow"))
			{
				flags |= DependencyObjectTypeFlags.HasNoUserOnAttachedToWindow;
			}
			if (HasNoUserMethod("MovedToWindow"))
			{
				flags |= DependencyObjectTypeFlags.HasNoUserMovedToWindow;
			}
			if (HasNoUserMethod("ViewDidMoveToWindow"))
			{
				flags |= DependencyObjectTypeFlags.HasNoUserViewDidMoveToWindow;
			}
			if (!hasUserEquals && !baseHasSealedEquals)
			{
				flags |= DependencyObjectTypeFlags.CanOverrideEquals;
			}
			if (!hasOnPropertyChanged2OneParam)
			{
				flags |= DependencyObjectTypeFlags.NeedsOnPropertyChanged2Stub;
			}

			return new DependencyObjectTypeData(
				Name: typeSymbol.Name,
				HintName: typeSymbol.GetFullMetadataNameForFileName(),
				Namespace: ns,
				ContainingTypeHeaders: new EquatableArray<string>(containingHeaders.ToImmutable()),
				SelfClassHeader: selfHeader,
				Flags: flags,
				DiagnosticInfo: diagnosticInfo);
		}

		private static bool IsNotDependencyObjectGeneratorSourceFile(IMethodSymbol m)
		{
			return !m.Locations.FirstOrDefault()?.SourceTree?.FilePath.Contains(nameof(DependencyObjectGenerator)) ?? true;
		}

		private static void EmitSource(
			SourceProductionContext context,
			DependencyObjectTypeData data,
			DependencyObjectCompilationContext compCtx,
			DependencyObjectConfig config)
		{
			if (!compCtx.IsValidPlatform)
			{
				return;
			}

			if (!config.IsUnoSolution && data.DiagnosticInfo is { } diagInfo)
			{
				context.ReportDiagnostic(Diagnostic.Create(_descriptor, diagInfo.Location, diagInfo.BaseTypeName));
				return;
			}

			var builder = new IndentedStringBuilder();
			builder.Append(@"// <auto-generated>
// ******************************************************************
// This file has been generated by Uno.UI (DependencyObjectGenerator)
// ******************************************************************
// </auto-generated>

#pragma warning disable 1591 // Ignore missing XML comment warnings
");

			AnalyzerSuppressionsGenerator.Generate(builder, config.AnalyzerSuppressions.ToArray());

			builder.Append(@"
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using Uno.UI;
using Uno.UI.Controls;
using Uno.UI.DataBinding;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Uno.Diagnostics.Eventing;
");

			Action<IIndentedStringBuilder> beforeClassHeaderAction = b =>
			{
				if (data.HasFlag(DependencyObjectTypeFlags.NeedsBindableAttribute))
				{
					b.AppendLineIndented(@"[global::Microsoft.UI.Xaml.Data.Bindable]");
				}
			};

			var implementations = new string?[]
			{
				"IDependencyObjectStoreProvider",
				config.IsUnoSolution && !data.HasFlag(DependencyObjectTypeFlags.IsSealed) ? "IDependencyObjectInternal" : null,
				"IWeakReferenceProvider",
			}.Where(x => x is not null);

			using (OpenTypeContext(builder, data, beforeClassHeaderAction, afterClassHeader: " : " + string.Join(", ", implementations)))
			{
				GenerateDependencyObjectImplementation(data, builder, compCtx.HasDispatcherQueueOnDependencyObject, config.IsUnoSolution);
				GenerateIBinderImplementation(data, builder);
			}

			context.AddSource(data.HintName, builder.ToString());
		}

		private static IDisposable OpenTypeContext(
			IIndentedStringBuilder builder,
			DependencyObjectTypeData data,
			Action<IIndentedStringBuilder>? beforeClassHeaderAction,
			string afterClassHeader)
		{
			var disposables = new Stack<IDisposable>();
			if (data.Namespace is { } ns)
			{
				disposables.Push(builder.BlockInvariant($"namespace {ns}"));
			}
			foreach (var header in data.ContainingTypeHeaders)
			{
				disposables.Push(builder.BlockInvariant(header));
			}
			beforeClassHeaderAction?.Invoke(builder);
			disposables.Push(builder.BlockInvariant(data.SelfClassHeader + afterClassHeader));

			return Disposable.Create(() =>
			{
				while (disposables.Count > 0)
				{
					disposables.Pop().Dispose();
				}
			});
		}

		private static void GenerateIBinderImplementation(DependencyObjectTypeData data, IndentedStringBuilder builder)
		{
			WriteInitializer(builder);

			WriteToStringOverride(data, builder);

			WriteAndroidEqualityOverride(data, builder);

			WriteAndroidBinderDetails(data, builder);

			WriteAndroidAttachedToWindow(data, builder);

			WriteAttachToWindow(data, builder);
			WriteViewDidMoveToWindow(data, builder);

			WriteiOSMoveToSuperView(data, builder);
			WriteMacOSViewWillMoveToSuperview(data, builder);

			WriteDispose(data, builder);
			WriteBinderImplementation(data, builder);
		}

		private static void WriteToStringOverride(DependencyObjectTypeData data, IndentedStringBuilder builder)
		{
			if (data.HasFlag(DependencyObjectTypeFlags.HasNoUserToString))
			{
				builder.AppendIndented(@"public override string ToString() => GetType().FullName;");
			}
		}

		private static void WriteiOSMoveToSuperView(DependencyObjectTypeData data, IndentedStringBuilder builder)
		{
			var isiosView = data.HasFlag(DependencyObjectTypeFlags.IsIosView);
			var hasNoWillMoveToSuperviewMethod = data.HasFlag(DependencyObjectTypeFlags.HasNoUserWillMoveToSuperview);

			var overridesWillMoveToSuperview = isiosView && hasNoWillMoveToSuperviewMethod;

			if (overridesWillMoveToSuperview)
			{
				builder.AppendMultiLineIndented(@"
public override void WillMoveToSuperview(UIKit.UIView newsuper)
{
	base.WillMoveToSuperview(newsuper);

	WillMoveToSuperviewPartial(newsuper);
}

partial void WillMoveToSuperviewPartial(UIKit.UIView newsuper);
					");
			}
			else
			{
				builder.AppendIndented($"// Skipped _iosViewSymbol: {isiosView}, hasNoWillMoveToSuperviewMethod: {hasNoWillMoveToSuperviewMethod}");
			}
		}

		private static void WriteMacOSViewWillMoveToSuperview(DependencyObjectTypeData data, IndentedStringBuilder builder)
		{
			var isiosView = data.HasFlag(DependencyObjectTypeFlags.IsMacosView);
			var hasNoWillMoveToSuperviewMethod = data.HasFlag(DependencyObjectTypeFlags.HasNoUserViewWillMoveToSuperview);

			var overridesWillMoveToSuperview = isiosView && hasNoWillMoveToSuperviewMethod;

			if (overridesWillMoveToSuperview)
			{
				builder.AppendMultiLineIndented(@"
public override void ViewWillMoveToSuperview(AppKit.NSView newsuper)
{
	base.ViewWillMoveToSuperview(newsuper);

	WillMoveToSuperviewPartial(newsuper);
}

partial void WillMoveToSuperviewPartial(AppKit.NSView newsuper);
					");
			}
			else
			{
				builder.AppendIndented($"// Skipped _macosViewSymbol: {isiosView}, hasNoViewWillMoveToSuperviewMethod: {hasNoWillMoveToSuperviewMethod}");
				builder.AppendLine();
			}
		}

		private static void WriteAndroidAttachedToWindow(DependencyObjectTypeData data, IndentedStringBuilder builder)
		{
			var isAndroidView = data.HasFlag(DependencyObjectTypeFlags.IsAndroidView);
			var isAndroidActivity = data.HasFlag(DependencyObjectTypeFlags.IsAndroidActivity);
			var isAndroidFragment = data.HasFlag(DependencyObjectTypeFlags.IsAndroidFragment);
			var isUnoViewGroup = data.HasFlag(DependencyObjectTypeFlags.IsUnoViewGroup);
			var implementsIFrameworkElement = data.HasFlag(DependencyObjectTypeFlags.ImplementsIFrameworkElement);
			var hasOverridesAttachedToWindowAndroid = isAndroidView && data.HasFlag(DependencyObjectTypeFlags.HasNoUserOnAttachedToWindow);

			if (isAndroidView || isAndroidActivity || isAndroidFragment)
			{
				builder.AppendMultiLineIndented($@"
#if {hasOverridesAttachedToWindowAndroid} //Is Android view (that doesn't already override OnAttachedToWindow)

#if {isUnoViewGroup} //Is UnoViewGroup
					// Both methods below are implementation of abstract methods
					// which are called from onAttachedToWindow in Java.

					protected override void OnNativeLoaded()
					{{
						BinderAttachedToWindow();
					}}

					protected override void OnNativeUnloaded()
					{{
						BinderDetachedFromWindow();
					}}
#else //Not UnoViewGroup
					protected override void OnAttachedToWindow()
					{{
						base.OnAttachedToWindow();
						__Store.Parent = base.Parent;
#if {implementsIFrameworkElement} //Is IFrameworkElement
						OnLoading();
						OnLoaded();
#endif
						BinderAttachedToWindow();
					}}


					protected override void OnDetachedFromWindow()
					{{
						base.OnDetachedFromWindow();
#if {implementsIFrameworkElement} //Is IFrameworkElement
						OnUnloaded();
#endif
					if(base.Parent == null)
					{{
						__Store.Parent = null;
					}}

						BinderDetachedFromWindow();
					}}
#endif // IsUnoViewGroup
#endif // OverridesAttachedToWindow

					private void BinderAttachedToWindow()
					{{
						OnAttachedToWindowPartial();
					}}


					private void BinderDetachedFromWindow()
					{{
						OnDetachedFromWindowPartial();
					}}

					/// <summary>
					/// A method called when the control is attached to the Window (equivalent of Loaded)
					/// </summary>
					partial void OnAttachedToWindowPartial();

					/// <summary>
					/// A method called when the control is attached to the Window (equivalent of Unloaded)
					/// </summary>
					partial void OnDetachedFromWindowPartial();
				");
			}
		}

		private static void WriteAttachToWindow(DependencyObjectTypeData data, IndentedStringBuilder builder)
		{
			var hasOverridesAttachedToWindowiOS = data.HasFlag(DependencyObjectTypeFlags.IsIosView)
				&& data.HasFlag(DependencyObjectTypeFlags.HasNoUserMovedToWindow);

			if (hasOverridesAttachedToWindowiOS)
			{
				builder.AppendMultiLineIndented($@"
public override void MovedToWindow()
{{
	base.MovedToWindow();

	if(Window != null)
	{{
		OnAttachedToWindowPartial();
	}}
	else
	{{
		OnDetachedFromWindowPartial();
	}}
}}

/// <summary>
/// A method called when the control is attached to the Window (equivalent of Loaded)
/// </summary>
partial void OnAttachedToWindowPartial();

/// <summary>
/// A method called when the control is attached to the Window (equivalent of Unloaded)
/// </summary>
partial void OnDetachedFromWindowPartial();
					");
			}
			else
			{
				builder.AppendIndented($@"// hasOverridesAttachedToWindowiOS=false");
			}
		}

		private static void WriteViewDidMoveToWindow(DependencyObjectTypeData data, IndentedStringBuilder builder)
		{
			var hasOverridesAttachedToWindowiOS = data.HasFlag(DependencyObjectTypeFlags.IsMacosView)
				&& data.HasFlag(DependencyObjectTypeFlags.HasNoUserViewDidMoveToWindow);

			if (hasOverridesAttachedToWindowiOS)
			{
				builder.AppendMultiLineIndented($@"
public override void ViewDidMoveToWindow()
{{
	base.ViewDidMoveToWindow();

	if(Window != null)
	{{
		OnAttachedToWindowPartial();
	}}
	else
	{{
		OnDetachedFromWindowPartial();
	}}
}}

/// <summary>
/// A method called when the control is attached to the Window (equivalent of Loaded)
/// </summary>
partial void OnAttachedToWindowPartial();

/// <summary>
/// A method called when the control is attached to the Window (equivalent of Unloaded)
/// </summary>
partial void OnDetachedFromWindowPartial();
					");
			}
			else
			{
				builder.AppendIndented($@"// hasOverridesAttachedToWindowiOS=false");
			}
		}

		private static void WriteAndroidBinderDetails(DependencyObjectTypeData data, IndentedStringBuilder builder)
		{
			if (data.HasFlag(DependencyObjectTypeFlags.IsAndroidView))
			{
				builder.AppendMultiLineIndented($@"
public BinderDetails GetBinderDetail()
{{
	return null;
}}
					");
			}
		}

		private static void WriteInitializer(IndentedStringBuilder builder)
		{
			var content = $@"
private readonly static IEventProvider _binderTrace = Tracing.Get(DependencyObjectStore.TraceProvider.Id);
private BinderReferenceHolder _refHolder;

public event global::Windows.Foundation.TypedEventHandler<FrameworkElement, DataContextChangedEventArgs> DataContextChanged;

partial void InitializeBinder();

private void __InitializeBinder()
{{
	if(BinderReferenceHolder.IsEnabled)
	{{
		_refHolder = new BinderReferenceHolder(this.GetType(), this);
	}}
}}

/// <summary>
/// Obsolete method kept for binary compatibility
/// </summary>
[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
public void ClearBindings()
{{
	__Store.ClearBindings();
}}

/// <summary>
/// Obsolete method kept for binary compatibility
/// </summary>
[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
public void RestoreBindings()
{{
	__Store.RestoreBindings();
}}

private global::Uno.UI.DataBinding.ManagedWeakReference _selfWeakReference;
global::Uno.UI.DataBinding.ManagedWeakReference IWeakReferenceProvider.WeakReference
{{
	get
	{{
		if(_selfWeakReference == null)
		{{
			_selfWeakReference = global::Uno.UI.DataBinding.WeakReferencePool.RentSelfWeakReference(this);
		}}

		return _selfWeakReference;
	}}
}}
				";

			builder.AppendMultiLineIndented(content);
		}

		private static void WriteDispose(DependencyObjectTypeData data, IndentedStringBuilder builder)
		{
			var hasDispose = data.HasFlag(DependencyObjectTypeFlags.IsIosView) || data.HasFlag(DependencyObjectTypeFlags.IsMacosView);

			if (hasDispose)
			{
				builder.AppendMultiLineIndented($@"
#if __APPLE_UIKIT__ || __IOS__ || __TVOS__
					private bool _isDisposed;

					[SuppressMessage(
						""Microsoft.Usage"",
						""CA2215:DisposeMethodsShouldCallBaseClassDispose"",
						Justification = ""The dispose is re-scheduled in order to properly remove children from their parent"")]
					protected sealed override void Dispose(bool disposing)
					{{
						// This method is present in order to ensure for faster collection of a disposed visual tree
						// as well as ensure that instances can be properly returned to the FrameworkTemplatePool.
						//
						// This method can be called by the finalizer, in which case the object is re-registered
						// for finalization, and the Dispose method is invoked explicitly during a idle dispatch.
						//
						// This operation can fail if Dispose() is called by user code on UIView instances.
						// The Roslyn analyzer UnoDoNotDisposeNativeViews will warn the developer if this is the case.
						//
						// For native exceptions that may be raised if Disposed is called incorrectly, see
						// https://github.com/xamarin/xamarin-macios/issues/19493.

						if(_isDisposed)
						{{
							base.Dispose(disposing);
							return;
						}}

						if (disposing)
						{{
							// __Store may be null if the control has been recreated from
							// a native representation via the IntPtr ctor, particularly on iOS.
							__Store?.Dispose();

#if __APPLE_UIKIT__ || __IOS__ || __TVOS__
							var subviews = Subviews;

							if (subviews.Length > 0)
							{{
								BinderCollector.RequestCollect();
								foreach (var v in subviews)
								{{
									v.RemoveFromSuperview();
								}}
							}}
#endif

							base.Dispose(disposing);

							_isDisposed = true;
						}}
						else
						{{
							GC.ReRegisterForFinalize(this);

							_ = Dispatcher.RunIdleAsync(_ => Dispose());
						}}
					}}
#endif
				");
			}
		}

		private static void WriteBinderImplementation(DependencyObjectTypeData data, IndentedStringBuilder builder)
		{
			var virtualModifier = data.HasFlag(DependencyObjectTypeFlags.IsSealed) ? "" : "virtual";
			var protectedModifier = data.HasFlag(DependencyObjectTypeFlags.IsSealed) ? "private" : "internal protected";
			var legacyNonBrowsable = "[EditorBrowsable(EditorBrowsableState.Never)]";

			string dataContextChangedInvokeArgument;
			if (data.HasFlag(DependencyObjectTypeFlags.IsFrameworkElement))
			{
				// We can pass 'this' safely to a parameter of type FrameworkElement.
				dataContextChangedInvokeArgument = "this";
			}
			else if (data.HasFlag(DependencyObjectTypeFlags.FrameworkElementInheritsFromThis))
			{
				// Example: Border -> FrameworkElement -> BindableView
				// If we have a BindableView, it may or may not be FrameworkElement.
				dataContextChangedInvokeArgument = "this as FrameworkElement";
			}
			else
			{
				// This can't be a FrameworkElement. Just pass null.
				// Passing `this as FrameworkElement` will produce a compile-time error.
				// error CS0039: Cannot convert type '{0}' to '{1}' via a reference conversion, boxing conversion, unboxing conversion, wrapping conversion, or null type conversion
				dataContextChangedInvokeArgument = "null";
			}

			builder.AppendMultiLineIndented($@"

#region DataContext DependencyProperty

public object DataContext
{{
	get => GetValue(DataContextProperty);
	set => SetValue(DataContextProperty, value);
}}

// Using a DependencyProperty as the backing store for DataContext.  This enables animation, styling, binding, etc...
[global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(""Trimming"", ""IL2111"")]
public static DependencyProperty DataContextProperty {{ get ; }} =
	DependencyProperty.Register(
		name: nameof(DataContext),
		propertyType: typeof(object),
		ownerType: typeof({data.Name}),
		typeMetadata: new FrameworkPropertyMetadata(
			defaultValue: null,
			options: FrameworkPropertyMetadataOptions.Inherits,
			propertyChangedCallback: (s, e) => (({data.Name})s).OnDataContextChanged(e)
		)
);

{protectedModifier} {virtualModifier} void OnDataContextChanged(DependencyPropertyChangedEventArgs e)
{{
	OnDataContextChangedPartial(e);
	DataContextChanged?.Invoke({dataContextChangedInvokeArgument}, new DataContextChangedEventArgs(DataContext));
}}

#endregion

#region TemplatedParent DependencyProperty // legacy api, should no longer to be used.

{legacyNonBrowsable}public DependencyObject TemplatedParent
{{
	get => (DependencyObject)GetValue(TemplatedParentProperty);
	set => SetValue(TemplatedParentProperty, value);
}}

// Using a DependencyProperty as the backing store for TemplatedParent.  This enables animation, styling, binding, etc...
{legacyNonBrowsable}
[global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(""Trimming"", ""IL2111"")]
public static DependencyProperty TemplatedParentProperty {{ get ; }} =
	DependencyProperty.Register(
		name: nameof(TemplatedParent),
		propertyType: typeof(DependencyObject),
		ownerType: typeof({data.Name}),
		typeMetadata: new FrameworkPropertyMetadata(
			defaultValue: null,
			options: /*FrameworkPropertyMetadataOptions.Inherits | */FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext | FrameworkPropertyMetadataOptions.WeakStorage,
			propertyChangedCallback: (s, e) => (({data.Name})s).OnTemplatedParentChanged(e)
		)
	);


{legacyNonBrowsable}
{protectedModifier} {virtualModifier} void OnTemplatedParentChanged(DependencyPropertyChangedEventArgs e)
{{
	OnTemplatedParentChangedPartial(e);
}}

#endregion

public void SetBinding(object target, string dependencyProperty, global::Microsoft.UI.Xaml.Data.BindingBase binding)
{{
	__Store.SetBinding(target, dependencyProperty, binding);
}}

public void SetBinding(string dependencyProperty, global::Microsoft.UI.Xaml.Data.BindingBase binding)
{{
	__Store.SetBinding(dependencyProperty, binding);
}}

public void SetBinding(DependencyProperty dependencyProperty, global::Microsoft.UI.Xaml.Data.BindingBase binding)
{{
	__Store.SetBinding(dependencyProperty, binding);
}}

public void SetBindingValue(object value, [CallerMemberName] string propertyName = null)
{{
	__Store.SetBindingValue(value, propertyName);
}}

[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
internal bool IsAutoPropertyInheritanceEnabled {{ get => __Store.IsAutoPropertyInheritanceEnabled; set => __Store.IsAutoPropertyInheritanceEnabled = value; }}

partial void OnDataContextChangedPartial(DependencyPropertyChangedEventArgs e);

{legacyNonBrowsable}
partial void OnTemplatedParentChangedPartial(DependencyPropertyChangedEventArgs e);

public global::Microsoft.UI.Xaml.Data.BindingExpression GetBindingExpression(DependencyProperty dependencyProperty)
	=>  __Store.GetBindingExpression(dependencyProperty);

public void ResumeBindings()
	=>__Store.ResumeBindings();

public void SuspendBindings() =>
	__Store.SuspendBindings();
				");
		}

		private static void WriteAndroidEqualityOverride(DependencyObjectTypeData data, IndentedStringBuilder builder)
		{
			var hasEqualityOverride = data.HasFlag(DependencyObjectTypeFlags.CanOverrideEquals);

			if (hasEqualityOverride && data.HasFlag(DependencyObjectTypeFlags.IsAndroidView))
			{
				builder.AppendMultiLineIndented($@"
public override int GetHashCode()
{{
	// For the the current kind of type, we do not need to call back
	// to android for the GetHashCode implementation. The .NET proxy hash is
	// enough. This way, we do not get to pay the price of the interop to get
	// this value.
	return RuntimeHelpers.GetHashCode(this);
}}

public override bool Equals(object other)
{{
	// For the the current kind of type, we do not need to call back
	// to android for the Equals implementation. We assume that proxies are
	// one-to-one mapping with native instances, making the reference comparison
	// of proxies enough to do the job.
	return RuntimeHelpers.ReferenceEquals(this, other);
}}
					");
			}
		}

		private static void GenerateDependencyObjectImplementation(
			DependencyObjectTypeData data,
			IndentedStringBuilder builder,
			bool hasDispatcherQueue,
			bool isUnoSolution)
		{
			builder.AppendLineIndented(@"private DependencyObjectStore __storeBackingField;");
			builder.AppendLineIndented(@"public global::Windows.UI.Core.CoreDispatcher Dispatcher => global::Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher;");

			if (hasDispatcherQueue)
			{
				builder.AppendLineIndented(@"public global::Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue { get; } = global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();");
			}

			using (builder.BlockInvariant($"private DependencyObjectStore __Store"))
			{
				using (builder.BlockInvariant($"get"))
				{
					using (builder.BlockInvariant($"if(__storeBackingField == null)"))
					{
						builder.AppendLineIndented("__storeBackingField = new DependencyObjectStore(this, DataContextProperty);");
						builder.AppendLineIndented("__InitializeBinder();");
					}

					builder.AppendLineIndented("return __storeBackingField;");
				}
			}
			builder.AppendLineIndented(@"public bool IsStoreInitialized => __storeBackingField != null;");

			builder.AppendLineIndented(@"DependencyObjectStore IDependencyObjectStoreProvider.Store => __Store;");

			builder.AppendLineIndented("public object GetValue(DependencyProperty dp) => __Store.GetValue(dp);");

			builder.AppendLineIndented("public void SetValue(DependencyProperty dp, object value) => __Store.SetValue(dp, value);");

			builder.AppendLineIndented("public void ClearValue(DependencyProperty dp) => __Store.ClearValue(dp);");

			builder.AppendLineIndented("public object ReadLocalValue(DependencyProperty dp) => __Store.ReadLocalValue(dp);");

			builder.AppendLineIndented("public object GetAnimationBaseValue(DependencyProperty dp) => __Store.GetAnimationBaseValue(dp);");

			builder.AppendLineIndented("public long RegisterPropertyChangedCallback(DependencyProperty dp, DependencyPropertyChangedCallback callback) => __Store.RegisterPropertyChangedCallback(dp, callback);");

			builder.AppendLineIndented("public void UnregisterPropertyChangedCallback(DependencyProperty dp, long token) => __Store.UnregisterPropertyChangedCallback(dp, token);");

			if (isUnoSolution && !data.HasFlag(DependencyObjectTypeFlags.IsSealed))
			{
				builder.AppendLineIndented("void IDependencyObjectInternal.OnPropertyChanged2(global::Microsoft.UI.Xaml.DependencyPropertyChangedEventArgs args) => OnPropertyChanged2(args);");

				if (data.HasFlag(DependencyObjectTypeFlags.NeedsOnPropertyChanged2Stub))
				{
					builder.AppendLineIndented("internal virtual void OnPropertyChanged2(global::Microsoft.UI.Xaml.DependencyPropertyChangedEventArgs args) { }");
				}
			}
		}
	}

	[Flags]
	internal enum DependencyObjectTypeFlags
	{
		None = 0,
		IsSealed = 1 << 0,
		IsIosView = 1 << 1,
		IsMacosView = 1 << 2,
		IsAndroidView = 1 << 3,
		IsAndroidActivity = 1 << 4,
		IsAndroidFragment = 1 << 5,
		IsUnoViewGroup = 1 << 6,
		IsFrameworkElement = 1 << 7,
		FrameworkElementInheritsFromThis = 1 << 8,
		ImplementsIFrameworkElement = 1 << 9,
		NeedsBindableAttribute = 1 << 10,
		HasNoUserToString = 1 << 11,
		HasNoUserWillMoveToSuperview = 1 << 12,
		HasNoUserViewWillMoveToSuperview = 1 << 13,
		HasNoUserOnAttachedToWindow = 1 << 14,
		HasNoUserMovedToWindow = 1 << 15,
		HasNoUserViewDidMoveToWindow = 1 << 16,
		CanOverrideEquals = 1 << 17,
		NeedsOnPropertyChanged2Stub = 1 << 18,
	}

	internal sealed record DependencyObjectDiagnosticInfo(string BaseTypeName, Location Location);

	internal sealed record DependencyObjectTypeData(
		string Name,
		string HintName,
		string? Namespace,
		EquatableArray<string> ContainingTypeHeaders,
		string SelfClassHeader,
		DependencyObjectTypeFlags Flags,
		DependencyObjectDiagnosticInfo? DiagnosticInfo)
	{
		public bool HasFlag(DependencyObjectTypeFlags flag) => (Flags & flag) != 0;
	}

	internal sealed record DependencyObjectCompilationContext(bool IsValidPlatform, bool HasDispatcherQueueOnDependencyObject)
	{
		public static DependencyObjectCompilationContext FromCompilation(Compilation compilation)
		{
			var outputKind = compilation.Options.OutputKind;
			var isValidPlatform = outputKind != OutputKind.WindowsRuntimeApplication
				&& outputKind != OutputKind.WindowsRuntimeMetadata;

			var dependencyObjectSymbol = compilation.GetTypeByMetadataName(XamlConstants.Types.DependencyObject);
			var hasDispatcherQueue = dependencyObjectSymbol?.GetMembers("DispatcherQueue").Any() ?? false;

			return new DependencyObjectCompilationContext(isValidPlatform, hasDispatcherQueue);
		}
	}

	internal sealed record DependencyObjectConfig(bool IsUnoSolution, EquatableArray<string> AnalyzerSuppressions)
	{
		private static readonly char[] _commaSeparator = new[] { ',' };

		public static DependencyObjectConfig FromOptions(AnalyzerConfigOptionsProvider provider)
		{
			var globalOptions = provider.GlobalOptions;

			globalOptions.TryGetValue("build_property._IsUnoUISolution", out var isUnoUI);
			var isUnoSolution = string.Equals(isUnoUI, "true", StringComparison.OrdinalIgnoreCase);

			globalOptions.TryGetValue("build_property.XamlGeneratorAnalyzerSuppressionsProperty", out var suppressionsRaw);
			var suppressions = (suppressionsRaw ?? string.Empty).Split(_commaSeparator, StringSplitOptions.RemoveEmptyEntries);

			return new DependencyObjectConfig(isUnoSolution, new EquatableArray<string>(suppressions.ToImmutableArray()));
		}
	}

	internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
		where T : IEquatable<T>
	{
		private readonly ImmutableArray<T> _array;

		public EquatableArray(ImmutableArray<T> array)
		{
			_array = array;
		}

		public int Length => _array.IsDefault ? 0 : _array.Length;

		public T this[int index] => _array[index];

		public bool Equals(EquatableArray<T> other)
		{
			if (_array.IsDefault)
			{
				return other._array.IsDefault;
			}
			if (other._array.IsDefault)
			{
				return false;
			}
			return _array.AsSpan().SequenceEqual(other._array.AsSpan());
		}

		public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

		public override int GetHashCode()
		{
			if (_array.IsDefault)
			{
				return 0;
			}
			var hash = 17;
			foreach (var item in _array)
			{
				hash = unchecked(hash * 31 + (item?.GetHashCode() ?? 0));
			}
			return hash;
		}

		public T[] ToArray() => _array.IsDefault ? Array.Empty<T>() : _array.ToArray();

		public IEnumerator<T> GetEnumerator()
			=> _array.IsDefault
				? Enumerable.Empty<T>().GetEnumerator()
				: ((IEnumerable<T>)_array).GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

		public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);
	}
}
