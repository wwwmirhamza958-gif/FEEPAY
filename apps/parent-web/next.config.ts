import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  transpilePackages: ["@fee/ui", "@fee/money", "@fee/validation", "@fee/api-client"],
};

export default nextConfig;
