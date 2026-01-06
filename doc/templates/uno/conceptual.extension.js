// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Extension for conceptual.html.primary template
 * Provides pre and post transform hooks for customizing page models
 */

exports.preTransform = function (model) {
  // Pre-transform hook - modify model before processing.
  return model;
};

exports.postTransform = function (model) {
  // Post-transform hook - modify model after processing.
  return model;
};
