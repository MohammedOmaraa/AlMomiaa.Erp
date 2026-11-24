import type { Config } from "tailwindcss";
import preset from "@almomiaa/ui/tailwind.preset";

const config: Config = {
  presets: [preset],
  content: [
    "./index.html",
    "./src/**/*.{ts,tsx}",
    "../../packages/ui-kit/src/**/*.{ts,tsx}",
    "../../packages/ui-kit/dist/**/*.js"
  ],
  plugins: []
};

export default config;
