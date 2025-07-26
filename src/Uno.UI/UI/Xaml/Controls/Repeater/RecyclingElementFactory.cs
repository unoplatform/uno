// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Controls
{
	[ContentProperty(Name = nameof(Templates))]
	public partial class RecyclingElementFactory : ElementFactory
	{
		private RecyclePool m_recyclePool;
		private IDictionary<string, DataTemplate> m_templates = new Dictionary<string, DataTemplate>();
		private SelectTemplateEventArgs m_args;

		public event TypedEventHandler<RecyclingElementFactory, SelectTemplateEventArgs> SelectTemplateKey;

		public RecyclingElementFactory()
		{
		}

		#region IRecyclingElementFactory

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

		#endregion

		#region IRecyclingElementFactoryOverrides

		protected virtual string OnSelectTemplateKeyCore(object dataContext, UIElement owner)
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

		#endregion

		#region IElementFactoryOverrides

		protected override UIElement GetElementCore(ElementFactoryGetArgs args)
		{
			if (m_templates is null || m_templates.Count == 0)
			{
				throw new InvalidOperationException("Templates property cannot be null or empty.");
			}

			var winrtOwner = args.Parent;
			var templateKey =
				m_templates.Count == 1 ? m_templates.First().Key : OnSelectTemplateKeyCore(args.Data, winrtOwner);

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

		#endregion
	}
}
