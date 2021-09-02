#nullable enable

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Uno.UI.SourceGenerators.Helpers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

#if NETFRAMEWORK
using Uno.SourceGeneration;
#endif


namespace Uno.UI.SourceGenerators.ImplementedRoutedEvents
{
	[Generator]
	public class ImplementedRoutedEventsGenerator : ISourceGenerator
	{
		private const string GetImplementedRoutedEventsMethodName = "GetImplementedRoutedEvents";

		public void Initialize(GeneratorInitializationContext context)
		{
			// No initialization required.
		}

		public void Execute(GeneratorExecutionContext context)
		{
			if (!DesignTimeHelper.IsDesignTime(context))
			{
				var controlSymbol = context.Compilation.GetTypeByMetadataName("Windows.UI.Xaml.Controls.Control");
				if (controlSymbol is null)
				{
					return;
				}

				var getImplementedRoutedEventsSymbol = controlSymbol.GetMembers(GetImplementedRoutedEventsMethodName).SingleOrDefault(m => m.Kind == SymbolKind.Method) as IMethodSymbol;
				if (getImplementedRoutedEventsSymbol is null)
				{
					return;
				}

				if (!getImplementedRoutedEventsSymbol.ContainingAssembly.IsSameAssemblyOrHasFriendAccessTo(context.Compilation.Assembly))
				{
					return;
				}

				var visitor = new ControlTypesVisitor(context, getImplementedRoutedEventsSymbol);
				visitor.Visit(context.Compilation.SourceModule);
			}
		}

		private class ControlTypesVisitor : SymbolVisitor
		{
			private readonly GeneratorExecutionContext _context;
			private readonly IMethodSymbol _getImplementedRoutedEvents;
			private readonly HashSet<INamedTypeSymbol> _seenTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

			private readonly Dictionary<string, INamedTypeSymbol> _events = new Dictionary<string, INamedTypeSymbol>();

			public ControlTypesVisitor(GeneratorExecutionContext context, IMethodSymbol getImplementedRoutedEventsSymbol)
			{
				_context = context;
				_getImplementedRoutedEvents = getImplementedRoutedEventsSymbol;
				var pointerRoutedEventArgs = GetRequiredTypeByMetadataName("Windows.UI.Xaml.Input.PointerRoutedEventArgs", context.Compilation);
				_events.Add("OnPointerPressed", pointerRoutedEventArgs);
				_events.Add("OnPointerReleased", pointerRoutedEventArgs);
				_events.Add("OnPointerEntered", pointerRoutedEventArgs);
				_events.Add("OnPointerExited", pointerRoutedEventArgs);
				_events.Add("OnPointerMoved", pointerRoutedEventArgs);
				_events.Add("OnPointerCanceled", pointerRoutedEventArgs);
				_events.Add("OnPointerCaptureLost", pointerRoutedEventArgs);
				_events.Add("OnPointerWheelChanged", pointerRoutedEventArgs);

				var manipulationStartingRoutedEventArgs = GetRequiredTypeByMetadataName("Windows.UI.Xaml.Input.ManipulationStartingRoutedEventArgs", context.Compilation);
				_events.Add("OnManipulationStarting", manipulationStartingRoutedEventArgs);

				var manipulationStartedRoutedEventArgs = GetRequiredTypeByMetadataName("Windows.UI.Xaml.Input.ManipulationStartedRoutedEventArgs", context.Compilation);
				_events.Add("OnManipulationStarted", manipulationStartedRoutedEventArgs);

				var manipulationDeltaRoutedEventArgs = GetRequiredTypeByMetadataName("Windows.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs", context.Compilation);
				_events.Add("OnManipulationDelta", manipulationDeltaRoutedEventArgs);

				var manipulationInertiaStartingRoutedEventArgs = GetRequiredTypeByMetadataName("Windows.UI.Xaml.Input.ManipulationInertiaStartingRoutedEventArgs", context.Compilation);
				_events.Add("OnManipulationInertiaStarting", manipulationInertiaStartingRoutedEventArgs);

				var manipulationCompletedRoutedEventArgs = GetRequiredTypeByMetadataName("Windows.UI.Xaml.Input.ManipulationCompletedRoutedEventArgs", context.Compilation);
				_events.Add("OnManipulationCompleted", manipulationCompletedRoutedEventArgs);

				var tappedRoutedEventArgs = GetRequiredTypeByMetadataName("Windows.UI.Xaml.Input.TappedRoutedEventArgs", context.Compilation);
				_events.Add("OnTapped", tappedRoutedEventArgs);

				var doubleTappedRoutedEventArgs = GetRequiredTypeByMetadataName("Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs", context.Compilation);
				_events.Add("OnDoubleTapped", doubleTappedRoutedEventArgs);

				var rightTappedRoutedEventArgs = GetRequiredTypeByMetadataName("Windows.UI.Xaml.Input.RightTappedRoutedEventArgs", context.Compilation);
				_events.Add("OnRightTapped", rightTappedRoutedEventArgs);

				var holdingRoutedEventArgs = GetRequiredTypeByMetadataName("Windows.UI.Xaml.Input.HoldingRoutedEventArgs", context.Compilation);
				_events.Add("OnHolding", holdingRoutedEventArgs);

				var dragEventArgs = GetRequiredTypeByMetadataName("Windows.UI.Xaml.DragEventArgs", context.Compilation);
				_events.Add("OnDragEnter", dragEventArgs);
				_events.Add("OnDragOver", dragEventArgs);
				_events.Add("OnDragLeave", dragEventArgs);
				_events.Add("OnDrop", dragEventArgs);

				var keyRoutedEventArgs = GetRequiredTypeByMetadataName("Windows.UI.Xaml.Input.KeyRoutedEventArgs", context.Compilation);
				_events.Add("OnKeyDown", keyRoutedEventArgs);
				_events.Add("OnKeyUp", keyRoutedEventArgs);

				var routedEventArgs = GetRequiredTypeByMetadataName("Windows.UI.Xaml.RoutedEventArgs", context.Compilation);
				_events.Add("OnLostFocus", routedEventArgs);
				_events.Add("OnGotFocus", routedEventArgs);
			}

