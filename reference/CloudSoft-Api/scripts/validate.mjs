// Validates the deployed CloudCiApi by:
//   1. Loading /swagger and capturing the operations layout (three quotes operations
//      + tokens login + the Authorize button bound to the Bearer scheme).
//   2. Clicking Authorize, pasting a JWT (from $TOKEN), expanding GET /api/quotes,
//      clicking Try it out + Execute, capturing the 200 response screenshot.
//
// Saves both screenshots into ../docs/screenshots/.
//
// Usage:
//   FQDN=ca-api-week6.<env>.northeurope.azurecontainerapps.io \
//   TOKEN=eyJ... \
//   node scripts/validate.mjs

import { chromium } from "playwright";
import { mkdirSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const screenshotDir = join(here, "..", "docs", "screenshots");
mkdirSync(screenshotDir, { recursive: true });

const fqdn = process.env.FQDN;
const token = process.env.TOKEN;
if (!fqdn) {
  console.error("Set FQDN env var (e.g., FQDN=ca-api-week6.<env>.azurecontainerapps.io)");
  process.exit(2);
}
if (!token) {
  console.error("Set TOKEN env var (a fresh JWT issued by /api/tokens/login)");
  process.exit(2);
}

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1400, height: 1000 } });

const swaggerUrl = `https://${fqdn}/swagger/index.html`;
console.log(`Loading ${swaggerUrl}`);
const response = await page.goto(swaggerUrl, { waitUntil: "networkidle", timeout: 30000 });
console.log(`HTTP ${response.status()}`);
if (response.status() !== 200) {
  console.error(`Expected HTTP 200 from /swagger, got ${response.status()}`);
  await browser.close();
  process.exit(1);
}

// Wait for Swagger UI to render the operations.
await page.waitForSelector(".opblock", { timeout: 15000 });

const swaggerShot = join(screenshotDir, "week-6-swagger.png");
await page.screenshot({ path: swaggerShot, fullPage: true });
console.log(`Saved ${swaggerShot}`);

// Click Authorize, paste the token, click Authorize then Close.
await page.locator(".btn.authorize").first().click();
// The Bearer scheme dialog renders a single text input; match anything that's
// inside the auth modal so we are robust to Swagger UI version changes.
await page.locator(".modal-ux-content input[type='text']").first().fill(token);
await page.locator(".modal-ux .auth-btn-wrapper .authorize").click();
await page.locator(".modal-ux .auth-btn-wrapper .btn-done").click();

// Expand the GET /api/quotes operation, click Try it out, then Execute.
const getOp = page.locator(".opblock.opblock-get").first();
await getOp.click();
await getOp.locator(".try-out__btn").click();
await getOp.locator(".execute").click();

// Wait for the response block to render with a 200.
await getOp.locator(".responses-table .response_current").waitFor({ timeout: 15000 });

// Capture the operation block (already expanded with the response below it).
const authShot = join(screenshotDir, "week-6-authorised-call.png");
await getOp.screenshot({ path: authShot });
console.log(`Saved ${authShot}`);

await browser.close();
console.log("OK");
