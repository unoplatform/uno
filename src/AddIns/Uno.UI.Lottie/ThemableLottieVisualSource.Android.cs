using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Data;
using Uno.Disposables;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Uno.Extensions;
using Uno.UI.Lottie;
#if !HAS_UNO_WINUI
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
#endif

// **********************************************************
// *                        ? WHY ?                         *
// **********************************************************
// *                                                        *
// * This file exists only because there's a bug in the     *
// * Xamarin Lottie Player as described in this GH issue:   *
// * https://github.com/Baseflow/LottieXamarin/issues/303   *
// *                                                        *
// * Once this bug is resolved, it will be possible to      *
// * remove this file and the dependency to Newtonsoft.Json *
// * for the Android target of this project.                *
// *                                                        *
// **********************************************************

#if HAS_UNO_WINUI
namespace CommunityToolkit.WinUI.Lottie
#else
namespace Microsoft.Toolkit.Uwp.UI.Lottie
#endif
{
	[Bindable]
	public partial class ThemableLottieVisualSource : LottieVisualSourceBase, IThemableAnimatedVisualSource
	{
		private JObject? _currentDocument;

		private UpdatedAnimation? _updateCallback;
		private string? _sourceCacheKey;


		protected override bool IsPayloadNeedsToBeUpdated => true;

		protected override IDisposable? LoadAndObserveAnimationData(
			IInputStream sourceJson,
			string sourceCacheKey,
			UpdatedAnimation updateCallback)
		{
			var cts = new CancellationTokenSource();

			_updateCallback = updateCallback;

			LoadAndUpdate(cts.Token, sourceCacheKey, sourceJson);

			return Disposable.Create(() =>
			{
				cts.Cancel();
				cts.Dispose();
			});
		}

		private void LoadAndUpdate(
			CancellationToken ct,
			string sourceCacheKey,
			IInputStream sourceJson)
		{
			_sourceCacheKey = sourceCacheKey;

			// Note: we're using Newtonsoft JSON.NET here
			// because System.Text.Json does not support changing the
			// parsed document - it's read only.

			// LOAD & PARSE JSON
			LoadAndParseDocument(sourceJson);

			if (_currentDocument == null)
			{
				return;
			}

			// APPLY PROPERTIES
			ApplyProperties();

			// NOTIFY
			NotifyCallback();
		}

		private void LoadAndParseDocument(IInputStream sourceJson)
		{
			JObject document;
			using (var stream = sourceJson.AsStreamForRead(0))
			{
				using var streamReader = new StreamReader(stream);
				using var reader = new JsonTextReader(streamReader);
				document = JObject.Load(reader);
			}

			_currentDocument = document;

			foreach (var colorBinding in _colorsBindings)
			{
				colorBinding.Value.Elements.Clear();
			}

			void ParseLayers(JToken layersElement)
			{
				if (!(layersElement is JArray layers))
				{
					return; // potentially invalid lottie file
				}

				foreach (var layer in layers)
				{
					if (layer is JObject l)
					{
						var shapesValue = l.GetValue("shapes");

						if (shapesValue is JArray shapes)
						{
							foreach (var shape in shapes)
							{
								if (shape is JObject s)
								{
									ParseShape(s);
								}
							}
						}
					}
				}
			}

			void ParseShape(JObject shapeElement)
			{
				var typeValue = shapeElement.GetValue("ty")!;
				if (typeValue.Type != JTokenType.String)
				{
					return; // potentially invalid lottie file
				}

				var shapeType = typeValue.Value<string>();

				if (shapeType != null && shapeType.Equals("gr"))
				{
					// That's a group

					var itemsProperty = shapeElement.GetValue("it");

					if (itemsProperty is JArray items)
					{
						foreach (var item in items)
						{
							if (item is JObject s)
							{
								ParseShape(s);
							}
						}
					}

					return;
				}

				var nameProperty = shapeElement.GetValue("nm")!;

				if (nameProperty.Type != JTokenType.String)
				{
					return; // No name
				}

				var name = nameProperty.Value<string>()!;

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

			if (document.TryGetValue("layers", out var layers))
			{
				ParseLayers(layers);
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
					var k = (element.GetValue("c") as JObject)?.GetValue("k") as JArray;

					if (k != null)
					{
						k.Clear();
						k.Add(new JValue(colorComponents[0]));
						k.Add(new JValue(colorComponents[1]));
						k.Add(new JValue(colorComponents[2]));
						k.Add(new JValue(colorComponents[3]));

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
				var json = _currentDocument?.ToString(Formatting.None);
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
			internal List<JObject> Elements { get; } = new List<JObject>(1);
			internal Color? CurrentValue { get; set; }
			internal Color? NextValue { get; set; }
		}
	}
}
