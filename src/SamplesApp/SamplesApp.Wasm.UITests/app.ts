import { TestRunner } from "./TestRunner";

const puppeteer = require('puppeteer');
const path = require("path");
const fs = require('fs');

(async () => {
	console.log(`Init puppeteer`);
	const browser = await puppeteer.launch({
		"headless": false,
		args: ['--no-sandbox', '--disable-setuid-sandbox'],
		"defaultViewport": { "width": 1280, "height": 1024 }
	});

	const page = await browser.newPage();
	await page.goto("http://localhost:8000/");

	var runner = new TestRunner(page);
	await runner.runTests();

	console.log(`Tests done`);

	await browser.close();
})();
