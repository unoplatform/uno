// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RecyclingElementFactory.cpp, commit 4b206bce3

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.UI.Xaml.Controls;

partial class RecyclingElementFactory
{
	public RecyclingElementFactory()
	{
	}

	// #pragma region IRecyclingElementFactory

	/// <summary>
	/// Gets or sets the pool used to recycle elements created by this factory.
	/// </summary>
	public RecyclePool RecyclePool
	{
		get
		{
			return m_recyclePool;
		}
		set
		{
			m_recyclePool = value;
		}
	}

	/// <summary>
	/// Gets or sets the templates used to create elements by this factory.
	/// </summary>
	public IDictionary<string, DataTemplate> Templates
	{
		get
		{
			return m_templates;
		}
		set
		{
			m_templates = value;
		}
	}

	// #pragma endregion

	// #pragma region IRecyclingElementFactoryOverrides

	/// <summary>
	/// When implemented in a derived class, returns the key of the template to use for the specified data.
	/// </summary>
	/// <param name="dataContext">The data for which a template key is requested.</param>
	/// <param name="owner">The owner element for which the data context is being prepared.</param>
	/// <returns>The key of the template to use.</returns>
	protected virtual string OnSelectTemplateKeyCore(
		object dataContext,
		UIElement owner)
	{
		if (m_args is null)
		{
			m_args = new SelectTemplateEventArgs();
		}

		var args = m_args;
		args.TemplateKey = null;
		args.DataContext = dataContext;
		args.Owner = owner;

		SelectTemplateKey?.Invoke(this, args);

		var templateKey = args.TemplateKey;
		if (string.IsNullOrEmpty(templateKey))
		{
			throw new InvalidOperationException("Please provide a valid template identifier in the handler for the SelectTemplateKey event.");
		}

		return templateKey;
	}

	// #pragma endregion

	// #pragma region IElementFactoryOverrides

	protected override UIElement GetElementCore(ElementFactoryGetArgs args)
	{
		if (m_templates is null || m_templates.Count == 0)
		{
			throw new InvalidOperationException("Templates property cannot be null or empty.");
		}

		var winrtOwner = args.Parent;
		var templateKey =
			m_templates.Count == 1 ?
			m_templates.First().Key :
			OnSelectTemplateKeyCore(args.Data, winrtOwner);

		if (string.IsNullOrEmpty(templateKey))
		{
			// Note: We could allow null/whitespace, which would work as long as
			// the recycle pool is not shared. in order to make this work in all cases
			// currently we validate that a valid template key is provided.
			throw new InvalidOperationException("Template key cannot be empty or null.");
		}

		// Get an element from the Recycle Pool or create one
		var element = m_recyclePool.TryGetElement(templateKey, winrtOwner) as FrameworkElement;

		if (element is null)
		{
			// No need to call HasKey if there is only one template.
			if (m_templates.Count > 1 && !m_templates.ContainsKey(templateKey))
			{
				string message = "No templates of key " + templateKey + " were found in the templates collection.";
				throw new InvalidOperationException(message);
			}

			var dataTemplate = m_templates[templateKey];
			element = dataTemplate.LoadContent() as FrameworkElement;

			// Associate ReuseKey with element
			RecyclePool.SetReuseKey(element, templateKey);
		}

		return element;
	}

	protected override void RecycleElementCore(ElementFactoryRecycleArgs args)
	{
		var element = args.Element;
		var key = RecyclePool.GetReuseKey(element);
		m_recyclePool.PutElement(element, key, args.Parent);
	}

	// #pragma endregion
}
