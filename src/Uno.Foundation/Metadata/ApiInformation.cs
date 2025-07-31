#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Uno.Foundation.Logging;

namespace Windows.Foundation.Metadata;

/// <summary>
/// Enables you to detect whether a specified member, type, or API contract is present
/// so that you can safely make API calls across a variety of devices.
/// </summary>
public partial class ApiInformation
{
	const DynamicallyAccessedMemberTypes PublicMembers = DynamicallyAccessedMemberTypes.PublicEvents |
		DynamicallyAccessedMemberTypes.PublicFields |
		DynamicallyAccessedMemberTypes.PublicMethods |
		DynamicallyAccessedMemberTypes.PublicProperties;

	private static HashSet<string> _notImplementedOnce = new HashSet<string>();
	private static readonly object _gate = new object();
	private static Dictionary<string, bool> _isTypePresent = new Dictionary<string, bool>();

	private readonly static Dictionary<string, Type> _typeCache = new Dictionary<string, Type>();
	private readonly static List<Assembly> _assemblies = new List<Assembly>(3 /* All three uno assemblies */) {
		typeof(ApiInformation).Assembly
	};

	/// <summary>
	/// Registers an assembly as part of the Is*Present methods
	/// </summary>
	/// <param name="assembly"></param>
	internal static void RegisterAssembly(Assembly assembly)
	{
		lock (_assemblies)
		{
			if (!_assemblies.Contains(assembly))
			{
				_assemblies.Add(assembly);
			}
		}
	}

	private static bool IsImplementedByUno(MemberInfo? member) => (member?.GetCustomAttributes(typeof(Uno.NotImplementedAttribute), false)?.Length ?? -1) == 0;

	public static bool IsTypePresent(
			[DynamicallyAccessedMembers(PublicMembers)]
			string typeName)
	{
		lock (_gate)
		{
			if (!_isTypePresent.TryGetValue(typeName, out var result))
			{
				_isTypePresent[typeName] = result = IsImplementedByUno(GetValidType(typeName));
			}

			return result;
		}
	}

