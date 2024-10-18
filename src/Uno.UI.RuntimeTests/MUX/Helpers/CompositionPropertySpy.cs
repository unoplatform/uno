// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Composition;
using Windows.UI.Xaml;

namespace MUXControlsTestApp.Utilities
{
	/// <summary>
	/// Utility class used to spy on Composition properties that are being animated by ExpressionAnimation.
	/// </summary>
	public static class CompositionPropertySpy
	{
		private enum CompositionFacadeType
		{
			Translation,
			Scale,
		}
		private enum CompositionPropertyType
		{
			Boolean,
			Float,
			Vector2,
			Vector3,
			// Add to this enumeration if there is a need for spying on another type of Composition property.
		}

		const string c_source = "source";
		const string c_translationFacadeName = "Translation";
		const string c_scaleFacadeName = "Scale";
		private static Dictionary<CompositionObject, KeyValuePair<CompositionPropertySet, Dictionary<string, ExpressionAnimation>>> s_objectsDictionary = new Dictionary<CompositionObject, KeyValuePair<CompositionPropertySet, Dictionary<string, ExpressionAnimation>>>();
		private static Dictionary<UIElement, KeyValuePair<CompositionPropertySet, Dictionary<string, ExpressionAnimation>>> s_elementsDictionary = new Dictionary<UIElement, KeyValuePair<CompositionPropertySet, Dictionary<string, ExpressionAnimation>>>();
		private static UnoAutoResetEvent s_tickEvent = null;

		/// <summary>
		/// Starts spying on the translation facade of the provided element.
		/// </summary>
		/// <param name="sourceElement"></param>
		/// <param name="originalValue"></param>
		public static void StartSpyingTranslationFacade(UIElement sourceElement, Compositor compositor, Vector3 originalValue)
		{
			CompositionPropertySet propertySet = null;
			ExpressionAnimation expressionAnimation = null;
			string propertySetPropertyName = null;

			StartSpyingFacade(sourceElement, compositor, CompositionFacadeType.Translation, out propertySet, out expressionAnimation, out propertySetPropertyName);

			propertySet.InsertVector3(propertySetPropertyName, originalValue);
			propertySet.StartAnimation(propertySetPropertyName, expressionAnimation);
		}

		/// <summary>
		/// Starts spying on the translation facade of the provided element.
		/// </summary>
		/// <param name="sourceElement"></param>
		/// <param name="originalValue"></param>
		public static void StartSpyingScaleFacade(UIElement sourceElement, Compositor compositor, Vector3 originalValue)
		{
			CompositionPropertySet propertySet = null;
			ExpressionAnimation expressionAnimation = null;
			string propertySetPropertyName = null;

			StartSpyingFacade(sourceElement, compositor, CompositionFacadeType.Scale, out propertySet, out expressionAnimation, out propertySetPropertyName);

			propertySet.InsertVector3(propertySetPropertyName, originalValue);
			propertySet.StartAnimation(propertySetPropertyName, expressionAnimation);
		}

		/// <summary>
		/// Starts spying on the boolean property of the provided object.
		/// </summary>
		/// <param name="sourceObject"></param>
		/// <param name="propertyName"></param>
		/// <param name="originalValue"></param>
		public static void StartSpyingBooleanProperty(CompositionObject sourceObject, string propertyName, bool originalValue = false)
		{
			CompositionPropertySet propertySet = null;
			ExpressionAnimation expressionAnimation = null;
			string propertySetPropertyName = null;

			StartSpyingProperty(sourceObject, ref propertyName, out propertySet, out expressionAnimation, out propertySetPropertyName);

			propertySet.InsertBoolean(propertySetPropertyName, originalValue);
			propertySet.StartAnimation(propertySetPropertyName, expressionAnimation);
		}

		/// <summary>
		/// Starts spying on the scalar property of the provided object.
		/// </summary>
		/// <param name="sourceObject"></param>
		/// <param name="propertyName"></param>
		/// <param name="originalValue"></param>
		public static void StartSpyingScalarProperty(CompositionObject sourceObject, string propertyName, float originalValue = 0.0f)
		{
			CompositionPropertySet propertySet = null;
			ExpressionAnimation expressionAnimation = null;
			string propertySetPropertyName = null;

			StartSpyingProperty(sourceObject, ref propertyName, out propertySet, out expressionAnimation, out propertySetPropertyName);

			propertySet.InsertScalar(propertySetPropertyName, originalValue);
			propertySet.StartAnimation(propertySetPropertyName, expressionAnimation);
		}

