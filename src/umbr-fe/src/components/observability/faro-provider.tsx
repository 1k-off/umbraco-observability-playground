"use client";

import { getWebInstrumentations, initializeFaro } from "@grafana/faro-web-sdk";
import { TracingInstrumentation } from "@grafana/faro-web-tracing";
import { useEffect } from "react";

let faroInitialized = false;

function faroPropagationUrls(): (string | RegExp)[] {
  const patterns: (string | RegExp)[] = [/^\/api\//, /^\//];

  const site = process.env.NEXT_PUBLIC_SITE_URL;
  if (site) {
    try {
      patterns.push(new RegExp(`^${new URL(site).origin.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}`));
    } catch {
      // ignore invalid URL at build/runtime
    }
  }

  return patterns;
}

function initFaro(collectorUrl: string) {
  if (faroInitialized || typeof window === "undefined") {
    return;
  }

  initializeFaro({
    url: collectorUrl,
    app: {
      name: "umbraco-web",
      version: "0.1.0",
      environment: process.env.NEXT_PUBLIC_DEPLOYMENT_ENVIRONMENT ?? "demo",
    },
    instrumentations: [
      ...getWebInstrumentations(),
      new TracingInstrumentation({
        instrumentationOptions: {
          // RSC / client fetch to this origin carry traceparent for SSR continuation.
          propagateTraceHeaderCorsUrls: faroPropagationUrls(),
        },
      }),
    ],
  });

  faroInitialized = true;
}

export function FaroProvider({
  children,
  collectorUrl,
}: {
  children: React.ReactNode;
  collectorUrl: string;
}) {
  useEffect(() => {
    initFaro(collectorUrl);
  }, [collectorUrl]);

  return children;
}
