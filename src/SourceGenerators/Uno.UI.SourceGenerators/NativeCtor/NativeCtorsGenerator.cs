#nullable enable

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Uno.UI.SourceGenerators.Helpers;
using System.Diagnostics;
using Uno.Extensions;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Uno.Roslyn;
using Uno.UI.SourceGenerators.XamlGenerator;


#if NETFRAMEWORK
using Uno.SourceGeneration;
#endif

namespace Uno.UI.SourceGenerators.NativeCtor
{
#if !NETFRAMEWORK
	public struct NativeCtorInitializationDataCollector
	{
		public INamedTypeSymbol? iOSViewSymbol { get; }
		public INamedTypeSymbol? ObjCRuntimeNativeHandle { get; }
		public INamedTypeSymbol? MacOSViewSymbol { get; }
		public INamedTypeSymbol? AndroidViewSymbol { get; }
		public INamedTypeSymbol? IntPtrSymbol { get; }
		public INamedTypeSymbol? JniHandleOwnershipSymbol { get; }
		public INamedTypeSymbol?[] JavaCtorParams { get; }

		public NativeCtorInitializationDataCollector(Compilation compilation)
		{
			iOSViewSymbol = compilation.GetTypeByMetadataName("UIKit.UIView");
			ObjCRuntimeNativeHandle = compilation.GetTypeByMetadataName("ObjCRuntime.NativeHandle");
			MacOSViewSymbol = compilation.GetTypeByMetadataName("AppKit.NSView");
			AndroidViewSymbol = compilation.GetTypeByMetadataName("Android.Views.View");
			IntPtrSymbol = compilation.GetTypeByMetadataName("System.IntPtr");
			JniHandleOwnershipSymbol = compilation.GetTypeByMetadataName("Android.Runtime.JniHandleOwnership");
			JavaCtorParams = new[] { IntPtrSymbol, JniHandleOwnershipSymbol };
		}
	}

	public struct NativeCtorExecutionDataCollector
	{
		public NativeCtorExecutionDataCollector(GeneratorExecutionContext context)
		{
		}
	}
#endif

	[Generator]
#if NETFRAMEWORK
	[GenerateAfter("Uno.UI.SourceGenerators.XamlGenerator." + nameof(XamlGenerator.XamlCodeGenerator))]
#endif
	public class NativeCtorsGenerator : AbstractNamedTypeSymbolGenerator
#if !NETFRAMEWORK
		<NativeCtorInitializationDataCollector, NativeCtorExecutionDataCollector>
#endif
	{
#if !NETFRAMEWORK
		public override NativeCtorExecutionDataCollector GetExecutionDataCollector(GeneratorExecutionContext context) => new NativeCtorExecutionDataCollector(context);
		public override NativeCtorInitializationDataCollector GetInitializationDataCollector(Compilation compilation) => new NativeCtorInitializationDataCollector(compilation);

		public override bool IsCandidateSymbolInRoslynExecution(GeneratorExecutionContext context, INamedTypeSymbol symbol, NativeCtorExecutionDataCollector collector)
		{
			return true;
		}

		public override bool IsCandidateSymbolInRoslynInitialization(GeneratorSyntaxContext context, INamedTypeSymbol symbol, NativeCtorInitializationDataCollector collector)
		{

			var isiOSView = symbol.Is(collector.iOSViewSymbol);
			var ismacOSView = symbol.Is(collector.MacOSViewSymbol);
			var isAndroidView = symbol.Is(collector.AndroidViewSymbol);

			if (isiOSView || ismacOSView)
			{
				Func<IMethodSymbol, bool> predicate = m =>
					!m.Parameters.IsDefaultOrEmpty
					&& (
						SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, collector.IntPtrSymbol)
						|| SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, collector.ObjCRuntimeNativeHandle));

				var nativeCtor = GetNativeCtor(symbol, predicate, considerAllBaseTypes: false);

				if (nativeCtor == null && GetNativeCtor(symbol.BaseType, predicate, considerAllBaseTypes: true) != null)
				{
					return true;
				}
			}

			if (isAndroidView)
			{
				Func<IMethodSymbol, bool> predicate = m => m.Parameters.Select(p => p.Type).SequenceEqual(collector.JavaCtorParams ?? Array.Empty<ITypeSymbol?>());
				var nativeCtor = GetNativeCtor(symbol, predicate, considerAllBaseTypes: false);

				if (nativeCtor == null && GetNativeCtor(symbol.BaseType, predicate, considerAllBaseTypes: true) != null)
				{
					return true;
				}
			}

			return false;

