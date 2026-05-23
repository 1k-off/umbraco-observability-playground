import { NextRequest } from "next/server";

import { getArticlesPage } from "@/lib/cms/client";

export async function GET(request: NextRequest) {
  const { searchParams } = request.nextUrl;
  const skip = Number.parseInt(searchParams.get("skip") ?? "0", 10);
  const take = Number.parseInt(searchParams.get("take") ?? "10", 10);
  const category = searchParams.get("category");

  const page = Math.floor(skip / Math.max(take, 1)) + 1;

  try {
    const result = await getArticlesPage({
      page,
      pageSize: take,
      categorySlug: category,
    });

    return Response.json({
      total: result.total,
      skip,
      take: result.pageSize,
      items: result.articles,
    });
  } catch (error) {
    const message = error instanceof Error ? error.message : "CMS request failed";
    return Response.json({ error: message }, { status: 502 });
  }
}
