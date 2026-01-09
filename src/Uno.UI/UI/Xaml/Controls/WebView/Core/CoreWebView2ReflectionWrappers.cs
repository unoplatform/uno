#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Windows.Storage.Streams;

namespace Microsoft.Web.WebView2.Core;

internal class ReflectionNativeWebResourceResponse : INativeWebResourceResponse
{
	private readonly object _target;

	public ReflectionNativeWebResourceResponse(object target)
	{
		_target = target;
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
	private Type TargetType => _target.GetType();

	internal object Target => _target;

	public IRandomAccessStream Content
	{
		get => (IRandomAccessStream)GetProperty("Content");
		set => SetProperty("Content", value);
	}

	public INativeHttpResponseHeaders Headers => new ReflectionNativeHttpResponseHeaders(GetProperty("Headers"));

	public int StatusCode
	{
		get => (int)GetProperty("StatusCode");
		set => SetProperty("StatusCode", value);
	}

	public string ReasonPhrase
	{
		get => (string)GetProperty("ReasonPhrase");
		set => SetProperty("ReasonPhrase", value);
	}

	private object GetProperty(string name)
	{
		var prop = TargetType.GetProperty(name);
		return prop?.GetValue(_target) ?? throw new InvalidOperationException($"Property {name} not found on {_target.GetType()}");
	}

	private void SetProperty(string name, object value)
	{
		var prop = TargetType.GetProperty(name);
		if (prop != null)
			prop.SetValue(_target, value);
		else
			throw new InvalidOperationException($"Property {name} not found on {_target.GetType()}");
	}
}

internal class ReflectionNativeHttpResponseHeaders : INativeHttpResponseHeaders
{
	private readonly object _target;

