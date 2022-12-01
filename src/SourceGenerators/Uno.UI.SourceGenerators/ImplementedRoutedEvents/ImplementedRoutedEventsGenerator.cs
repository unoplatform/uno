#nullable enable

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.UI.SourceGenerators.Helpers;
using System.Collections.Generic;
using Uno.Extensions;
using Uno.UI.SourceGenerators.XamlGenerator;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

#if NETFRAMEWORK
using Uno.SourceGeneration;
#endif

namespace Uno.UI.SourceGenerators.ImplementedRoutedEvents;

[Generator]
public class ImplementedRoutedEventsGenerator : ISourceGenerator
{
	public void Initialize(GeneratorInitializationContext context)
	{
		// No initialization required.
	}

	public void Execute(GeneratorExecutionContext context)
	{
		if (!DesignTimeHelper.IsDesignTime(context) && PlatformHelper.IsValidPlatform(context))
		{
			var uiElementSymbol = context.Compilation.GetTypeByMetadataName(XamlConstants.Types.UIElement);
			if (uiElementSymbol is null)
			{
				return;
			}

			var visitor = new ImplementedRoutedEventsVisitor(context, uiElementSymbol);
			visitor.Visit(context.Compilation.SourceModule);
		}
	}

	private class ImplementedRoutedEventsVisitor : SymbolVisitor
	{
		private readonly GeneratorExecutionContext _context;

		private readonly Dictionary<string, INamedTypeSymbol> _events = new Dictionary<string, INamedTypeSymbol>();

		private readonly INamedTypeSymbol _uiElementSymbol;
		private readonly INamedTypeSymbol _controlSymbol;

		public ImplementedRoutedEventsVisitor(GeneratorExecutionContext context, INamedTypeSymbol uiElementSymbol)
		{
			_context = context;

			_uiElementSymbol = uiElementSymbol;
			_controlSymbol = GetRequiredTypeByMetadataName(XamlConstants.Types.Control);

			var pointerRoutedEventArgs = GetRequiredTypeByMetadataName(XamlConstants.Types.PointerRoutedEventArgs);
			_events.Add("OnPointerPressed", pointerRoutedEventArgs);
			_events.Add("OnPointerReleased", pointerRoutedEventArgs);
			_events.Add("OnPointerEntered", pointerRoutedEventArgs);
			_events.Add("OnPointerExited", pointerRoutedEventArgs);
			_events.Add("OnPointerMoved", pointerRoutedEventArgs);
			_events.Add("OnPointerCanceled", pointerRoutedEventArgs);
			_events.Add("OnPointerCaptureLost", pointerRoutedEventArgs);
			_events.Add("OnPointerWheelChanged", pointerRoutedEventArgs);

			var manipulationStartingRoutedEventArgs = GetRequiredTypeByMetadataName(XamlConstants.Types.ManipulationStartingRoutedEventArgs);
			_events.Add("OnManipulationStarting", manipulationStartingRoutedEventArgs);

			var manipulationStartedRoutedEventArgs = GetRequiredTypeByMetadataName(XamlConstants.Types.ManipulationStartedRoutedEventArgs);
			_events.Add("OnManipulationStarted", manipulationStartedRoutedEventArgs);

			var manipulationDeltaRoutedEventArgs = GetRequiredTypeByMetadataName(XamlConstants.Types.ManipulationDeltaRoutedEventArgs);
			_events.Add("OnManipulationDelta", manipulationDeltaRoutedEventArgs);

			var manipulationInertiaStartingRoutedEventArgs = GetRequiredTypeByMetadataName(XamlConstants.Types.ManipulationInertiaStartingRoutedEventArgs);
			_events.Add("OnManipulationInertiaStarting", manipulationInertiaStartingRoutedEventArgs);

			var manipulationCompletedRoutedEventArgs = GetRequiredTypeByMetadataName(XamlConstants.Types.ManipulationCompletedRoutedEventArgs);
			_events.Add("OnManipulationCompleted", manipulationCompletedRoutedEventArgs);

			var tappedRoutedEventArgs = GetRequiredTypeByMetadataName(XamlConstants.Types.TappedRoutedEventArgs);
			_events.Add("OnTapped", tappedRoutedEventArgs);

			var doubleTappedRoutedEventArgs = GetRequiredTypeByMetadataName(XamlConstants.Types.DoubleTappedRoutedEventArgs);
			_events.Add("OnDoubleTapped", doubleTappedRoutedEventArgs);

			var rightTappedRoutedEventArgs = GetRequiredTypeByMetadataName(XamlConstants.Types.RightTappedRoutedEventArgs);
			_events.Add("OnRightTapped", rightTappedRoutedEventArgs);

			var holdingRoutedEventArgs = GetRequiredTypeByMetadataName(XamlConstants.Types.HoldingRoutedEventArgs);
			_events.Add("OnHolding", holdingRoutedEventArgs);

			var dragEventArgs = GetRequiredTypeByMetadataName(XamlConstants.Types.DragEventArgs);
			_events.Add("OnDragEnter", dragEventArgs);
			_events.Add("OnDragOver", dragEventArgs);
			_events.Add("OnDragLeave", dragEventArgs);
			_events.Add("OnDrop", dragEventArgs);

			var keyRoutedEventArgs = GetRequiredTypeByMetadataName(XamlConstants.Types.KeyRoutedEventArgs);
			_events.Add("OnKeyDown", keyRoutedEventArgs);
			_events.Add("OnKeyUp", keyRoutedEventArgs);

			var routedEventArgs = GetRequiredTypeByMetadataName(XamlConstants.Types.RoutedEventArgs);
			_events.Add("OnLostFocus", routedEventArgs);
			_events.Add("OnGotFocus", routedEventArgs);

			var bringIntoViewRequestedEventArgs = GetRequiredTypeByMetadataName(XamlConstants.Types.BringIntoViewRequestedEventArgs);
			_events.Add("OnBringIntoViewRequested", bringIntoViewRequestedEventArgs);
		}

