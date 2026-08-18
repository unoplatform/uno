// The CI `UpdateTasksSHA` target rewrites the task class suffix to the build SHA, so the versioned names
// below only match the task sources in a local build. Keeping them in this single file lets the
// substitution list cover the tests without rewriting every test source.

global using RuntimeAssetsSelectorTask = Uno.UI.Tasks.RuntimeAssetsSelector.RuntimeAssetsSelectorTask_v0;
global using RuntimeAssetsValidatorTask = Uno.UI.Tasks.RuntimeAssetsValidator.RuntimeAssetsValidatorTask_v0;