		/// <summary>
		/// Starts spying on the vector2 property of the provided object.
		/// </summary>
		/// <param name="sourceObject"></param>
		/// <param name="propertyName"></param>
		/// <param name="originalValue"></param>
		public static void StartSpyingVector2Property(CompositionObject sourceObject, string propertyName, Vector2 originalValue)
		{
			CompositionPropertySet propertySet = null;
			ExpressionAnimation expressionAnimation = null;
			string propertySetPropertyName = null;

			StartSpyingProperty(sourceObject, ref propertyName, out propertySet, out expressionAnimation, out propertySetPropertyName);

			propertySet.InsertVector2(propertySetPropertyName, originalValue);
			propertySet.StartAnimation(propertySetPropertyName, expressionAnimation);
		}

		/// <summary>
		/// Stops spying on the translation facade provided UIElement.
		/// </summary>
		/// <param name="sourceElement"></param>
		public static void StopSpyingTranslationFacade(UIElement sourceElement)
		{
			StopSpyingFacade(sourceElement, CompositionFacadeType.Translation);
		}

		/// <summary>
		/// Stops spying on the scale facade provided UIElement.
		/// </summary>
		/// <param name="sourceElement"></param>
		public static void StopSpyingScaleFacade(UIElement sourceElement)
		{
			StopSpyingFacade(sourceElement, CompositionFacadeType.Scale);
		}

		private static void StopSpyingFacade(UIElement sourceElement, CompositionFacadeType facadeType)
		{
			if (sourceElement == null)
			{
				throw new ArgumentException();
			}

			CompositionPropertySet propertySet = null;
			Dictionary<string, ExpressionAnimation> expressionAnimations = null;
			ExpressionAnimation expressionAnimation = null;
			string propertySetPropertyName = null;

			GetExpressionAnimation<UIElement>(
				sourceElement,
				FacadeTypeToStringName(facadeType),
				s_elementsDictionary,
				out propertySet,
				out expressionAnimations,
				out expressionAnimation,
				out propertySetPropertyName);

			if (propertySet != null)
			{
				propertySet.StopAnimation(propertySetPropertyName);
				expressionAnimations.Remove(propertySetPropertyName);
			}
		}

		/// <summary>
		/// Stops spying on the property of the provided object.
		/// </summary>
		/// <param name="sourceObject"></param>
		/// <param name="propertyName"></param>
		public static void StopSpyingProperty(CompositionObject sourceObject, string propertyName)
		{
			if (sourceObject == null || string.IsNullOrWhiteSpace(propertyName))
			{
				throw new ArgumentException();
			}

			CompositionPropertySet propertySet = null;
			Dictionary<string, ExpressionAnimation> expressionAnimations = null;
			ExpressionAnimation expressionAnimation = null;
			string propertySetPropertyName = null;

			GetExpressionAnimation<CompositionObject>(
				sourceObject,
				propertyName,
				s_objectsDictionary,
				out propertySet,
				out expressionAnimations,
				out expressionAnimation,
				out propertySetPropertyName);

			if (propertySet != null)
			{
				propertySet.StopAnimation(propertySetPropertyName);
				expressionAnimations.Remove(propertySetPropertyName);
			}
		}

		/// <summary>
		/// Accesses the translation value through the xaml facade provided UIElement.
		/// </summary>
		/// <param name="sourceElement"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static CompositionGetValueStatus TryGetTranslationFacade(UIElement sourceElement, out Vector3 value)
		{
			value = Vector3.Zero;

			object objectValue = null;
			CompositionGetValueStatus status = TryGetFacadeValue(sourceElement, CompositionFacadeType.Translation, out objectValue);
			if (objectValue != null)
			{
				value = (Vector3)objectValue;
			}
			return status;
		}

		/// <summary>
		/// Accesses the scale value through the xaml facade provided UIElement.
		/// </summary>
		/// <param name="sourceElement"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static CompositionGetValueStatus TryGetScaleFacade(UIElement sourceElement, out Vector3 value)
		{
			value = Vector3.Zero;

			object objectValue = null;
			CompositionGetValueStatus status = TryGetFacadeValue(sourceElement, CompositionFacadeType.Scale, out objectValue);
			if (objectValue != null)
			{
				value = (Vector3)objectValue;
			}
			return status;
		}

		/// <summary>
		/// Accesses the animated boolean value for the provided object and its property name.
		/// </summary>
		/// <param name="sourceObject"></param>
		/// <param name="propertyName"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static CompositionGetValueStatus TryGetBoolean(CompositionObject sourceObject, string propertyName, out bool value)
		{
			value = false;

			object objectValue = null;
			CompositionGetValueStatus status = TryGetPropertyValue(sourceObject, propertyName, CompositionPropertyType.Boolean, out objectValue);
			if (objectValue != null)
			{
				value = (bool)objectValue;
			}
			return status;
		}

