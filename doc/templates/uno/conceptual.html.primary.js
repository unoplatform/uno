// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

const common = require('./common.js');
const extension = require('./conceptual.extension.js')

exports.transform = function (model) {
  if (extension && extension.preTransform) {
    model = extension.preTransform(model);
  }

  model._disableToc = model._disableToc || !model._tocPath || (model._navPath === model._tocPath);
  
  // Hide edit links for generated pages:
  // - API documentation (auto-generated from code)
  // - Implemented controls pages (generated via PowerShell script)
  const pathToCheck = (model._path || model._rel || '').toLowerCase();
  const isGeneratedPage = pathToCheck.includes('/api/') || 
                         pathToCheck.includes('articles/implemented/') ||
                         pathToCheck.includes('articles\\implemented\\');
  
  if (isGeneratedPage) {
    model.docurl = null;
  } else {
    model.docurl = model.docurl || common.getImproveTheDocHref(model, model._gitContribute, model._gitUrlPattern);
  }

  if (extension && extension.postTransform) {
    model = extension.postTransform(model);
  }

  return model;
}
