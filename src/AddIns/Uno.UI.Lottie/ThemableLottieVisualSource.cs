using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Lottie;
using Uno.Extensions;
using Uno.Disposables;
using Windows.UI;

#if HAS_UNO_WINUI
namespace CommunityToolkit.WinUI.Lottie
#else
namespace Microsoft.Toolkit.Uwp.UI.Lottie
#endif
{
	[Bindable]
	public partial class ThemableLottieVisualSource : LottieVisualSourceBase, IThemableAnimatedVisualSource
	{
		// Lottie files are routinely hand-edited to carry the "{ Color : var(X) }" binding grammar in
		// shape names, so keep tolerating the trailing commas and comments an editor leaves behind.
		private static readonly JsonDocumentOptions _documentOptions = new() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };

		// The default encoder escapes non-ASCII and HTML-sensitive characters, which would rewrite every
		// layer name in the document; the relaxed encoder emits them as-is, matching the previous output.
		private static readonly JsonSerializerOptions _serializerOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

		private JsonObject? _currentDocument;

		private UpdatedAnimation? _updateCallback;
		private string? _sourceCacheKey;

		protected override bool IsPayloadNeedsToBeUpdated => true;

		public void LoadForTests(
			IInputStream sourceJson,
			string sourceCacheKey,
			UpdatedAnimation updateCallback)
		{
			_updateCallback = updateCallback;
			LoadAndUpdateAsync(sourceCacheKey, sourceJson, CancellationToken.None).GetAwaiter().GetResult();
		}

		public string? GetJson() => _currentDocument?.ToJsonString(_serializerOptions);

		protected override IDisposable? LoadAndObserveAnimationData(
			IInputStream sourceJson,
			string sourceCacheKey,
			UpdatedAnimation updateCallback)
		{
			var cts = new CancellationTokenSource();

			_updateCallback = updateCallback;
			_sourceCacheKey = sourceCacheKey;

			return new AnimationDataLoadSubscription(
				LoadAndUpdateAsync(sourceCacheKey, sourceJson, cts.Token),
				() =>
				{
					cts.Cancel();
					_updateCallback = null;
					_sourceCacheKey = null;
					cts.Dispose();
				});
		}

		private async Task LoadAndUpdateAsync(
			string sourceCacheKey,
			IInputStream sourceJson,
			CancellationToken ct)
		{
			_sourceCacheKey = sourceCacheKey;

			// LOAD & PARSE JSON
			var json = await ReadAnimationJsonAsync(sourceJson, ct);
			ct.ThrowIfCancellationRequested();
			LoadAndParseDocument(json);

			if (_currentDocument == null)
			{
				return;
			}

			// APPLY PROPERTIES
			ApplyProperties();

			// NOTIFY
			NotifyCallback();
		}

		private void LoadAndParseDocument(string json)
		{
			var document = JsonNode.Parse(json, documentOptions: _documentOptions) as JsonObject;

			if (document == null)
			{
				return;
			}

			_currentDocument = document;

			foreach (var colorBinding in _colorsBindings)
			{
				colorBinding.Value.Elements.Clear();
				colorBinding.Value.NextValue ??= colorBinding.Value.CurrentValue;
			}

			void ParseLayers(JsonArray layers)
			{
				if (layers == null)
				{
					return; // potentially invalid lottie file
				}

				foreach (var layer in layers)
				{
					if (layer is JsonObject l)
					{
						if (l.TryGetPropertyValue("shapes", out var shapesValue))
						{
							if (shapesValue is JsonArray shapes)
							{
								foreach (var shape in shapes)
								{
									if (shape is JsonObject s)
									{
										ParseShape(s);
									}
								}
							}
						}
					}
				}
			}

			void ParseShape(JsonObject shapeElement)
			{
				if (!shapeElement.TryGetPropertyValue("ty", out var typeValue))
				{
					return; // potentially invalid lottie file
				}
				if (typeValue is not JsonValue typeString || typeString.GetValueKind() != JsonValueKind.String)
				{
					return; // potentially invalid lottie file
				}

				var shapeType = typeString.GetValue<string>();

				if (shapeType != null && shapeType.Equals("gr"))
				{
					// That's a group

					if (!shapeElement.TryGetPropertyValue("it", out var itemsProperty)
						|| itemsProperty is not JsonArray items)
					{
						return; // potentially invalid lottie file
					}

					foreach (var item in items)
					{
						if (item is JsonObject s)
						{
							ParseShape(s);
						}
					}

					return;
				}

				if (!shapeElement.TryGetPropertyValue("nm", out var nameProperty)
					|| nameProperty is not JsonValue nameString
					|| nameString.GetValueKind() != JsonValueKind.String)
				{
					return; // No name
				}

				var name = nameString.GetValue<string>();

				if (!string.IsNullOrWhiteSpace(name))
				{
					var elementBindings = PropertyBindingsParser.ParseBindings(name);
					if (elementBindings.Length > 0)
					{
						foreach (var binding in elementBindings)
						{
							if (binding.propertyName.Equals("Color", StringComparison.Ordinal))
							{
								if (_colorsBindings.TryGetValue(binding.bindingName, out var colorBinding))
								{
									colorBinding.Elements.Add(shapeElement);
								}
								else
								{
									colorBinding = new ColorBinding();
									colorBinding.Elements.Add(shapeElement);
									_colorsBindings[binding.bindingName] = colorBinding;
								}
							}
						}
					}
				}
			}

			if (document.TryGetPropertyValue("layers", out var lyrs) && lyrs is JsonArray documentLayers)
			{
				ParseLayers(documentLayers);
			}
		}

		private bool ApplyProperties()
		{
			var changed = false;
			foreach (var colorBinding in _colorsBindings)
			{
				if (!(colorBinding.Value.NextValue is { } color))
				{
					continue; // nothing to change
				}

				var colorComponents = new[] { color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f };

				foreach (var element in colorBinding.Value.Elements)
				{
					if (element.TryGetPropertyValue("c", out var cElm)
						&& cElm is JsonObject c
						&& c.TryGetPropertyValue("k", out var kElm)
						&& kElm is JsonArray k)
					{

						k.Clear();
						k.Add(colorComponents[0]);
						k.Add(colorComponents[1]);
						k.Add(colorComponents[2]);
						k.Add(colorComponents[3]);

						changed = true;
					}
				}

				colorBinding.Value.CurrentValue = colorBinding.Value.NextValue;
				colorBinding.Value.NextValue = null;
			}

			return changed;
		}

		private void NotifyCallback()
		{
			if (_updateCallback is { } callback)
			{
				var json = _currentDocument?.ToJsonString(_serializerOptions);
				if (json is { })
				{
					var propertiesKey = _colorsBindings
						.SelectToArray(kvp => $"{kvp.Key}-{kvp.Value.CurrentValue}")
						.JoinBy("-");

					callback(json, _sourceCacheKey + "-" + propertiesKey);
				}
			}
		}


		private class ColorBinding
		{
			internal List<JsonObject> Elements { get; } = new List<JsonObject>(1);
			internal Color? CurrentValue { get; set; }
			internal Color? NextValue { get; set; }
		}
	}
}
