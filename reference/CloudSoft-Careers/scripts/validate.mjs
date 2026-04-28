// Validates the deployed CloudCiCareers.Web by walking the applicant + recruiter flow:
//   1. Load home, capture the job listing.
//   2. Click "Apply" on the first job, capture the apply form.
//   3. Submit name + email + a small valid PDF, capture the redirect (Details page).
//   4. Visit /Applications, capture the recruiter listing.
//   5. Click into Details, capture the status-edit page.
//   6. Submit a non-PDF (text bytes named cv.pdf), capture the validation error.
//
// Saves screenshots into ../docs/screenshots/.
//
// Usage:
//   FQDN=ca-careers-week7.<env>.northeurope.azurecontainerapps.io \
//   node scripts/validate.mjs

import { chromium } from "playwright";
import { mkdirSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const screenshotDir = join(here, "..", "docs", "screenshots");
mkdirSync(screenshotDir, { recursive: true });

const fqdn = process.env.FQDN;
if (!fqdn) {
  console.error("Set FQDN env var (e.g., FQDN=ca-careers-week7.<env>.northeurope.azurecontainerapps.io)");
  process.exit(2);
}

const baseUrl = `https://${fqdn}`;

// Minimal valid PDF — passes the %PDF magic-bytes check and parses in real viewers.
const minimalPdf = Buffer.from([
  0x25, 0x50, 0x44, 0x46, 0x2d, 0x31, 0x2e, 0x34, 0x0a,           // %PDF-1.4\n
  ...Buffer.from("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n"),
  ...Buffer.from("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n"),
  ...Buffer.from("3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 100 100] >>\nendobj\n"),
  ...Buffer.from("xref\n0 4\n0000000000 65535 f\n0000000010 00000 n\n0000000056 00000 n\n0000000111 00000 n\n"),
  ...Buffer.from("trailer\n<< /Size 4 /Root 1 0 R >>\nstartxref\n173\n%%EOF\n"),
]);

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1400, height: 1000 } });

console.log(`Loading ${baseUrl}/`);
let response = await page.goto(baseUrl, { waitUntil: "networkidle", timeout: 30000 });
console.log(`HTTP ${response.status()}`);
if (response.status() !== 200) {
  console.error(`Expected HTTP 200 from /, got ${response.status()}`);
  await browser.close();
  process.exit(1);
}

await page.waitForSelector(".card-title", { timeout: 15000 });
const jobsShot = join(screenshotDir, "week-7-jobs.png");
await page.screenshot({ path: jobsShot, fullPage: true });
console.log(`Saved ${jobsShot}`);

// Click the first Apply button
await page.locator(".btn.btn-primary", { hasText: "Apply" }).first().click();
await page.waitForSelector("input[name='Name']", { timeout: 10000 });
const applyShot = join(screenshotDir, "week-7-apply-form.png");
await page.screenshot({ path: applyShot, fullPage: true });
console.log(`Saved ${applyShot}`);

// Fill and submit valid form
await page.fill("input[name='Name']", "Sigrid Larsson");
await page.fill("input[name='Email']", "sigrid@example.com");
await page.setInputFiles("input[name='cv']", {
  name: "cv.pdf",
  mimeType: "application/pdf",
  buffer: minimalPdf,
});
await page.click("button[type='submit']");

// Wait for redirect to Details
await page.waitForURL(/\/Applications\/Details\//, { timeout: 20000 });
await page.waitForSelector(".alert-success", { timeout: 10000 });
const thanksShot = join(screenshotDir, "week-7-application-thanks.png");
await page.screenshot({ path: thanksShot, fullPage: true });
console.log(`Saved ${thanksShot}`);

// Recruiter listing
await page.goto(`${baseUrl}/Applications`, { waitUntil: "networkidle" });
await page.waitForSelector("table.table tbody tr", { timeout: 10000 });
const listingShot = join(screenshotDir, "week-7-listing.png");
await page.screenshot({ path: listingShot, fullPage: true });
console.log(`Saved ${listingShot}`);

// Open the first row's detail page
await page.locator("table.table tbody tr a", { hasText: "Open" }).first().click();
await page.waitForSelector("select[name='newStatus']", { timeout: 10000 });
const detailShot = join(screenshotDir, "week-7-detail.png");
await page.screenshot({ path: detailShot, fullPage: true });
console.log(`Saved ${detailShot}`);

// Validation-error capture: go back, submit a non-PDF
await page.goto(baseUrl, { waitUntil: "networkidle" });
await page.locator(".btn.btn-primary", { hasText: "Apply" }).first().click();
await page.waitForSelector("input[name='Name']", { timeout: 10000 });
await page.fill("input[name='Name']", "Test Notpdf");
await page.fill("input[name='Email']", "notpdf@example.com");
await page.setInputFiles("input[name='cv']", {
  name: "fake.pdf",
  mimeType: "application/pdf",
  buffer: Buffer.from("This is not a PDF — just plain text renamed.\n"),
});
await page.click("button[type='submit']");
await page.waitForSelector(".text-danger", { timeout: 10000 });
const errorShot = join(screenshotDir, "week-7-validation-error.png");
await page.screenshot({ path: errorShot, fullPage: true });
console.log(`Saved ${errorShot}`);

await browser.close();
console.log("OK");
