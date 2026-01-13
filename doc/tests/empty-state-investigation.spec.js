const { test, expect } = require('@playwright/test');

test.describe('Empty State Investigation', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('http://localhost:3002/articles/intro.html');
    await page.waitForLoadState('networkidle');
  });

  test('should investigate empty state rendering', async ({ page }) => {
    console.log('\n=== Starting Investigation ===\n');
    
    // Wait for Pagefind to initialize
    await page.waitForTimeout(1000);
    
    // Get the search input
    const searchInput = page.locator('.pagefind-ui__search-input');
    await expect(searchInput).toBeVisible();
    console.log('✓ Search input is visible');
    
    // Focus the search input
    await searchInput.focus();
    console.log('✓ Search input focused');
    
    // Wait a bit for any JS to run
    await page.waitForTimeout(500);
    
    // Check if drawer exists
    const drawer = page.locator('.pagefind-ui__drawer');
    const drawerVisible = await drawer.isVisible();
    console.log(`Drawer visible: ${drawerVisible}`);
    
    if (drawerVisible) {
      // Check drawer classes
      const drawerClasses = await drawer.getAttribute('class');
      console.log(`Drawer classes: ${drawerClasses}`);
      
      // Check if drawer has hidden class
      const isHidden = drawerClasses.includes('pagefind-ui__hidden');
      console.log(`Drawer has hidden class: ${isHidden}`);
    }
    
    // Check for results area
    const resultsArea = page.locator('.pagefind-ui__results-area');
    const resultsAreaExists = await resultsArea.count();
    console.log(`Results area count: ${resultsAreaExists}`);
    
    if (resultsAreaExists > 0) {
      const resultsAreaVisible = await resultsArea.isVisible();
      console.log(`Results area visible: ${resultsAreaVisible}`);
      
      // Get the HTML of results area
      const resultsHTML = await resultsArea.innerHTML();
      console.log(`\nResults area HTML:\n${resultsHTML}\n`);
    }
    
    // Check for empty state
    const emptyState = page.locator('.search-empty-state');
    const emptyStateCount = await emptyState.count();
    console.log(`Empty state elements count: ${emptyStateCount}`);
    
    if (emptyStateCount > 0) {
      const emptyStateVisible = await emptyState.isVisible();
      console.log(`Empty state visible: ${emptyStateVisible}`);
      
      const emptyStateHTML = await emptyState.innerHTML();
      console.log(`\nEmpty state HTML:\n${emptyStateHTML}\n`);
    }
    
    // Check for pills
    const pills = page.locator('.search-history-pills');
    const pillsCount = await pills.count();
    console.log(`Pills elements count: ${pillsCount}`);
    
    if (pillsCount > 0) {
      const pillsVisible = await pills.isVisible();
      console.log(`Pills visible: ${pillsVisible}`);
    }
    
    // Get the entire drawer HTML for analysis
    if (drawerVisible) {
      const drawerHTML = await drawer.innerHTML();
      console.log(`\n=== Full Drawer HTML ===\n${drawerHTML}\n`);
    }
    
    // Take a screenshot for visual inspection
    await page.screenshot({ path: 'tests/screenshots/empty-state-investigation.png', fullPage: true });
    console.log('\n✓ Screenshot saved to tests/screenshots/empty-state-investigation.png');
    
    console.log('\n=== Investigation Complete ===\n');
  });
  
  test('should check if updatePillsContainer is being called', async ({ page }) => {
    console.log('\n=== Checking Function Calls ===\n');
    
    // Add console listener
    page.on('console', msg => {
      if (msg.text().includes('updatePillsContainer') || msg.text().includes('empty') || msg.text().includes('drawer')) {
        console.log(`Browser console: ${msg.text()}`);
      }
    });
    
    // Get the search input and focus it
    const searchInput = page.locator('.pagefind-ui__search-input');
    await searchInput.focus();
    
    await page.waitForTimeout(1000);
    
    // Check the drawer state via JavaScript
    const drawerState = await page.evaluate(() => {
      const drawer = document.querySelector('.pagefind-ui__drawer');
      const resultsArea = document.querySelector('.pagefind-ui__results-area');
      const emptyState = document.querySelector('.search-empty-state');
      
      return {
        drawerExists: !!drawer,
        drawerVisible: drawer ? window.getComputedStyle(drawer).display !== 'none' : false,
        drawerClasses: drawer ? drawer.className : 'N/A',
        resultsAreaExists: !!resultsArea,
        resultsAreaHTML: resultsArea ? resultsArea.innerHTML : 'N/A',
        emptyStateExists: !!emptyState,
        emptyStateParent: emptyState ? emptyState.parentElement.className : 'N/A'
      };
    });
    
    console.log('\n=== JavaScript Evaluation Results ===');
    console.log(JSON.stringify(drawerState, null, 2));
    console.log('\n=================================\n');
  });
});
