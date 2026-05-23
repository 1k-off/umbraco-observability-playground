import { NextRequest, NextResponse } from "next/server";

const alloyFaroBase =
  process.env.FARO_COLLECTOR_UPSTREAM ?? "http://alloy-gateway.observability.svc.cluster.local:12347";

async function proxy(request: NextRequest, pathSegments: string[] | undefined) {
  // Faro SDK posts to the collector URL; Alloy faro.receiver serves /collect.
  const suffix = pathSegments?.length ? `/${pathSegments.join("/")}` : "/collect";
  const target = new URL(`${alloyFaroBase.replace(/\/$/, "")}${suffix}`);

  request.nextUrl.searchParams.forEach((value, key) => {
    target.searchParams.set(key, value);
  });

  const headers = new Headers();
  const contentType = request.headers.get("content-type");
  if (contentType) {
    headers.set("content-type", contentType);
  }

  const body =
    request.method === "GET" || request.method === "HEAD" ? undefined : await request.arrayBuffer();

  const upstream = await fetch(target, {
    method: request.method,
    headers,
    body,
  });

  return new NextResponse(upstream.body, {
    status: upstream.status,
    headers: {
      "content-type": upstream.headers.get("content-type") ?? "application/json",
    },
  });
}

export async function GET(request: NextRequest, context: { params: Promise<{ path?: string[] }> }) {
  const { path } = await context.params;
  return proxy(request, path);
}

export async function POST(request: NextRequest, context: { params: Promise<{ path?: string[] }> }) {
  const { path } = await context.params;
  return proxy(request, path);
}

export async function OPTIONS() {
  return new NextResponse(null, {
    status: 204,
    headers: {
      "Access-Control-Allow-Origin": "*",
      "Access-Control-Allow-Methods": "GET, POST, OPTIONS",
      "Access-Control-Allow-Headers": "Content-Type",
    },
  });
}
