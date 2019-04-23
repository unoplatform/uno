import { TestRunner } from "./TestRunner";

const puppeteer = require('puppeteer');
const path = require("path");
const fs = require('fs');

(async () => {
	console.log(`Init puppeteer`);
	const browser = await puppeteer.launch({
		"headless": true,
		args: ['--no-sandbox', '--disable-setuid-sandbox'],
		"defaultViewport": { "width": 1280, "height": 1024 }
	});

	const page = await browser.newPage();
	page.on('console', msg => {
		console.log('BROWSER LOG:', msg.text());
	});
	page.on('requestfailed', err => console.error('BROWSER-REQUEST-FAILED:', err))
	await page.goto("http://localhost:8000/");

	var runner = new TestRunner(page);
	await runner.runTests();

	console.log(`Tests done`);

	await browser.close();
})();
