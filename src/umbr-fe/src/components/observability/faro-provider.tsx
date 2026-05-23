"use client";

import { getWebInstrumentations, initializeFaro } from "@grafana/faro-web-sdk";
import { TracingInstrumentation } from "@grafana/faro-web-tracing";
import { useEffect } from "react";

let faroInitialized = false;

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
    instrumentations: [...getWebInstrumentations(), new TracingInstrumentation()],
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