		private INamedTypeSymbol GetRequiredTypeByMetadataName(string fullyQualifiedMetadataName)
		{
			var type = _context.Compilation.GetTypeByMetadataName(fullyQualifiedMetadataName);
			return type ?? throw new InvalidOperationException($"The type '{fullyQualifiedMetadataName}' is not found.");
		}

		public override void VisitModule(IModuleSymbol symbol) => VisitNamespace(symbol.GlobalNamespace);

		public override void VisitNamedType(INamedTypeSymbol symbol)
		{
			foreach (var type in symbol.GetTypeMembers())
			{
				VisitNamedType(type);
			}

			ProcessType(symbol);
		}

		public override void VisitNamespace(INamespaceSymbol symbol)
		{
			foreach (var n in symbol.GetNamespaceMembers())
			{
				VisitNamespace(n);
			}

			foreach (var t in symbol.GetTypeMembers())
			{
				VisitNamedType(t);
			}
		}

		/// <summary>
		/// Checks whether the given TypeDeclarationSyntax is partial, and all parents are also partial
		/// </summary>
		private static bool IsPartialChainOfTypes(TypeDeclarationSyntax typeDeclaration)
		{
			if (typeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
			{
				// We have a partial modifier, now we check the parent.
				// If the parent isn't TypeDeclarationSyntax (e.g, CompilationUnitSyntax or NamespaceDeclarationSyntax), then
				// we are good. Otherwise, recurse.
				return typeDeclaration.Parent is not TypeDeclarationSyntax parent || IsPartialChainOfTypes(parent);
			}

			return false;
		}

		private void ProcessType(INamedTypeSymbol type)
		{
			if (!type.IsAbstract &&
				type.Is(_uiElementSymbol) &&
				type.DeclaringSyntaxReferences[0].GetSyntax() is TypeDeclarationSyntax typeDeclaration &&
				IsPartialChainOfTypes(typeDeclaration))
			{
				var builder = new StringBuilder("global::Uno.UI.Xaml.RoutedEventFlag.None");

				foreach (var @event in _events)
				{
					if (!@event.Key.StartsWith("On", StringComparison.Ordinal))
					{
						throw new InvalidOperationException("Expected event to start with 'On'");
					}

					if (IsEventOverrideImplemented(type, @event.Key, @event.Value))
					{
						builder.Append($" | global::Uno.UI.Xaml.RoutedEventFlag.{@event.Key.Substring("On".Length)}");
					}
				}

				GenerateCode(type, builder.ToString());
			}
		}

		private bool IsEventOverrideImplemented(INamedTypeSymbol type, string name, INamedTypeSymbol arg)
		{
			if (type.Equals(_uiElementSymbol, SymbolEqualityComparer.Default) ||
				type.Equals(_controlSymbol, SymbolEqualityComparer.Default))
			{
				return false;
			}

			var method = type
				.GetMembers(name)
				.OfType<IMethodSymbol>()
				.SingleOrDefault(
					method =>
						method.Parameters.Length == 1 &&
						arg.Equals(method.Parameters[0].Type, SymbolEqualityComparer.Default));

			// Base type can't be null, so the suppression should be safe.
			// We know that the initial type inherits UIElement class (ie, _implementedRoutedEvents.ContainingType)
			// And the recursion will end as soon as we get there.
			return
				method?.IsOverride == true ||
				IsEventOverrideImplemented(type.BaseType!, name, arg);
		}

		private void GenerateCode(INamedTypeSymbol type, string routedEventFlags)
		{
			var builder = new IndentedStringBuilder();
			builder.AppendLineIndented("// <auto-generated>");

			using (type.AddToIndentedStringBuilder(builder))
			{
				WriteClass(builder, type, routedEventFlags);
			}

			// Keep containing namespace here to avoid controls defined in both WUXC and MUXC from one overwriting the other.
			var generatedSource = builder.ToString();
			var generatedFileName = $"{GetFullMetadataNameForFileName(type)}_ImplementedRoutedEvents.g.cs";
			_context.AddSource(generatedFileName, generatedSource);
		}

		private static void WriteClass(IIndentedStringBuilder builder, INamedTypeSymbol type, string routedEventFlags)
		{
			builder.AppendLineIndented("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
			builder.AppendLineIndented("private static global::Uno.UI.Xaml.RoutedEventFlag __uno_ImplementedRoutedEvents = global::Uno.UI.Xaml.UIElementGeneratedProxy.RegisterImplementedRoutedEvents(");
			using (builder.Indent())
			{
				builder.AppendLineIndented($"typeof({type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}),");
				builder.AppendLineIndented(routedEventFlags);
			}

			builder.AppendLineIndented(");");
		}

		// Taken from https://github.com/CommunityToolkit/dotnet/blob/58762429501b4d7cb8c11013ca26e488fbcff592/CommunityToolkit.Mvvm.SourceGenerators/Extensions/INamedTypeSymbolExtensions.cs
		private static string GetFullMetadataNameForFileName(INamedTypeSymbol symbol)
		{
			return symbol.GetFullMetadataName().Replace('`', '-').Replace('+', '.');
		}
	}
}
