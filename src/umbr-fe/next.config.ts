import type { NextConfig } from "next";

import { assertBuildEnv, getServerEnv } from "./src/lib/env";

assertBuildEnv();

const cms = new URL(getServerEnv().CMS_BASE_URL);

const nextConfig: NextConfig = {
  output: "standalone",
  images: {
    remotePatterns: [
      {
        protocol: cms.protocol.replace(":", "") as "http" | "https",
        hostname: cms.hostname,
        port: cms.port || undefined,
        pathname: "/media/**",
      },
    ],
  },
};

export default nextConfig;
