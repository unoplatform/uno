#nullable enable

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml.Xbf;

/// <summary>
/// The XBF of a XAML file, along with the type information its node stream refers to.
/// </summary>
/// <remarks>
/// This is the counterpart of the XamlTypeInfo WinUI's XAML compiler generates: the node stream names
/// types and properties by index, and the generated subclass resolves them without reflection.
/// Used by generated code only.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class XbfMetadataProvider
{
	private string?[]? _strings;
	private Type?[]? _types;
	private DependencyProperty?[]? _properties;
	private Dictionary<int, XbfTemplateContent>? _templates;
	private int _stringCount;
	private int _stringOffsetsStart;
	private int _stringDataStart;
	private int _nodeStreamStart;

	/// <summary>
	/// The XBF document: header, string table, then the node stream.
	/// </summary>
	protected abstract ReadOnlySpan<byte> Data { get; }

	protected abstract int TypeCount { get; }

	protected abstract int PropertyCount { get; }

	protected abstract object CreateInstance(int typeIndex);

	protected abstract Type GetTypeCore(int typeIndex);

	protected abstract DependencyProperty GetDependencyPropertyCore(int propertyIndex);

	protected abstract void SetPropertyValue(int propertyIndex, object target, object? value);

	protected abstract object? GetPropertyValue(int propertyIndex, object target);

	protected abstract void AddToCollection(int adderIndex, object collection, object? item);

	protected abstract object? GetConstant(int constantIndex);

	internal ReadOnlySpan<byte> NodeStream
	{
		get
		{
			EnsureInitialized();
			return Data.Slice(_nodeStreamStart);
		}
	}

	internal object Create(int typeIndex) => CreateInstance(typeIndex);

	internal void SetProperty(int propertyIndex, object target, object? value) => SetPropertyValue(propertyIndex, target, value);

	internal object? GetProperty(int propertyIndex, object target) => GetPropertyValue(propertyIndex, target);

	internal void Add(int adderIndex, object collection, object? item) => AddToCollection(adderIndex, collection, item);

	internal object? Constant(int constantIndex) => GetConstant(constantIndex);

	internal Type GetXamlType(int typeIndex)
	{
		var types = _types ??= new Type?[TypeCount];
		return types[typeIndex] ??= GetTypeCore(typeIndex);
	}

	internal DependencyProperty GetDependencyProperty(int propertyIndex)
	{
		var properties = _properties ??= new DependencyProperty?[PropertyCount];
		return properties[propertyIndex] ??= GetDependencyPropertyCore(propertyIndex);
	}

	internal string GetString(int stringIndex)
	{
		EnsureInitialized();

		return _strings![stringIndex] ??= ReadString(stringIndex);
	}

	/// <summary>
	/// Gets the content of the template found at <paramref name="start"/>.
	/// </summary>
	/// <remarks>
	/// Templates are compared through their factory, so every template created from the same
	/// content needs the same one.
	/// </remarks>
	internal XbfTemplateContent GetTemplateContent(int start, int length, object? parseContext, string? baseUri)
	{
		var templates = _templates ??= new();

		if (!templates.TryGetValue(start, out var content))
		{
			templates[start] = content = new XbfTemplateContent(this, start, length, parseContext, baseUri);
		}

		return content;
	}

	private string ReadString(int stringIndex)
	{
		var data = Data;
		var offsets = data.Slice(_stringOffsetsStart + stringIndex * 4, 8);
		var start = BinaryPrimitives.ReadInt32LittleEndian(offsets);
		var end = BinaryPrimitives.ReadInt32LittleEndian(offsets.Slice(4));

		return Encoding.UTF8.GetString(data.Slice(_stringDataStart + start, end - start));
	}

	private void EnsureInitialized()
	{
		if (_strings is not null)
		{
			return;
		}

		var data = Data;

		if (BinaryPrimitives.ReadUInt32LittleEndian(data) != XbfConstants.Magic)
		{
			throw new InvalidOperationException("The XBF header is invalid.");
		}

		var metadataSize = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(4));
		var majorVersion = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(12));
		if (majorVersion != XbfConstants.MajorFileVersion)
		{
			throw new InvalidOperationException($"The XBF version {majorVersion} is not supported.");
		}

		_stringCount = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(20));
		_stringOffsetsStart = XbfConstants.HeaderSize;
		_stringDataStart = _stringOffsetsStart + (_stringCount + 1) * 4;
		_nodeStreamStart = XbfConstants.HeaderSize + metadataSize;
		_strings = new string?[_stringCount];
	}
}
