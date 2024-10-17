using System;
using System.Runtime.CompilerServices;
using Uno.Extensions;
using Uno.Foundation.Logging;

namespace Windows.UI.Xaml
{
	internal class DependencyPropertyDescriptor
	{
		private static readonly bool CanUseTypeGetType =
#if __WASM__
			// Workaround for https://github.com/dotnet/runtime/issues/45078
			Environment.GetEnvironmentVariable("UNO_BOOTSTRAP_MONO_RUNTIME_MODE") == "Interpreter";
#else
			true;
#endif
		public DependencyPropertyDescriptor(Type ownerType, string name)
		{
			OwnerType = ownerType;
			Name = name;
		}

		public string Name { get; }

		public Type OwnerType { get; }

		private static readonly char[] _parenthesesChars = new[] { '(', ')' };

		/// <summary>
		/// Parses an attached PropertyPath in the form of "(clrnamespace:Type.Property)"
		/// </summary>
		/// <param name="propertyPath">The property path</param>
		/// <returns></returns>
		internal static DependencyPropertyDescriptor Parse(string propertyPath)
		{
			if (propertyPath.Contains(':'))
			{
				// (Uno.UI.Tests.BinderTests:Attachable.MyValue)

				var bindingParts = propertyPath.Trim(_parenthesesChars).Split(':');

				if (bindingParts.Length == 2)
				{
					var ns = bindingParts[0];
					var propertyParts = bindingParts[1].Split('.');

					if (propertyParts.Length == 2)
					{
						var typeName = propertyParts[0];
						var propertyname = propertyParts[1];

						var qualifiedTypeName = ns + "." + typeName;

						// Search using the current metadata provider
						var type = Uno.UI.DataBinding.BindingPropertyHelper
							.BindableMetadataProvider
							?.GetBindableTypeByFullName(qualifiedTypeName)
							?.Type;

						if (type == null)
						{
							type = SearchTypeInLoadedAssemblies(qualifiedTypeName);
						}

						if (type != null)
						{
							return new DependencyPropertyDescriptor(type, propertyname);
						}
					}
					else
					{
						if (typeof(DependencyPropertyDescriptor).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
						{
							typeof(DependencyPropertyDescriptor).Log().DebugFormat($"The property path [{propertyPath}] is not formatted properly (must only access one property)");
						}
					}
				}
				else
				{
					if (typeof(DependencyPropertyDescriptor).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
					{
						typeof(DependencyPropertyDescriptor).Log().DebugFormat($"The property path [{propertyPath}] is not formatted properly (must have exactly one ':')");
					}
				}
			}

			return null;
		}


		/// <remarks>
		/// This method is split to avoid Type.GetType causing fallbacks
		/// on the Wasm interpreter.
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Type SearchTypeInLoadedAssemblies(string qualifiedTypeName)
		{
			// If not available, search through Reflection
			var type = CanUseTypeGetType ? Type.GetType(qualifiedTypeName) : null;

			if (type == null)
			{
				foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
				{
					type = asm.GetType(qualifiedTypeName);
					if (type != null)
					{
						break;
					}
				}
			}

			return type;
		}
	}
}
