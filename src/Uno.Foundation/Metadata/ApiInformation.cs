using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Uno.Logging;

namespace Windows.Foundation.Metadata
{
	public partial class ApiInformation
	{
		private static HashSet<string> _notImplementedOnce = new HashSet<string>();
		private static readonly object _gate = new object();
		private static Dictionary<string, bool> _isTypePresent = new Dictionary<string, bool>();
		private static Dictionary<(string typeName, string methodName, uint inputParameterCount), bool> _isMethodPresent 
			= new Dictionary<(string typeName, string methodName, uint inputParameterCount), bool>();

		private readonly static string[] _assemblies = new string[] {
			"Uno.UI",
			"Uno.Foundation",
			"Uno"
		};

		private static bool IsImplementedByUno(MemberInfo member) => (member?.GetCustomAttributes(typeof(Uno.NotImplementedAttribute), false)?.Length ?? -1) == 0;

		public static bool IsTypePresent(string typeName)
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

		public static bool IsMethodPresent(string typeName, string methodName) 
			=> GetValidType(typeName)?.GetMethod(methodName) != null;

		public static bool IsMethodPresent(string typeName, string methodName, uint inputParameterCount) 
			=> GetValidType(typeName)
				?.GetMethods()
				?.Where(m => m.Name == methodName && m.GetParameters().Length == inputParameterCount)
				.Any() ?? false;

		public static bool IsEventPresent(string typeName, string eventName)
			=> IsImplementedByUno(
				GetValidType(typeName)
				?.GetEvent(eventName));

		public static bool IsPropertyPresent(string typeName, string propertyName) 
			=> IsImplementedByUno(
				GetValidType(typeName)
				?.GetProperty(propertyName));

		public static bool IsReadOnlyPropertyPresent(string typeName, string propertyName)
		{
			var property = GetValidType(typeName)
				?.GetProperty(propertyName);

			if(IsImplementedByUno(property))
			{
				return property.GetMethod != null && property.SetMethod == null;
			}

			return false;
		}

		public static bool IsWriteablePropertyPresent(string typeName, string propertyName)
		{
			var property = GetValidType(typeName)
				?.GetProperty(propertyName);

			if (IsImplementedByUno(property))
			{
				return property.GetMethod != null && property.SetMethod != null;
			}

			return false;
		}

		public static bool IsEnumNamedValuePresent(string enumTypeName, string valueName) 
			=> GetValidType(enumTypeName)?.GetField(valueName) != null;

		/// <summary>
		/// Determines if runtime use of not implemented members raises an exception, or logs an error message.
		/// </summary>
		public static bool IsFailWhenNotImplemented { get; set; }

		/// <summary>
		/// Determines if runtime use of not implemented members is logged only once, or at each use.
		/// </summary>
		public static bool AlwaysLogNotImplementedMessages { get; set; }

		private static Type GetValidType(string typeName)
		{
			foreach (var assembly in _assemblies)
			{
				var type = Type.GetType(typeName + ", " + assembly);

				if (type != null)
				{
					return type;
				}
			}

			return null;
		}

		internal static void TryRaiseNotImplemented(string type, string memberName)
		{
			var message = $"The member {memberName} is not implemented in Uno.";

			if (IsFailWhenNotImplemented)
			{
				throw new NotImplementedException(message);
			}
			else
			{
				lock(_notImplementedOnce)
				{
					if(!_notImplementedOnce.Contains(memberName) || AlwaysLogNotImplementedMessages)
					{
						_notImplementedOnce.Add(memberName);

                        Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory.CreateLogger(type).Error(message);
					}
				}
			}
		}
	}
}
