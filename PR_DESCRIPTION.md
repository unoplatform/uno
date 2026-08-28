**GitHub Issue:** closes #15046

## PR Type

📚 Documentation content changes

## What changed? 🚀

Standardized the presentation of documentation Legend sections by using Markdown headings at the appropriate document level. Existing legend entries and table content are unchanged.

## Validation

- `git diff --check`
- `markdownlint -c build/.markdownlint.json "doc/**/*.md"`
- `cspell --config ./build/cSpell.json "doc/**/*.md" "doc/**/toc.yml" --no-progress` (415 files)

## PR Checklist ✅

- [ ] 🧪 Added [Runtime tests, UI tests, or a manual test sample](https://github.com/unoplatform/uno/blob/master/doc/articles/uno-development/working-with-the-samples-apps.md) (not applicable to documentation-only changes)
- [x] 📚 Docs have been updated following the [documentation template](https://github.com/unoplatform/uno/blob/master/doc/.feature-template.md)
- [ ] 🖼️ Validated PR `Screenshots Compare Test Run` results (not applicable to documentation-only changes)
- [x] ❗ Contains **NO** breaking changes
- [ ] 👀 Reviewed 2 other [open pull requests](https://github.com/unoplatform/uno/pulls) (optional)
