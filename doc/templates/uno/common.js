// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Generate the "Edit this page" / "Improve this Doc" link.
 * For generated documentation pages (e.g., /implemented/ directory), hide the link
 * since these files are auto-generated and should not be manually edited.
 * 
 * @param {Object} model - The page model containing metadata
 * @param {Object} gitContribute - Git contribution settings from docfx.json
 * @param {string} gitUrlPattern - Git URL pattern (e.g., "github")
 * @returns {string|null} - The edit URL or null to hide the link
 */
exports.getImproveTheDocHref = function (model, gitContribute, gitUrlPattern) {
  if (!model) {
    return null;
  }

  // Use the gitContribute from parameter or from model._gitContribute
  const contribute = gitContribute || model._gitContribute;
  
  if (!contribute || !contribute.repo || !contribute.branch) {
    return null;
  }

  // Check if this is a generated documentation file
  // Generated files are in the implemented/ or articles/implemented/ directories and don't have corresponding source files
  const rawSourcePath = model._path || model.source_url || model._rel_path || '';
  // Normalize path: use forward slashes and remove leading slashes for consistent prefix checks
  const normalizedSourcePath = rawSourcePath.replace(/\\/g, '/').replace(/^\/+/, '');
  const isGeneratedDoc = normalizedSourcePath.startsWith('implemented/') ||
                          normalizedSourcePath.startsWith('articles/implemented/') ||
                          model._isGenerated === true;

  if (isGeneratedDoc) {
    // Hide the "Edit this page" link for generated documentation
    return null;
  }

  // For non-generated files, construct the GitHub edit URL
  const repo = contribute.repo;
  const branch = contribute.branch;
  const path = model._path || model._rel_path || '';

  if (!repo || !branch || path == null || path === '') {
    return null;
  }

  // Construct GitHub edit URL
  if (gitUrlPattern === 'github') {
    // Remove any leading slashes and ensure forward slashes
    const cleanPath = path.replace(/^[\/\\]+/, '').replace(/\\/g, '/');
    return `https://github.com/${repo}/blob/${branch}/${cleanPath}`;
  }

  return null;
};
