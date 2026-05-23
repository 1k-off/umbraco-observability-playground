import { NextRequest } from "next/server";

import { searchArticles } from "@/lib/cms/client";

export async function GET(request: NextRequest) {
  const { searchParams } = request.nextUrl;
  const q = searchParams.get("q")?.trim();

  if (!q) {
    return Response.json({ error: "Query parameter q is required." }, { status: 400 });
  }

  const page = Math.max(1, Number.parseInt(searchParams.get("page") ?? "1", 10) || 1);
  const pageSize = Math.min(50, Math.max(1, Number.parseInt(searchParams.get("pageSize") ?? "10", 10) || 10));

  try {
    const result = await searchArticles(q, page, pageSize);
    return Response.json(result);
  } catch (error) {
    const message = error instanceof Error ? error.message : "CMS search failed";
    return Response.json({ error: message }, { status: 502 });
  }
}
