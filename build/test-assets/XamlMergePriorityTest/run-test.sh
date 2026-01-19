#!/bin/bash
# Test script to validate XamlMerge priority-based ordering

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TEST_PROJECT_DIR="$SCRIPT_DIR"

echo "Running XamlMerge priority test..."
echo "Test project directory: $TEST_PROJECT_DIR"

# Run the MSBuild target
msbuild "$TEST_PROJECT_DIR/TestProject.csproj" /t:TestPrepareXamlMergeInput /v:minimal

# Check if the output file exists
if [ ! -f "$TEST_PROJECT_DIR/test-output.txt" ]; then
    echo "ERROR: test-output.txt was not created"
    exit 1
fi

# Read the output and verify ordering
mapfile -t ordered_resources < "$TEST_PROJECT_DIR/test-output.txt"

echo ""
echo "Ordered resources:"
for resource in "${ordered_resources[@]}"; do
    echo "  - $resource"
done

# Verify that HighPriorityPage.xaml comes first
first_resource="${ordered_resources[0]}"
if [[ ! "$first_resource" =~ "HighPriorityPage.xaml" ]]; then
    echo ""
    echo "ERROR: Expected HighPriorityPage.xaml to be first, but got: $first_resource"
    exit 1
fi

# Verify that other pages come after
has_page1=false
has_page2=false
has_page3=false

for i in "${!ordered_resources[@]}"; do
    if [ $i -eq 0 ]; then
        continue  # Skip the first one (already checked)
    fi
    
    resource="${ordered_resources[$i]}"
    if [[ "$resource" =~ "Page1.xaml" ]]; then
        has_page1=true
    elif [[ "$resource" =~ "Page2.xaml" ]]; then
        has_page2=true
    elif [[ "$resource" =~ "Page3.xaml" ]]; then
        has_page3=true
    fi
done

if [ "$has_page1" = false ] || [ "$has_page2" = false ] || [ "$has_page3" = false ]; then
    echo ""
    echo "ERROR: Not all pages were found in the ordered resources"
    exit 1
fi

echo ""
echo "✓ Test passed: HighPriorityPage.xaml is first, followed by other pages"
exit 0