		/// <summary>
		/// Accesses the animated scalar value for the provided object and its property name.
		/// </summary>
		/// <param name="sourceObject"></param>
		/// <param name="propertyName"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static CompositionGetValueStatus TryGetScalar(CompositionObject sourceObject, string propertyName, out float value)
		{
			value = 0.0f;

			object objectValue = null;
			CompositionGetValueStatus status = TryGetPropertyValue(sourceObject, propertyName, CompositionPropertyType.Float, out objectValue);
			if (objectValue != null)
			{
				value = (float)objectValue;
			}
			return status;
		}

		/// <summary>
		/// Accesses the animated Vector2 value for the provided object and its property name.
		/// </summary>
		/// <param name="sourceObject"></param>
		/// <param name="propertyName"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static CompositionGetValueStatus TryGetVector2(CompositionObject sourceObject, string propertyName, out Vector2 value)
		{
			value = Vector2.Zero;

			object objectValue = null;
			CompositionGetValueStatus status = TryGetPropertyValue(sourceObject, propertyName, CompositionPropertyType.Vector2, out objectValue);
			if (objectValue != null)
			{
				value = (Vector2)objectValue;
			}
			return status;
		}

		/// <summary>
		/// Forces the UI thread to tick N times synchronously. This allows reading the exact final value of a property
		/// after it stops animating.
		/// </summary>
		/// <param name="ticks"></param>
		public static async Task SynchronouslyTickUIThread(uint ticks)
		{
			if (s_tickEvent == null)
			{
				s_tickEvent = new UnoAutoResetEvent(false);
			}

			for (uint tick = 0; tick < ticks; tick++)
			{
				RunOnUIThread.Execute(() =>
				{
					Windows.UI.Xaml.Media.CompositionTarget.Rendering += CompositionTarget_Rendering;
				});

				await s_tickEvent.WaitOne();
			}
		}

		private static void CompositionTarget_Rendering(object sender, object e)
		{
			Windows.UI.Xaml.Media.CompositionTarget.Rendering -= CompositionTarget_Rendering;
			s_tickEvent.Set();
		}

		private static void StartSpyingFacade(
			UIElement sourceElement,
			Compositor compositor,
			CompositionFacadeType facadeType,
			out CompositionPropertySet propertySet,
			out ExpressionAnimation expressionAnimation,
			out string propertySetPropertyName)
		{
			propertySet = null;
			propertySetPropertyName = null;
			expressionAnimation = null;

			if (sourceElement == null)
			{
				throw new ArgumentException();
			}

			propertySetPropertyName = FacadeTypeToStringName(facadeType);
			StartSpy<UIElement>(sourceElement, s_elementsDictionary, compositor, ref propertySetPropertyName, ref propertySetPropertyName, out propertySet, out expressionAnimation);

			expressionAnimation.SetExpressionReferenceParameter(c_source, sourceElement);
		}

		private static void StartSpyingProperty(
			CompositionObject sourceObject,
			ref string propertyName,
			out CompositionPropertySet propertySet,
			out ExpressionAnimation expressionAnimation,
			out string propertySetPropertyName)
		{
			propertySet = null;
			propertySetPropertyName = null;
			expressionAnimation = null;

			if (sourceObject == null || string.IsNullOrWhiteSpace(propertyName))
			{
				throw new ArgumentException();
			}

			propertyName = propertyName.Trim();
			propertySetPropertyName = propertyName.Replace('.', '_');
			StartSpy<CompositionObject>(sourceObject, s_objectsDictionary, sourceObject.Compositor, ref propertyName, ref propertySetPropertyName, out propertySet, out expressionAnimation);

			expressionAnimation.SetReferenceParameter(c_source, sourceObject);
		}

		private static void StartSpy<T>(
			T source,
			Dictionary<T, KeyValuePair<CompositionPropertySet, Dictionary<string, ExpressionAnimation>>> dictionary,
			Compositor compositor,
			ref string propertyName,
			ref string propertySetPropertyName,
			out CompositionPropertySet propertySet,
			out ExpressionAnimation expressionAnimation)
		{
			propertySet = null;
			expressionAnimation = null;

			if (source == null)
			{
				throw new ArgumentException();
			}

			KeyValuePair<CompositionPropertySet, Dictionary<string, ExpressionAnimation>> propertySetExpressionsPair;
			Dictionary<string, ExpressionAnimation> expressionAnimations = null;

			if (!dictionary.ContainsKey(source))
			{
				propertySet = compositor.CreatePropertySet();
				expressionAnimations = new Dictionary<string, ExpressionAnimation>();

				propertySetExpressionsPair = new KeyValuePair<CompositionPropertySet, Dictionary<string, ExpressionAnimation>>(propertySet, expressionAnimations);
				dictionary.Add(source, propertySetExpressionsPair);
			}
			else
			{
				propertySetExpressionsPair = dictionary[source];
				propertySet = propertySetExpressionsPair.Key;
				expressionAnimations = propertySetExpressionsPair.Value;
			}

			if (!expressionAnimations.ContainsKey(propertySetPropertyName))
			{
				expressionAnimation = compositor.CreateExpressionAnimation(c_source + "." + propertyName);
				expressionAnimations.Add(propertySetPropertyName, expressionAnimation);
			}
			else
			{
				expressionAnimation = expressionAnimations[propertySetPropertyName];
			}
		}

