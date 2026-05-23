import { Suspense } from "react";

import { ArticleCard } from "@/components/articles/article-card";
import { ArticlesPagination } from "@/components/articles/articles-pagination";
import { CategoryFilter } from "@/components/articles/category-filter";
import { Skeleton } from "@/components/ui/skeleton";
import { getArticlesPage, getCategories } from "@/lib/cms/client";

export const revalidate = 60;

type PageProps = {
  searchParams: Promise<{ page?: string; category?: string }>;
};

function PaginationFallback() {
  return <Skeleton className="mx-auto h-9 w-64" />;
}

export default async function ArticlesPage({ searchParams }: PageProps) {
  const params = await searchParams;
  const page = Math.max(1, Number.parseInt(params.page ?? "1", 10) || 1);
  const categorySlug = params.category?.trim() || null;

  const [categories, listing] = await Promise.all([
    getCategories(),
    getArticlesPage({ page, categorySlug }),
  ]);

  const totalPages = Math.max(1, Math.ceil(listing.total / listing.pageSize));
  const activeCategory = categories.find((c) => c.slug === categorySlug);

  return (
    <div className="mx-auto max-w-5xl px-4 py-10">
      <header className="mb-8 space-y-4">
        <h1 className="text-3xl font-bold tracking-tight">Articles</h1>
        {activeCategory ? (
          <p className="text-muted-foreground">
            Category: <span className="font-medium text-foreground">{activeCategory.name}</span>
            {" · "}
            {listing.total} article{listing.total === 1 ? "" : "s"}
          </p>
        ) : (
          <p className="text-muted-foreground">{listing.total} articles from the CMS</p>
        )}
        <CategoryFilter categories={categories} activeSlug={categorySlug} />
      </header>

      {listing.articles.length === 0 ? (
        <p className="text-muted-foreground">No articles match this filter.</p>
      ) : (
        <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
          {listing.articles.map((article) => (
            <ArticleCard key={article.id} article={article} />
          ))}
        </div>
      )}

      <div className="mt-10">
        <Suspense fallback={<PaginationFallback />}>
          <ArticlesPagination page={listing.page} totalPages={totalPages} />
        </Suspense>
      </div>
    </div>
  );
}
