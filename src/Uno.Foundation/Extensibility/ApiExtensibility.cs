#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace Uno.Foundation.Extensibility;

/// <summary>
/// Registry for API existensibility providers, used to provide optional
/// implementations for compatible parts of WinUI and WinRT APIs.
/// </summary>
public static class ApiExtensibility
{
	private static readonly object _gate = new object();
	private static readonly Dictionary<Type, Func<object, object>> _registrations = new Dictionary<Type, Func<object, object>>();

	/// <summary>
	/// Registers an extension instance builder for the specified type
	/// </summary>
	/// <param name="type">The type to register</param>
	/// <param name="builder">A builder that will be provided an optional owner, and returns an instance of the extension</param>
	/// <remarks>
	/// This method is generally called automatically when the <see cref="ApiExtensionAttribute"/> has been defined in an assembly.
	/// <para>
	/// Registration is idempotent: if <paramref name="type"/> is already registered the first registration
	/// is kept and this call is a no-op. This supports scenarios where multiple applications run in the same
	/// process and share this registry — for example a host and a secondary application loaded into a
	/// collectible <see cref="System.Runtime.Loader.AssemblyLoadContext"/> that shares Uno.Foundation with
	/// the host. Each application's generated startup registers the same framework providers via
	/// <see cref="ApiExtensionAttribute"/>; keeping the first avoids a fatal duplicate-key
	/// <see cref="ArgumentException"/> when the second application initializes.
	/// </para>
	/// </remarks>
	public static void Register(Type type, Func<object, object> builder)
	{
		type = type ?? throw new ArgumentNullException(nameof(type));
		builder = builder ?? throw new ArgumentNullException(nameof(type));

		lock (_gate)
		{
			if (!_registrations.ContainsKey(type))
			{
				_registrations.Add(type, builder);
			}
		}
	}

	/// <summary>
	/// Registers an extension instance builder for the specified type with a strongly-typed owner.
	/// </summary>
	/// <typeparam name="TOwner">Type of owner.</typeparam>
	/// <param name="type">The type to register</param>
	/// <param name="builder">A builder that will be provided an optional owner, and returns an instance of the extension</param>
	/// <remarks>
	/// This method is generally called automatically when the <see cref="ApiExtensionAttribute"/> has been defined in an assembly.
	/// Registration is idempotent — see <see cref="Register(Type, Func{object, object})"/> for details.
	/// </remarks>
	public static void Register<TOwner>(Type type, Func<TOwner, object> builder)
	{
		type = type ?? throw new ArgumentNullException(nameof(type));
		builder = builder ?? throw new ArgumentNullException(nameof(type));

		Func<object, object> objectBuilder = o =>
		{
			if (!(o is TOwner owner))
			{
				throw new InvalidOperationException($"Expected owner of type {typeof(TOwner).Name} to resolve instance of {type.Name}.");
			}

			return builder(owner);
		};

		lock (_gate)
		{
			if (!_registrations.ContainsKey(type))
			{
				_registrations.Add(type, objectBuilder);
			}
		}
	}

	/// <summary>
	/// Checks if an extension builder for the specified <typeparamref name="T"/> type has been registered.
	/// </summary>
	/// <typeparam name="T">A registered type</typeparam>
	/// <returns>If registered or not.</returns>
	public static bool IsRegistered<T>()
	{
		return _registrations.ContainsKey(typeof(T));
	}

	/// <summary>
	/// Creates an instance of an extension of the specified <typeparamref name="T"/> type
	/// </summary>
	/// <typeparam name="T">A registered type</typeparam>
	/// <param name="owner">An optional owner to be passed to the extension constructor</param>
	/// <param name="instance">The instance if the creation was successful</param>
	/// <returns>True if the creation succeeded, otherwise False.</returns>
	public static bool CreateInstance<T>(object owner, [NotNullWhen(true)] out T? instance)
		where T : class
	{
		lock (_gate)
		{
			if (_registrations.TryGetValue(typeof(T), out var builder))
			{
				if (builder(owner) is { } o)
				{
					instance = (T)o;
					return true;
				}
			}
		}

		instance = null;

		return false;
	}

	internal static T CreateInstance<T>(object owner)
		where T : class
	{
		if (CreateInstance<T>(owner, out var instance))
		{
			return instance;
		}
		else
		{
			throw new InvalidOperationException($"Unable to find {typeof(T)} extension");
		}
	}
}