	[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "GetMethod may return null, normal flow of operation")]
	internal static bool IsMethodPresent(Type type, string methodName)
		=> IsImplementedByUno(type?.GetMethod(methodName));

	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "GetField may return null, normal flow of operation")]
	public static bool IsMethodPresent(
			[DynamicallyAccessedMembers(PublicMembers)]
			string typeName,
			string methodName)
		=> IsImplementedByUno(
			GetValidType(typeName)
			?.GetMethod(methodName));

	public static bool IsMethodPresent(
			[DynamicallyAccessedMembers(PublicMembers)]
			string typeName,
			string methodName,
			uint inputParameterCount)
		=> IsImplementedByUno(
			GetValidType(typeName)
			?.GetMethods()
			?.FirstOrDefault(m => m.Name == methodName && m.GetParameters().Length == inputParameterCount));

	internal static bool IsEventPresent(
			[DynamicallyAccessedMembers(PublicMembers)]
			Type type,
			string methodName)
		=> IsImplementedByUno(type?.GetEvent(methodName));

	public static bool IsEventPresent(
			[DynamicallyAccessedMembers(PublicMembers)]
			string typeName,
			string eventName)
		=> IsImplementedByUno(
			GetValidType(typeName)
			?.GetEvent(eventName));

	[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "GetField may return null, normal flow of operation")]
	internal static bool IsPropertyPresent(Type type, string methodName)
		=> IsImplementedByUno(type?.GetProperty(methodName));

	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "GetProperty may return null, normal flow of operation")]
	public static bool IsPropertyPresent(
			[DynamicallyAccessedMembers(PublicMembers)]
			string typeName,
			string propertyName)
		=> IsImplementedByUno(
			GetValidType(typeName)
			?.GetProperty(propertyName));

	public static bool IsReadOnlyPropertyPresent(
			[DynamicallyAccessedMembers(PublicMembers)]
			string typeName,
			string propertyName)
	{
		var property = GetValidType(typeName)
			?.GetProperty(propertyName);

		if (IsImplementedByUno(property))
		{
			return property?.GetMethod != null && property.SetMethod == null;
		}

		return false;
	}

	public static bool IsWriteablePropertyPresent(
			[DynamicallyAccessedMembers(PublicMembers)]
			string typeName,
			string propertyName)
	{
		var property = GetValidType(typeName)
			?.GetProperty(propertyName);

		if (IsImplementedByUno(property))
		{
			return property?.GetMethod != null && property.SetMethod != null;
		}

		return false;
	}

	[UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "GetField may return null, normal flow of operation")]
	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "GetField may return null, normal flow of operation")]
	public static bool IsEnumNamedValuePresent(
		[DynamicallyAccessedMembers(PublicMembers)] string enumTypeName,
		string valueName)
		=> GetValidType(enumTypeName)?.GetField(valueName) != null;

	/// <summary>
	/// Determines if runtime use of not implemented members raises an exception, or logs an error message.
	/// </summary>
	public static bool IsFailWhenNotImplemented { get; set; }

	/// <summary>
	/// Determines if runtime use of not implemented members is logged only once, or at each use.
	/// </summary>
	public static bool AlwaysLogNotImplementedMessages { get; set; }

	/// <summary>
	/// The message log level used when a not implemented member is used at runtime, if <see cref="IsFailWhenNotImplemented"/> is false.
	/// </summary>
	public static LogLevel NotImplementedLogLevel { get; set; } = LogLevel.Debug;

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Types may be removed or not present as part of the normal operations of that method")]
	[return: DynamicallyAccessedMembers(PublicMembers)]
	private static Type? GetValidType(
			[DynamicallyAccessedMembers(PublicMembers)]
			string typeName)
	{
		lock (_assemblies)
		{
			if (_typeCache.TryGetValue(typeName, out var type))
			{
				return type;
			}

			var parts = typeName.Split(',');
			if (parts.Length == 2)
			{
				var fullyQualifierName = parts[0].Trim();
				var assemblyName = parts[1].Trim();
				var assembly = _assemblies.FirstOrDefault(a => a.FullName?.Split(',')[0] == assemblyName);

				type = AssemblyGetType(assembly, fullyQualifierName);

				if (type != null)
				{
					_typeCache[typeName] = type;

					return type;
				}
			}
			else
			{
				foreach (var assembly in _assemblies)
				{
					type = AssemblyGetType(assembly, typeName);

					if (type != null)
					{
						_typeCache[typeName] = type;

						return type;
					}
				}
			}

			[UnconditionalSuppressMessage("Trimming", "IL2073", Justification = "Assume that if Assembly.GetType() returns an assembly, it is un-trimmed, and thus has everything.")]
			[return: DynamicallyAccessedMembers(PublicMembers)]
			Type? AssemblyGetType(Assembly? assembly, string type)
			{
				return assembly?.GetType(type);
			}

			return null;
		}
	}

	internal static void TryRaiseNotImplemented(string type, string memberName, LogLevel errorLogLevelOverride = LogLevel.Error)
	{
		var message = $"The member {memberName} is not implemented. For more information, visit https://aka.platform.uno/notimplemented#m={Uri.EscapeDataString(type + "." + memberName)}";

		if (IsFailWhenNotImplemented)
		{
			throw new NotImplementedException(message);
		}
		else
		{
			lock (_notImplementedOnce)
			{
				if (!_notImplementedOnce.Contains(memberName) || AlwaysLogNotImplementedMessages)
				{
					_notImplementedOnce.Add(memberName);

					var logLevel = NotImplementedLogLevel == LogLevel.Error ? errorLogLevelOverride : NotImplementedLogLevel;

					LogExtensionPoint.Factory.CreateLogger(type).Log(logLevel, message);
				}
			}
		}
	}
}
