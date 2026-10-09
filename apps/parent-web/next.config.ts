import type { NextConfig } from "next";

/**
 * Same-origin rewrite so the browser never hits localhost:5137 cross-origin.
 * Client calls `/api/platform/...`.
 */
const apiUpstream =
  process.env.NEXT_PUBLIC_API_BASE_URL?.replace(/\/$/, "") ||
  "http://localhost:5137";

const nextConfig: NextConfig = {
  transpilePackages: ["@fee/ui", "@fee/money", "@fee/validation", "@fee/api-client"],
  async rewrites() {
    return [
      {
        source: "/api/platform/:path*",
        destination: `${apiUpstream}/:path*`,
      },
    ];
  },
};

export default nextConfig;
