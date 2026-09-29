import asyncio, sys
from playwright.async_api import async_playwright

async def main():
    async with async_playwright() as p:
        b = await p.chromium.launch(args=["--use-gl=swiftshader", "--enable-webgl", "--ignore-gpu-blocklist"])
        pg = await b.new_page(viewport={"width": 1440, "height": 860})
        errs = []
        pg.on("console", lambda m: errs.append(f"[{m.type}] {m.text}") if m.type in ("error", "warning") else None)
        pg.on("pageerror", lambda e: errs.append(f"[pageerror] {e}"))
        await pg.goto("http://localhost:3000", wait_until="networkidle")
        await pg.wait_for_timeout(12000)
        await pg.screenshot(path="/home/ubuntu/qa/1menu.png")
        await pg.keyboard.press("Enter")
        await pg.wait_for_timeout(3000)
        await pg.screenshot(path="/home/ubuntu/qa/2game.png")
        await pg.keyboard.down("ArrowRight"); await pg.wait_for_timeout(4000); await pg.keyboard.up("ArrowRight")
        await pg.mouse.move(1000, 450)
        for k in "QWE":
            await pg.keyboard.press(k); await pg.wait_for_timeout(300)
        await pg.mouse.down(); await pg.wait_for_timeout(1500); await pg.mouse.up()
        await pg.screenshot(path="/home/ubuntu/qa/3fight.png")
        await pg.keyboard.press("p"); await pg.wait_for_timeout(500)
        await pg.screenshot(path="/home/ubuntu/qa/4shop.png")
        await pg.keyboard.press("p")
        await pg.keyboard.press("Escape"); await pg.wait_for_timeout(500)
        await pg.screenshot(path="/home/ubuntu/qa/5pause.png")
        await pg.keyboard.press("Escape")
        await pg.keyboard.down("ArrowRight"); await pg.wait_for_timeout(9000); await pg.keyboard.up("ArrowRight")
        await pg.mouse.down(); await pg.wait_for_timeout(6000); await pg.mouse.up()
        await pg.screenshot(path="/home/ubuntu/qa/6later.png")
        print("\n".join(errs[:40]))
        await b.close()

asyncio.run(main())
