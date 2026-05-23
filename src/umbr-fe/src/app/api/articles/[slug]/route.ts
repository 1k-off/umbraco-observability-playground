import { getArticleBySlug } from "@/lib/cms/client";

type RouteContext = { params: Promise<{ slug: string }> };

export async function GET(_request: Request, context: RouteContext) {
  const { slug } = await context.params;

  try {
    const article = await getArticleBySlug(slug);

    if (!article) {
      return Response.json({ error: "Not found" }, { status: 404 });
    }

    return Response.json(article);
  } catch (error) {
    const message = error instanceof Error ? error.message : "CMS request failed";
    return Response.json({ error: message }, { status: 502 });
  }
}
