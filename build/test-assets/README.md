# XamlMerge Priority-Based Ordering Tests

This directory contains integration tests for the XamlMerge priority-based feature implemented in `src/Directory.Build.targets`.

## Feature Description

The `PrepareXamlMergeInput` target in `src/Directory.Build.targets` orders XAML pages for merging based on their priority:

1. Pages with `Priority="1"` metadata are included first
2. Pages without priority metadata (or with empty priority) are included after

This ensures that high-priority XAML resource dictionaries (like theme resources) are processed before other pages.

## Test Structure

### Test 1: Single Priority Page (XamlMergePriorityTest)

Tests that a single page with `Priority="1"` is ordered before pages without priority.

**Input:**
- `Page1.xaml` (no priority)
- `Page2.xaml` (no priority)
- `HighPriorityPage.xaml` (Priority="1")
- `Page3.xaml` (no priority)

**Expected Output Order:**
1. HighPriorityPage.xaml
2. Page1.xaml
3. Page2.xaml
4. Page3.xaml

### Test 2: Multiple Priority Pages (XamlMergePriorityTest2)

Tests that multiple pages with `Priority="1"` all come before pages without priority.

**Input:**
- `NormalPage1.xaml` (no priority)
- `PriorityPage1.xaml` (Priority="1")
- `PriorityPage2.xaml` (Priority="1")
- `NormalPage2.xaml` (no priority)

**Expected Output Order:**
1. PriorityPage1.xaml
2. PriorityPage2.xaml
3. NormalPage1.xaml
4. NormalPage2.xaml

## Running the Tests

### Manually run individual tests:

```bash
cd build/test-assets/XamlMergePriorityTest
dotnet msbuild TestProject.csproj /t:TestPrepareXamlMergeInput

cd build/test-assets/XamlMergePriorityTest2
dotnet msbuild TestProject.csproj /t:TestPrepareXamlMergeInput
```

### Check output:

```bash
cat build/test-assets/XamlMergePriorityTest/test-output.txt
cat build/test-assets/XamlMergePriorityTest2/test-output.txt
```

## Implementation Details

The tests use a simplified copy of the `PrepareXamlMergeInput` target:

```xml
<Target Name="PrepareXamlMergeInput" Condition="'$(XamlMergeOutputFile)'!=''">
	<ItemGroup>
		<OrderedMergedXamlResources Include="@(Page)" Condition="'%(Page.Priority)' == '1'" />
		<OrderedMergedXamlResources Include="@(Page)" Exclude="@(_NonMergedXamlResources)" Condition="'%(Page.Priority)' == ''" />
		
		<XamlMergeInput Include="@(OrderedMergedXamlResources)" />
	</ItemGroup>
</Target>
```

This target:
1. First adds all `Page` items with `Priority='1'` to `OrderedMergedXamlResources`
2. Then adds all `Page` items without priority (empty or not set) to `OrderedMergedXamlResources`
3. Finally copies `OrderedMergedXamlResources` to `XamlMergeInput` for the merge task

## Related Files

- `src/Directory.Build.targets` - Contains the actual `PrepareXamlMergeInput` target
- `src/Uno.UI/XamlMerge.targets` - Sets up priority for specific files (e.g., `Common_themeresources_any.xaml`)