		private static void GetExpressionAnimation<T>(
			T source,
			string propertyName,
			Dictionary<T, KeyValuePair<CompositionPropertySet, Dictionary<string, ExpressionAnimation>>> dictionary,
			out CompositionPropertySet propertySet,
			out Dictionary<string, ExpressionAnimation> expressionAnimations,
			out ExpressionAnimation expressionAnimation,
			out string propertySetPropertyName)
		{
			propertySet = null;
			expressionAnimations = null;
			expressionAnimation = null;
			propertySetPropertyName = null;

			propertyName = propertyName.Trim();
			propertySetPropertyName = propertyName.Replace('.', '_');

			if (dictionary.ContainsKey(source))
			{
				KeyValuePair<CompositionPropertySet, Dictionary<string, ExpressionAnimation>> propertySetExpressionsPair = dictionary[source];
				propertySet = propertySetExpressionsPair.Key;
				expressionAnimations = propertySetExpressionsPair.Value;
				if (expressionAnimations.ContainsKey(propertySetPropertyName))
				{
					expressionAnimation = expressionAnimations[propertySetPropertyName];
				}
			}
		}

		private static CompositionGetValueStatus TryGetFacadeValue(UIElement sourceElement, CompositionFacadeType facadeType, out object value)
		{
			value = null;
			//Once we add a facade which isn't of type Vector3 we'll need to switch on the facade type to specify the return type.
			return TryGetValue<UIElement>(sourceElement, s_elementsDictionary, FacadeTypeToStringName(facadeType), CompositionPropertyType.Vector3, out value);
		}

		private static CompositionGetValueStatus TryGetPropertyValue(CompositionObject sourceObject, string propertyName, CompositionPropertyType type, out object value)
		{
			return TryGetValue<CompositionObject>(sourceObject, s_objectsDictionary, propertyName, type, out value);
		}

		private static CompositionGetValueStatus TryGetValue<T>(
			T source,
			Dictionary<T, KeyValuePair<CompositionPropertySet, Dictionary<string, ExpressionAnimation>>> dictionary,
			string propertyName,
			CompositionPropertyType type,
			out object value)
		{
			value = null;

			if (source == null || string.IsNullOrWhiteSpace(propertyName))
			{
				throw new ArgumentException();
			}

			CompositionPropertySet propertySet = null;
			Dictionary<string, ExpressionAnimation> expressionAnimations = null;
			ExpressionAnimation expressionAnimation = null;
			string propertySetPropertyName = null;

			GetExpressionAnimation<T>(
				source,
				propertyName,
				dictionary,
				out propertySet,
				out expressionAnimations,
				out expressionAnimation,
				out propertySetPropertyName);

			if (propertySet != null)
			{
				propertySet.StopAnimation(propertySetPropertyName);

				CompositionGetValueStatus status = CompositionGetValueStatus.TypeMismatch;

				switch (type)
				{
					case CompositionPropertyType.Boolean:
						bool boolValue;
						status = propertySet.TryGetBoolean(propertySetPropertyName, out boolValue);
						value = boolValue;
						break;
					case CompositionPropertyType.Float:
						float floatValue;
						status = propertySet.TryGetScalar(propertySetPropertyName, out floatValue);
						value = floatValue;
						break;
					case CompositionPropertyType.Vector2:
						Vector2 vector2Value;
						status = propertySet.TryGetVector2(propertySetPropertyName, out vector2Value);
						value = vector2Value;
						break;
					case CompositionPropertyType.Vector3:
						Vector3 vector3Value;
						status = propertySet.TryGetVector3(propertySetPropertyName, out vector3Value);
						value = vector3Value;
						break;
						// Insert new case for any property type that is being added.
				}

				if (expressionAnimation != null)
				{
					propertySet.StartAnimation(propertySetPropertyName, expressionAnimation);
				}

				return status;
			}

			return CompositionGetValueStatus.NotFound;
		}

		private static string FacadeTypeToStringName(CompositionFacadeType facadeType)
		{
			switch (facadeType)
			{
				case CompositionFacadeType.Translation:
					return c_translationFacadeName;
				case CompositionFacadeType.Scale:
					return c_scaleFacadeName;
				default:
					return "";
			}
		}
	}
}
