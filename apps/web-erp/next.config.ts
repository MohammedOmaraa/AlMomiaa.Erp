import type { NextConfig } from "next";

const backendOrigin = (process.env.BACKEND_API_ORIGIN ?? "http://localhost:5000").replace(/\/$/, "");

const nextConfig: NextConfig = {
  transpilePackages: ["@almomiaa/ui", "@almomiaa/api-client"],
  async rewrites() {
    return [
      {
        source: "/api/v1/:path*",
        destination: `${backendOrigin}/api/v1/:path*`,
      },
      {
        source: "/health",
        destination: `${backendOrigin}/health`,
      },
    ];
  },
};

export default nextConfig;
