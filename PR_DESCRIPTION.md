**GitHub Issue:** closes #15045

## PR Type:

📚 Documentation content changes

## What changed? 🚀

Moved the AppBarButton-specific documentation to its own page and added it to the controls navigation.

The existing CommandBar page remains available, including a legacy AppBarButton heading that links to the new page. Existing navigation references now link directly to the relevant CommandBar and AppBarButton pages.

## PR Checklist ✅

- [ ] 🧪 Added Runtime tests, UI tests, or a manual test sample (not applicable to documentation-only changes)
- [x] 📚 Docs have been added/updated following the documentation template
- [ ] 🖼️ Validated PR `Screenshots Compare Test Run` results (not applicable to documentation-only changes)
- [x] ❗ Contains **NO** breaking changes
- [ ] 👀 Reviewed 2 other open pull requests (optional)

## Validation

- `git diff --check`: passed
- `markdownlint`: passed for all documentation files
- `cSpell`: passed for all documentation files and TOCs
- Focused `markdown-link-check`: passed for the new page, the updated navigation pages, and the CommandBar page
- DocFX: blocked by pre-existing missing generated/external TOCs and template recursion errors in the local clone
