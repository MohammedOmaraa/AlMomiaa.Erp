import type { Config } from "tailwindcss";
import preset from "../../packages/ui-kit/tailwind.preset";

const config: Config = {
  presets: [preset],
  content: ["./src/**/*.{ts,tsx,mdx}", "./.storybook/**/*.{ts,tsx}", "../../packages/ui-kit/src/**/*.{ts,tsx}"],
};
export default config;