			static IMethodSymbol? GetNativeCtor(INamedTypeSymbol? type, Func<IMethodSymbol, bool> predicate, bool considerAllBaseTypes)
			{
				// Consider:
				// Type A -> Type B -> Type C
				// HasCtor   NoCtor    NoCtor
				// We want to generate the ctor for both Type B and Type C
				// But since the generator doesn't guarantee Type B is getting processed first,
				// We need to check the inheritance hierarchy.
				// However, assume Type B wasn't declared in source, we can't generate the ctor for it.
				// Consequently, Type C shouldn't generate source as well.
				if (type is null)
				{
					return null;
				}

				var ctor = type.GetMembers(WellKnownMemberNames.InstanceConstructorName).Cast<IMethodSymbol>().FirstOrDefault(predicate);
				if (ctor != null || !considerAllBaseTypes || !type.Locations.Any(l => l.IsInSource))
				{
					return ctor;
				}
				else
				{
					return GetNativeCtor(type.BaseType, predicate, true);
				}
			}
		}
#endif

		private protected override SymbolGenerator GetGenerator(GeneratorExecutionContext context) => new SerializationMethodsGenerator(context);

		private sealed class SerializationMethodsGenerator : SymbolGenerator
		{
			private readonly INamedTypeSymbol? _iosViewSymbol;
			private readonly INamedTypeSymbol? _objcNativeHandleSymbol;
			private readonly INamedTypeSymbol? _macosViewSymbol;
			private readonly INamedTypeSymbol? _androidViewSymbol;
			private readonly INamedTypeSymbol? _intPtrSymbol;
			private readonly INamedTypeSymbol? _jniHandleOwnershipSymbol;
			private readonly INamedTypeSymbol?[]? _javaCtorParams;

			public SerializationMethodsGenerator(GeneratorExecutionContext context) : base(context)
			{
				_iosViewSymbol = context.Compilation.GetTypeByMetadataName("UIKit.UIView");
				_objcNativeHandleSymbol = context.Compilation.GetTypeByMetadataName("ObjCRuntime.NativeHandle");
				_macosViewSymbol = context.Compilation.GetTypeByMetadataName("AppKit.NSView");
				_androidViewSymbol = context.Compilation.GetTypeByMetadataName("Android.Views.View");
				_intPtrSymbol = context.Compilation.GetTypeByMetadataName("System.IntPtr");
				_jniHandleOwnershipSymbol = context.Compilation.GetTypeByMetadataName("Android.Runtime.JniHandleOwnership");
				_javaCtorParams = new[] { _intPtrSymbol, _jniHandleOwnershipSymbol };
			}

			private protected override bool IsCandidateSymbol(INamedTypeSymbol typeSymbol)
			{
				var isiOSView = typeSymbol.Is(_iosViewSymbol);
				var ismacOSView = typeSymbol.Is(_macosViewSymbol);
				var isAndroidView = typeSymbol.Is(_androidViewSymbol);

				if (isiOSView || ismacOSView)
				{
					Func<IMethodSymbol, bool> predicate = m =>
						!m.Parameters.IsDefaultOrEmpty
						&& (
							SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, _intPtrSymbol)
							|| SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, _objcNativeHandleSymbol));

					var nativeCtor = GetNativeCtor(typeSymbol, predicate, considerAllBaseTypes: false);

					if (nativeCtor == null && GetNativeCtor(typeSymbol.BaseType, predicate, considerAllBaseTypes: true) != null)
					{
						return true;
					}
				}

				if (isAndroidView)
				{
					Func<IMethodSymbol, bool> predicate = m => m.Parameters.Select(p => p.Type).SequenceEqual(_javaCtorParams ?? Array.Empty<ITypeSymbol?>());
					var nativeCtor = GetNativeCtor(typeSymbol, predicate, considerAllBaseTypes: false);

					if (nativeCtor == null && GetNativeCtor(typeSymbol.BaseType, predicate, considerAllBaseTypes: true) != null)
					{
						return true;
					}
				}

				return false;

				static IMethodSymbol? GetNativeCtor(INamedTypeSymbol? type, Func<IMethodSymbol, bool> predicate, bool considerAllBaseTypes)
				{
					// Consider:
					// Type A -> Type B -> Type C
					// HasCtor   NoCtor    NoCtor
					// We want to generate the ctor for both Type B and Type C
					// But since the generator doesn't guarantee Type B is getting processed first,
					// We need to check the inheritance hierarchy.
					// However, assume Type B wasn't declared in source, we can't generate the ctor for it.
					// Consequently, Type C shouldn't generate source as well.
					if (type is null)
					{
						return null;
					}

					var ctor = type.GetMembers(WellKnownMemberNames.InstanceConstructorName).Cast<IMethodSymbol>().FirstOrDefault(predicate);
					if (ctor != null || !considerAllBaseTypes || !type.Locations.Any(l => l.IsInSource))
					{
						return ctor;
					}
					else
					{
						return GetNativeCtor(type.BaseType, predicate, true);
					}
				}
			}


			private protected override string GetGeneratedCode(INamedTypeSymbol symbol)
			{
				var builder = new IndentedStringBuilder();
				builder.AppendLineIndented("// <auto-generated>");
				builder.AppendLineIndented("// *************************************************************");
				builder.AppendLineIndented("// This file has been generated by Uno.UI (NativeCtorsGenerator)");
				builder.AppendLineIndented("// *************************************************************");
				builder.AppendLineIndented("// </auto-generated>");
				builder.AppendLine();
				builder.AppendLineIndented("using System;");
				builder.AppendLine();
				var disposables = symbol.AddToIndentedStringBuilder(builder, beforeClassHeaderAction: builder =>
				{
					// These will be generated just before `partial class ClassName {`
					builder.Append("#if __IOS__ || __MACOS__");
					builder.AppendLine();
					builder.AppendLineIndented("[global::Foundation.Register]");
					builder.Append("#endif");
					builder.AppendLine();
				});

				var syntacticValidSymbolName = SyntaxFacts.GetKeywordKind(symbol.Name) == SyntaxKind.None ? symbol.Name : "@" + symbol.Name;

				if (NeedsExplicitDefaultCtor(symbol))
				{
					builder.AppendLineIndented("/// <summary>");
					builder.AppendLineIndented("/// Initializes a new instance of the class.");
					builder.AppendLineIndented("/// </summary>");
					builder.AppendLineIndented($"public {syntacticValidSymbolName}() {{ }}");
					builder.AppendLine();
				}

				builder.Append("#if __ANDROID__");
				builder.AppendLine();
				builder.AppendLineIndented("/// <summary>");
				builder.AppendLineIndented("/// Native constructor, do not use explicitly.");
				builder.AppendLineIndented("/// </summary>");
				builder.AppendLineIndented("/// <remarks>");
				builder.AppendLineIndented("/// Used by the Xamarin Runtime to materialize native ");
				builder.AppendLineIndented("/// objects that may have been collected in the managed world.");
				builder.AppendLineIndented("/// </remarks>");
				builder.AppendLineIndented($"public {syntacticValidSymbolName}(IntPtr javaReference, global::Android.Runtime.JniHandleOwnership transfer) : base (javaReference, transfer) {{ }}");
				builder.Append("#endif");
				builder.AppendLine();

				builder.Append("#if __IOS__ || __MACOS__ || __MACCATALYST__");
				builder.AppendLine();
				builder.AppendLineIndented("/// <summary>");
				builder.AppendLineIndented("/// Native constructor, do not use explicitly.");
				builder.AppendLineIndented("/// </summary>");
				builder.AppendLineIndented("/// <remarks>");
				builder.AppendLineIndented("/// Used by the Xamarin Runtime to materialize native ");
				builder.AppendLineIndented("/// objects that may have been collected in the managed world.");
				builder.AppendLineIndented("/// </remarks>");
				builder.AppendLineIndented($"public {syntacticValidSymbolName}(IntPtr handle) : base (handle) {{ }}");

				if (_objcNativeHandleSymbol != null)
				{
					builder.AppendLineIndented("/// <summary>");
					builder.AppendLineIndented("/// Native constructor, do not use explicitly.");
					builder.AppendLineIndented("/// </summary>");
					builder.AppendLineIndented("/// <remarks>");
					builder.AppendLineIndented("/// Used by the .NET Runtime to materialize native ");
					builder.AppendLineIndented("/// objects that may have been collected in the managed world.");
					builder.AppendLineIndented("/// </remarks>");
					builder.AppendLineIndented($"public {syntacticValidSymbolName}(global::ObjCRuntime.NativeHandle handle) : base (handle) {{ }}");
				}

				builder.Append("#endif");
				builder.AppendLine();

				while (disposables.Count > 0)
				{
					disposables.Pop().Dispose();
				}

				return builder.ToString();

				static bool NeedsExplicitDefaultCtor(INamedTypeSymbol typeSymbol)
				{
					var hasExplicitConstructor = typeSymbol
						.GetMembers(WellKnownMemberNames.InstanceConstructorName)
						.Any(m => m is IMethodSymbol { IsImplicitlyDeclared: false, Parameters: { Length: 0 } });
					if (hasExplicitConstructor)
					{
						return false;
					}

					var baseHasDefaultCtor = typeSymbol
						.BaseType?
						.GetMembers(WellKnownMemberNames.InstanceConstructorName)
						.Any(m => m is IMethodSymbol { Parameters: { Length: 0 } }) ?? false;
					return baseHasDefaultCtor;
				}
			}
		}

	}
}
