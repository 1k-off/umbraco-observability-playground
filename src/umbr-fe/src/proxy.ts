import { trace } from "@opentelemetry/api";
import type { NextRequest } from "next/server";
import { NextResponse } from "next/server";

/**
 * Forwards W3C trace context into the App Router (Faro adds traceparent on RSC fetch).
 * server-timing helps Grafana link browser ↔ backend when a span is active.
 */
export function proxy(_request: NextRequest) {
  const response = NextResponse.next();
  const span = trace.getActiveSpan();

  if (span) {
    const { traceId, spanId, traceFlags } = span.spanContext();
    const flags = traceFlags.toString(16).padStart(2, "0");
    response.headers.set(
      "server-timing",
      `traceparent;desc="00-${traceId}-${spanId}-${flags}"`,
    );
  }

  return response;
}

export const config = {
  matcher: ["/((?!_next/static|_next/image|favicon.ico|api/faro).*)"],
};
