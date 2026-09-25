import type { Config } from "tailwindcss";
import { feeTailwindTheme } from "@fee/ui/tailwind-preset";

const config: Config = {
  content: [
    "./src/**/*.{ts,tsx}",
    "../../packages/ui/src/**/*.{ts,tsx}",
  ],
  theme: feeTailwindTheme,
  plugins: [],
};

export default config;
