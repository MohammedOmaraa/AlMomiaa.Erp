import type { Config } from "tailwindcss";
import preset from "@almomiaa/ui/tailwind.preset";

const config: Config = {
  presets: [preset],
  content: [
    "./src/**/*.{js,ts,jsx,tsx,mdx}",
    "../../packages/ui-kit/src/**/*.{ts,tsx}",
    "../../packages/ui-kit/dist/**/*.js",
  ],
  plugins: [],
};

export default config;
