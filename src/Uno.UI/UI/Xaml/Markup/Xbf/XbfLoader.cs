#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Uno.UI.Xaml.Xbf;

/// <summary>
/// Creates the objects described by an XBF node stream. Used by generated code only.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class XbfLoader
{
	/// <summary>
	/// Creates the object whose nodes start at <paramref name="offset"/>.
	/// </summary>
	/// <param name="provider">The XBF of the XAML file.</param>
	/// <param name="offset">The offset of the object in the node stream.</param>
	/// <param name="owner">The owner of the resources and templates being created.</param>
	/// <param name="parseContext">The context resources are resolved with.</param>
	/// <param name="baseUri">The URI of the XAML file.</param>
	public static object? Load(XbfMetadataProvider provider, int offset, object? owner, object? parseContext, string? baseUri)
	{
		var context = new XbfLoadContext(provider, owner, parseContext, baseUri, settings: null, isTemplate: false);
		var writer = new BinaryFormatObjectWriter(context);

		return writer.WriteNodes(offset, int.MaxValue);
	}
}

/// <summary>
/// What the nodes of one object graph share: the scope a generated class holds in its fields.
/// </summary>
internal sealed class XbfLoadContext
{
	private Dictionary<string, ElementNameSubject>? _elementNameSubjects;
	private List<(DependencyObject Target, FrameworkElement? ResourceContext)>? _resourceBindings;
	private bool _isLoading;

	public XbfLoadContext(XbfMetadataProvider provider, object? owner, object? parseContext, string? baseUri, TemplateMaterializationSettings? settings, bool isTemplate)
	{
		Provider = provider;
		Owner = owner;
		ParseContext = parseContext;
		BaseUri = baseUri;
		Settings = settings;
		IsTemplate = isTemplate;

		if (isTemplate)
		{
			NameScope = new NameScope();
		}
	}

	public XbfMetadataProvider Provider { get; }

	public object? Owner { get; }

	public object? ParseContext { get; }

	public string? BaseUri { get; }

	public TemplateMaterializationSettings? Settings { get; }

	public bool IsTemplate { get; }

	public NameScope? NameScope { get; }

	public bool HasResourceBindings => _resourceBindings is { Count: > 0 };

	public ElementNameSubject GetElementNameSubject(string name, bool isRuntimeBound)
	{
		var subjects = _elementNameSubjects ??= new();

		if (!subjects.TryGetValue(name, out var subject))
		{
			subjects[name] = subject = new ElementNameSubject(isRuntimeBound, name);
		}

		return subject;
	}

	public void TrackResourceBindings(DependencyObject target, FrameworkElement? resourceContext)
	{
		if (_isLoading)
		{
			// The root is already loading: this is a deferred part, which is updated by what materializes it.
			return;
		}

		(_resourceBindings ??= new()).Add((target, resourceContext));
	}

	public void OnRootLoading(FrameworkElement sender, object args)
	{
		_isLoading = true;

		if (_resourceBindings is { } resourceBindings)
		{
			foreach (var (target, resourceContext) in resourceBindings)
			{
				target.UpdateResourceBindings(resourceContext);
			}
		}
	}
}

/// <summary>
/// The content of a template, as a range of the node stream.
/// </summary>
internal sealed class XbfTemplateContent
{
	private readonly XbfMetadataProvider _provider;
	private readonly int _start;
	private readonly int _length;
	private readonly object? _parseContext;
	private readonly string? _baseUri;

	public XbfTemplateContent(XbfMetadataProvider provider, int start, int length, object? parseContext, string? baseUri)
	{
		_provider = provider;
		_start = start;
		_length = length;
		_parseContext = parseContext;
		_baseUri = baseUri;
		Builder = Build;
	}

	/// <summary>
	/// The one builder of this content: templates are compared through their factory.
	/// </summary>
	public FrameworkTemplateBuilder Builder { get; }

	private UIElement? Build(object? owner, TemplateMaterializationSettings settings)
	{
		var context = new XbfLoadContext(_provider, owner, _parseContext, _baseUri, settings, isTemplate: true);
		var writer = new BinaryFormatObjectWriter(context);

		var rootInstance = writer.WriteNodes(_start, _start + _length) as UIElement;

		if (rootInstance is FrameworkElement frameworkElement && context.HasResourceBindings)
		{
			frameworkElement.Loading += context.OnRootLoading;
		}

		if (rootInstance is not null && NameScope.GetNameScope(rootInstance) is null)
		{
			NameScope.SetNameScope(rootInstance, context.NameScope);
			context.NameScope!.Owner = rootInstance;
		}

		return rootInstance;
	}
}

/// <summary>
/// Nodes that are run when what they build is first needed.
/// </summary>
internal sealed class XbfDeferredBuilder
{
	private readonly XbfLoadContext _context;
	private readonly int _start;
	private readonly int _length;
	private readonly object? _instance;

	public XbfDeferredBuilder(XbfLoadContext context, int start, int length, object? instance)
	{
		_context = context;
		_start = start;
		_length = length;
		_instance = instance;
	}

	/// <summary>
	/// Applies the nodes to the instance they were deferred for.
	/// </summary>
	public void Build()
	{
		var writer = new BinaryFormatObjectWriter(_context);

		writer.WriteNodes(_start, _start + _length, _instance);
	}

	/// <summary>
	/// Creates the element the nodes describe.
	/// </summary>
	public UIElement CreateElement() => (UIElement)Create();

	/// <summary>
	/// Creates the object the nodes describe.
	/// </summary>
	public object Create()
	{
		var writer = new BinaryFormatObjectWriter(_context);

		return writer.WriteNodes(_start, _start + _length)!;
	}
}