	public ReflectionNativeHttpResponseHeaders(object target)
	{
		_target = target;
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
	private Type TargetType => _target.GetType();

	public void AppendHeader(string name, string value)
		=> InvokeMethod("AppendHeader", name, value);

	public bool Contains(string name)
		=> (bool)InvokeMethod("Contains", name);

	public string GetHeader(string name)
		=> (string)InvokeMethod("GetHeader", name);

	public object GetHeaders(string name)
		=> InvokeMethod("GetHeaders", name);

	public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
	{
		if (_target is IEnumerable enumerable)
		{
			foreach (var item in enumerable)
			{
				yield return ConvertToKeyValuePair(item);
			}
		}
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	private object InvokeMethod(string name, params object[] args)
	{
		var method = TargetType.GetMethod(name, GetTypes(args));
		return method?.Invoke(_target, args) ?? throw new InvalidOperationException($"Method {name} not found on {_target.GetType()}");
	}

	private Type[] GetTypes(object[] args)
	{
		var types = new Type[args.Length];
		for (int i = 0; i < args.Length; i++)
			types[i] = args[i]?.GetType() ?? typeof(object);
		return types;
	}

	private static KeyValuePair<string, string> ConvertToKeyValuePair(object item)
	{
		return item switch
		{
			KeyValuePair<string, string> pair => pair,
			_ => new KeyValuePair<string, string>(
				(string)item.GetType().GetProperty("Key")!.GetValue(item)!,
				(string)item.GetType().GetProperty("Value")!.GetValue(item)!)
		};
	}
}

internal class ReflectionNativeHttpHeadersCollectionIterator : INativeHttpHeadersCollectionIterator
{
	private readonly object _target;

	public ReflectionNativeHttpHeadersCollectionIterator(object target)
	{
		_target = target;
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
	private Type TargetType => _target.GetType();

	public object Current => GetProperty("Current");

	public bool HasCurrent => (bool)GetProperty("HasCurrent");

	public bool MoveNext() => (bool)InvokeMethod("MoveNext");

	public uint GetMany(object items) => (uint)InvokeMethod("GetMany", items);

	private object GetProperty(string name) => TargetType.GetProperty(name)?.GetValue(_target) ?? throw new InvalidOperationException();
	private object InvokeMethod(string name, params object[] args) => TargetType.GetMethod(name)?.Invoke(_target, args) ?? throw new InvalidOperationException();
}

internal class ReflectionNativeWebResourceRequestedEventArgs : INativeWebResourceRequestedEventArgs
{
	private readonly object _target;

	public ReflectionNativeWebResourceRequestedEventArgs(object target)
	{
		_target = target;
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
	private Type TargetType => _target.GetType();

	public object Request => GetProperty("Request");

	public INativeWebResourceResponse? Response
	{
		get
		{
			var result = GetProperty("Response");
			return result == null ? null : new ReflectionNativeWebResourceResponse(result);
		}
		set => SetProperty("Response", value is ReflectionNativeWebResourceResponse r ? r.Target : (value == null ? null : throw new ArgumentException("Value must be a ReflectionNativeWebResourceResponse")));
	}

	// This assumes the underlying object has the correct Enum value, or int.
	public CoreWebView2WebResourceContext ResourceContext => (CoreWebView2WebResourceContext)GetProperty("ResourceContext");

	public CoreWebView2WebResourceRequestSourceKinds RequestedSourceKind => (CoreWebView2WebResourceRequestSourceKinds)GetProperty("RequestedSourceKind");

	public Windows.Foundation.Deferral GetDeferral() => (Windows.Foundation.Deferral)InvokeMethod("GetDeferral");

	private object GetProperty(string name) => TargetType.GetProperty(name)?.GetValue(_target) ?? throw new InvalidOperationException();
	private void SetProperty(string name, object value) => TargetType.GetProperty(name)?.SetValue(_target, value);
	private object InvokeMethod(string name) => TargetType.GetMethod(name)?.Invoke(_target, null) ?? throw new InvalidOperationException();
}

internal class ReflectionNativeWebResourceRequest : INativeWebResourceRequest
{
	private readonly object _target;

	public ReflectionNativeWebResourceRequest(object target)
	{
		_target = target;
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
	private Type TargetType => _target.GetType();

	internal object Target => _target;

	public string Uri
	{
		get => (string)GetProperty("Uri");
		set => SetProperty("Uri", value);
	}

	public string Method
	{
		get => (string)GetProperty("Method");
		set => SetProperty("Method", value);
	}

	public IRandomAccessStream Content
	{
		get => (IRandomAccessStream)GetProperty("Content");
		set => SetProperty("Content", value);
	}

	public INativeHttpRequestHeaders Headers => new ReflectionNativeHttpRequestHeaders(GetProperty("Headers"));

	private object GetProperty(string name) => TargetType.GetProperty(name)?.GetValue(_target) ?? throw new InvalidOperationException();
	private void SetProperty(string name, object value) => TargetType.GetProperty(name)?.SetValue(_target, value);
}

internal class ReflectionNativeHttpRequestHeaders : INativeHttpRequestHeaders
{
	private readonly object _target;

	public ReflectionNativeHttpRequestHeaders(object target)
	{
		_target = target;
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
	private Type TargetType => _target.GetType();

	internal object Target => _target;

	public string GetHeader(string name) => (string)InvokeMethod("GetHeader", name);

	public object GetHeaders(string name) => InvokeMethod("GetHeaders", name);

	public bool Contains(string name) => (bool)InvokeMethod("Contains", name);

	public void SetHeader(string name, string value) => InvokeMethod("SetHeader", name, value);

	public void RemoveHeader(string name) => InvokeMethod("RemoveHeader", name);

	public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
	{
		if (_target is IEnumerable enumerable)
		{
			foreach (var item in enumerable)
			{
				yield return ConvertToKeyValuePair(item);
			}
		}
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	private object InvokeMethod(string name, params object[] args)
	{
		var method = TargetType.GetMethod(name, GetTypes(args));
		return method?.Invoke(_target, args) ?? throw new InvalidOperationException($"Method {name} not found on {_target.GetType()}");
	}

	private Type[] GetTypes(object[] args)
	{
		var types = new Type[args.Length];
		for (int i = 0; i < args.Length; i++)
			types[i] = args[i]?.GetType() ?? typeof(object);
		return types;
	}

	private static KeyValuePair<string, string> ConvertToKeyValuePair(object item)
	{
		return item switch
		{
			KeyValuePair<string, string> pair => pair,
			_ => new KeyValuePair<string, string>(
				(string)item.GetType().GetProperty("Key")!.GetValue(item)!,
				(string)item.GetType().GetProperty("Value")!.GetValue(item)!)
		};
	}
}
