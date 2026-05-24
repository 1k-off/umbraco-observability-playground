import { trace } from "@opentelemetry/api";

export type LogLevel = "info" | "warn" | "error";

type LogFields = Record<string, string | number | boolean | undefined>;

function write(level: LogLevel, msg: string, fields?: LogFields): void {
  const span = trace.getActiveSpan();
  const ctx = span?.spanContext();

  const line = JSON.stringify({
    level,
    msg,
    time: new Date().toISOString(),
    service: process.env.OTEL_SERVICE_NAME ?? "umbraco-web",
    ...(ctx?.traceId && { trace_id: ctx.traceId, span_id: ctx.spanId }),
    ...fields,
  });

  if (level === "error") {
    console.error(line);
  } else if (level === "warn") {
    console.warn(line);
  } else {
    console.log(line);
  }
}

export const log = {
  info: (msg: string, fields?: LogFields) => write("info", msg, fields),
  warn: (msg: string, fields?: LogFields) => write("warn", msg, fields),
  error: (msg: string, fields?: LogFields) => write("error", msg, fields),
};