			private static INamedTypeSymbol GetRequiredTypeByMetadataName(string fullyQualifiedMetadataName, Compilation compilation)
			{
				var type = compilation.GetTypeByMetadataName(fullyQualifiedMetadataName);
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

			private void ProcessType(INamedTypeSymbol type)
			{
				// Control is defining the virtual property, we cannot generate an override for it.
				// Use of _seenTypes to prevent processing the same type twice, which causes warnings like:
				// warning CS2002: Source file 'src\Uno.UI\obj\Debug\monoandroid11.0\g\ImplementedRoutedEventsGenerator\TwoPaneView_ImplementedRoutedEvents_g_cs.g.cs' specified multiple times
				if (!type.IsAbstract && type.Is(_getImplementedRoutedEvents.ContainingType) &&
					!type.Equals(_getImplementedRoutedEvents.ContainingType, SymbolEqualityComparer.Default) &&
					_seenTypes.Add(type))
				{
					var list = new List<string>();
					list.Add("global::Uno.UI.Xaml.RoutedEventFlag.None");

					foreach (var @event in _events)
					{
						if (!@event.Key.StartsWith("On", StringComparison.Ordinal))
						{
							throw new InvalidOperationException("Expected event to start with 'On'");
						}

						if (IsEventOverrideImplemented(type, @event.Key, @event.Value))
						{
							list.Add($"global::Uno.UI.Xaml.RoutedEventFlag.{@event.Key.Substring(2)}");
						}
					}

					GenerateCode(type, list);
				}
			}

			private bool IsEventOverrideImplemented(INamedTypeSymbol type, string name, INamedTypeSymbol arg)
			{
				if (type.Equals(_getImplementedRoutedEvents.ContainingType, SymbolEqualityComparer.Default))
				{
					return false;
				}

				var method = type.GetMembers(name).SingleOrDefault(
					m => m is IMethodSymbol method && method.Parameters.Length == 1 && arg.Equals(method.Parameters[0].Type, SymbolEqualityComparer.Default)) as IMethodSymbol;
				// base type can't be null, so the suppression should be safe. We know that the initial type inherits Control class (ie, _implementedRoutedEvents.ContainingType)
				// And the recursion will end as soon as we get there.
				return method?.IsOverride == true || IsEventOverrideImplemented(type.BaseType!, name, arg);
			}

			private void GenerateCode(INamedTypeSymbol type, IEnumerable<string> routedEventFlags)
			{
				// Keep containing namespace here to avoid controls defined in both WUXC and MUXC from one overwriting the other.
				_context.AddSource($"{type.ContainingNamespace}.{type.Name}_ImplementedRoutedEvents.g.cs", $@"// <auto-generated>

namespace {type.ContainingNamespace}
{{
	partial class {type.Name}
	{{
		protected override global::Uno.UI.Xaml.RoutedEventFlag {GetImplementedRoutedEventsMethodName}()
		{{
			return {string.Join(" | ", routedEventFlags)};
		}}
	}}
}}
");
			}
		}
	}
}
