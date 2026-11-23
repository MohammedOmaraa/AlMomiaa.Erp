import { copyFile, mkdir } from "node:fs/promises";
await mkdir(new URL("../dist/assets/", import.meta.url), { recursive: true });
await copyFile(new URL("../src/assets/almomiaa-logo.jpg", import.meta.url), new URL("../dist/assets/almomiaa-logo.jpg", import.meta.url));
