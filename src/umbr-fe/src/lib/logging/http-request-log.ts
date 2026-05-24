import type { IncomingMessage, ServerResponse } from "http";
import { SpanKind, type Span } from "@opentelemetry/api";

import { log } from "@/lib/logging/logger";
import { isNoisePath } from "@/lib/logging/paths";

function requestPath(request: IncomingMessage): string {
  const raw = request.url ?? "/";
  try {
    return new URL(raw, "http://localhost").pathname;
  } catch {
    return raw.split("?")[0] ?? raw;
  }
}

export function shouldIgnoreIncomingRequest(request: IncomingMessage): boolean {
  return isNoisePath(requestPath(request));
}

function spanAttribute(span: Span, key: string): string | undefined {
  const attrs = (span as { attributes?: Record<string, unknown> }).attributes;
  const value = attrs?.[key];
  return typeof value === "string" ? value : undefined;
}

export function logHttpResponse(span: Span, response: IncomingMessage | ServerResponse): void {
  const kind = (span as { kind?: SpanKind }).kind;
  if (kind !== undefined && kind !== SpanKind.SERVER) {
    return;
  }

  const method = spanAttribute(span, "http.method");
  const route =
    spanAttribute(span, "http.route") ??
    spanAttribute(span, "http.target") ??
    spanAttribute(span, "url.path");

  if (route && isNoisePath(route)) {
    return;
  }

  const statusCode =
    "statusCode" in response && typeof response.statusCode === "number"
      ? response.statusCode
      : undefined;

  log.info("HTTP request", {
    "http.method": method,
    "http.route": route,
    ...(statusCode !== undefined && { "http.status_code": statusCode }),
  });
}
