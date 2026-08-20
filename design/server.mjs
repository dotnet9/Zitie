// 效果图预览服务器：node design/server.mjs（任意目录均可执行）
// 页面：http://127.0.0.1:8399/design/mockups/home.html 与 editor.html
import { createServer } from "node:http";
import { readFile } from "node:fs/promises";
import { dirname, extname, join, normalize } from "node:path";
import { fileURLToPath } from "node:url";

// 仓库根目录 = 本脚本所在 design 目录的上一级（页面需要引用 src 下的字体）
const root = join(dirname(fileURLToPath(import.meta.url)), "..");
const types = {
    ".html": "text/html; charset=utf-8",
    ".css": "text/css; charset=utf-8",
    ".js": "text/javascript; charset=utf-8",
    ".mjs": "text/javascript; charset=utf-8",
    ".ttf": "font/ttf",
    ".png": "image/png",
    ".svg": "image/svg+xml",
};

createServer(async (req, res) => {
    try {
        const url = decodeURIComponent(new URL(req.url, "http://localhost").pathname);
        const file = normalize(join(root, url));
        if (!file.startsWith(root)) throw new Error("forbidden");
        const data = await readFile(file);
        res.writeHead(200, { "Content-Type": types[extname(file)] ?? "application/octet-stream" });
        res.end(data);
    } catch {
        res.writeHead(404);
        res.end("not found");
    }
}).listen(8399, "127.0.0.1", () => console.log("design server on http://127.0.0.1:8399"));
