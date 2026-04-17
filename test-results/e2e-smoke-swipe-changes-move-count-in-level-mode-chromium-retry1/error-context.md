# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: e2e/smoke.spec.ts >> swipe changes move count in level mode
- Location: tests/e2e/smoke.spec.ts:88:5

# Error details

```
TimeoutError: page.waitForFunction: Timeout 5000ms exceeded.
```

# Page snapshot

```yaml
- generic [ref=e5]: FinalNumber
```

# Test source

```ts
  1  | import { Page, expect } from '@playwright/test';
  2  | 
  3  | /** Shape of window.gameState pushed by TestBridge.cs */
  4  | export interface GameState {
  5  |   screen: 'home' | 'worldSelect' | 'levelSelect' | 'game';
  6  |   overlay: 'none' | 'win' | 'lose';
  7  |   score: number;
  8  |   moves: number;
  9  |   par: number;
  10 |   target: number;
  11 |   infinite: boolean;
  12 | }
  13 | 
  14 | /**
  15 |  * Wait for the Unity WebGL player to finish loading.
  16 |  * Detects completion by checking that the loading bar is hidden
  17 |  * and window.gameState is available.
  18 |  */
  19 | export async function waitForUnityLoad(page: Page, timeoutMs = 45_000) {
  20 |   // Wait for the canvas to be visible
  21 |   await page.locator('#unity-canvas').waitFor({ state: 'visible', timeout: timeoutMs });
  22 | 
  23 |   // Wait for Unity to set window.gameState (TestBridge starts pushing in Start)
  24 |   await page.waitForFunction(
  25 |     () => (window as any).gameState?.screen != null,
  26 |     { timeout: timeoutMs },
  27 |   );
  28 | }
  29 | 
  30 | /** Read the current game state from the JS bridge. */
  31 | export async function getGameState(page: Page): Promise<GameState> {
  32 |   return page.evaluate(() => (window as any).gameState as GameState);
  33 | }
  34 | 
  35 | /** Wait until gameState.screen equals the expected value. */
  36 | export async function waitForScreen(page: Page, screen: GameState['screen'], timeoutMs = 15_000) {
> 37 |   await page.waitForFunction(
     |              ^ TimeoutError: page.waitForFunction: Timeout 5000ms exceeded.
  38 |     (s) => (window as any).gameState?.screen === s,
  39 |     screen,
  40 |     { timeout: timeoutMs },
  41 |   );
  42 | }
  43 | 
  44 | /** Wait until gameState.overlay equals the expected value. */
  45 | export async function waitForOverlay(page: Page, overlay: GameState['overlay'], timeoutMs = 15_000) {
  46 |   await page.waitForFunction(
  47 |     (o) => (window as any).gameState?.overlay === o,
  48 |     overlay,
  49 |     { timeout: timeoutMs },
  50 |   );
  51 | }
  52 | 
  53 | /**
  54 |  * Simulate a swipe gesture on the Unity canvas.
  55 |  * Drags from centre in the given direction over 150ms.
  56 |  */
  57 | export async function swipe(page: Page, direction: 'up' | 'down' | 'left' | 'right') {
  58 |   const canvas = page.locator('#unity-canvas');
  59 |   const box = await canvas.boundingBox();
  60 |   if (!box) throw new Error('Canvas not found');
  61 | 
  62 |   const cx = box.x + box.width / 2;
  63 |   const cy = box.y + box.height / 2;
  64 |   const dist = Math.min(box.width, box.height) * 0.25;
  65 | 
  66 |   const offsets: Record<string, { dx: number; dy: number }> = {
  67 |     up:    { dx: 0, dy: -dist },
  68 |     down:  { dx: 0, dy: dist },
  69 |     left:  { dx: -dist, dy: 0 },
  70 |     right: { dx: dist, dy: 0 },
  71 |   };
  72 |   const { dx, dy } = offsets[direction];
  73 | 
  74 |   await page.mouse.move(cx, cy);
  75 |   await page.mouse.down();
  76 |   await page.mouse.move(cx + dx, cy + dy, { steps: 5 });
  77 |   await page.mouse.up();
  78 | }
  79 | 
  80 | /**
  81 |  * Click at a proportional position on the canvas.
  82 |  * (0.5, 0.5) = centre, (0.5, 0.8) = bottom-centre, etc.
  83 |  */
  84 | export async function clickCanvas(page: Page, xFrac: number, yFrac: number) {
  85 |   const canvas = page.locator('#unity-canvas');
  86 |   const box = await canvas.boundingBox();
  87 |   if (!box) throw new Error('Canvas not found');
  88 | 
  89 |   await page.mouse.click(
  90 |     box.x + box.width * xFrac,
  91 |     box.y + box.height * yFrac,
  92 |   );
  93 | }
  94 | 
```