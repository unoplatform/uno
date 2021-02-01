using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Uno.Disposables;
using Uno.Extensions;

namespace Uno.Foundation.Interop
{
	/// <summary>
	/// Provider of <see cref="IJSObjectMetadata"/>
	/// </summary>
	internal static class JSObjectMetadataProvider
	{
		private static readonly Func<Type, IJSObjectMetadata> _getByReflection = t => new ReflectionMetadata(t);
		static JSObjectMetadataProvider() => _getByReflection = _getByReflection.AsMemoized();

		/// <summary>
		/// Get the <see cref="IJSObjectMetadata"/> for the given type
		/// </summary>
		public static IJSObjectMetadata Get(Type type)
		{
			return _getByReflection(type);
		}

		private class ReflectionMetadata : IJSObjectMetadata
		{
			private readonly Type _type;
			private static long _handles = 0L;

			private bool _isPrototypeExported;
			private Dictionary<string, MethodInfo> _methods;

			public ReflectionMetadata(Type type)
			{
				_type = type;
			}

			/// <inheritdoc />
			public long CreateNativeInstance(IntPtr managedHandle)
			{
				if (!_isPrototypeExported)
				{
					var prototype = BuildPrototype();

					// Makes type visible to javascript
					WebAssemblyRuntime.InvokeJS(prototype);
					_isPrototypeExported = true;
				}

				var id = Interlocked.Increment(ref _handles);
				WebAssemblyRuntime.InvokeJS($"{_type.FullName}.createInstance(\"{managedHandle}\", \"{id}\")");

				return id;
			}

			/// <inheritdoc />
			public string GetNativeInstance(IntPtr managedHandle, long jsHandle) 
				=> $"{_type.FullName}.getInstance(\"{managedHandle}\", \"{jsHandle}\")";

			/// <inheritdoc />
			public void DestroyNativeInstance(IntPtr managedHandle, long jsHandle) 
				=> WebAssemblyRuntime.InvokeJS($"{_type.FullName}.destroyInstance(\"{managedHandle}\", \"{jsHandle}\")");

			/// <inheritdoc />
			public object InvokeManaged(object instance, string method, string jsonParameters)
			{
				// TODO: Properly parse parameters
				var parameters = jsonParameters
					.Trim('{', '}', ' ')
					.Split(new [] {','}, StringSplitOptions.RemoveEmptyEntries)
					.Where(parameter => parameter.HasValueTrimmed())
					.Select(parameter => parameter.Split(new[] { ':' }, 2)[1].Trim('"', ' '))
					.ToArray();

				return _methods[method].Invoke(instance, parameters);
			}

			private string BuildPrototype()
			{
				if (_type.IsGenericType)
				{
					throw new NotSupportedException("Generic types are not supported");
				}

				var builder = new IndentedStringBuilder();
				var namespaces = _type.Namespace.Split('.');
				var methods = _type
					.GetMethods(BindingFlags.Instance | BindingFlags.Public)
					.Where(method => method.GetParameters().None(p => p.ParameterType != typeof(string))) // we support only string parameters for now
					.ToArray();

				// Cache the methods for the InvokeManaged method
				_methods = methods.ToDictionary(m => m.Name, m => m);

				builder.AppendLine("(function() {");
				using (builder.Indent())
				{
					using (new CompositeDisposable(namespaces.Select(NameSpaceToJavaScript).Reverse()))
					{
						builder.AppendLine($@"var {_type.Name} = (function() {{
							{_type.Name}.activeInstances = {{}};

							{_type.Name}.createInstance = function(managedId, jsId) {{
								this.activeInstances[jsId] = new {_type.Name}(managedId);

								return ""ok"";
							}}

							{_type.Name}.getInstance = function(managedId, jsId) {{
								return this.activeInstances[jsId];
							}}

							{_type.Name}.destroyInstance = function(managedId, jsId) {{
								delete this.activeInstances[jsId];

								return ""ok"";
							}}

							function {_type.Name}(managedHandle) {{
								console.log(""Create {_type.Name} for managed handle: "" + managedHandle);
								this.__managedHandle = managedHandle;
							}};

							{methods.Select(MethodToJavaScript).JoinBy("\n")}

							return {_type.Name};
						}}());
						{namespaces.Last()}.{_type.Name} = {_type.Name};");
					}
				}
				builder.AppendLine("return \"ok\";");
				builder.AppendLine("})();");

				return builder.ToString();

				IDisposable NameSpaceToJavaScript(string part, int index)
				{
					builder.AppendLine($"var {part} = window.{part};");
					builder.AppendLine($"(function ({part}) {{");

					var indent = builder.Indent();

					return Disposable.Create(() =>
					{
						indent.Dispose();
						if (index == 0)
						{
							builder.AppendLine($"}})({part} || ({part} = {{}}));");
							builder.AppendLine($"window.{part} = {part};");
						}
						else
						{
							var previous = namespaces[index - 1];
							builder.AppendLine($"}})({part} = {previous}.{part} || ({previous}.{part} = {{}}));");
						}
					});
				}

				string MethodToJavaScript(MethodInfo method)
				{
					var parameters = method.GetParameters().Where(p => p.ParameterType == typeof(string)).ToArray();

					if (parameters.Any())
					{
						return $@"
							{_type.Name}.prototype.{method.Name} = function({parameters.Select(parameter => parameter.Name).JoinBy(", ")}) {{
								var parameters = {{{parameters.Select(parameter => $"\"{parameter.Name}\": {parameter.Name}").JoinBy(", ")}}};
								var serializedParameters = JSON.stringify(parameters);

								Uno.Foundation.Interop.ManagedObject.dispatch(this.__managedHandle, ""{method.Name}"", serializedParameters);
							}};";
					}
					else
					{
						return $@"
							{_type.Name}.prototype.{method.Name} = function() {{
								Uno.Foundation.Interop.ManagedObject.dispatch(this.__managedHandle, ""{method.Name}"");
							}};";
					}
				}
			}
		}
	}
}