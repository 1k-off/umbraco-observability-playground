import Link from "next/link";
import { Suspense } from "react";

import { SearchForm } from "@/components/search/search-form";
import { Skeleton } from "@/components/ui/skeleton";
import { searchArticles } from "@/lib/cms/client";

export const dynamic = "force-dynamic";

type PageProps = {
  searchParams: Promise<{ q?: string; page?: string }>;
};

function SearchFormFallback() {
  return (
    <div className="flex gap-2">
      <Skeleton className="h-9 max-w-md flex-1" />
      <Skeleton className="h-9 w-20" />
    </div>
  );
}

async function SearchResults({ query, page }: { query: string; page: number }) {
  const result = await searchArticles(query, page, 10);

  if (result.items.length === 0) {
    return (
      <p className="text-muted-foreground">
        No results for <span className="font-medium text-foreground">&ldquo;{query}&rdquo;</span>.
      </p>
    );
  }

  return (
    <div className="space-y-6">
      <p className="text-sm text-muted-foreground">
        {result.total} result{result.total === 1 ? "" : "s"} for &ldquo;{query}&rdquo;
      </p>
      <ul className="divide-y rounded-lg border">
        {result.items.map((item) => (
          <li key={item.id} className="px-4 py-3">
            {item.slug ? (
              <Link href={`/articles/${item.slug}`} className="font-medium hover:underline">
                {item.name ?? item.slug}
              </Link>
            ) : (
              <span>{item.name ?? "Untitled"}</span>
            )}
            {item.slug ? <p className="text-xs text-muted-foreground">{item.slug}</p> : null}
          </li>
        ))}
      </ul>
    </div>
  );
}

export default async function SearchPage({ searchParams }: PageProps) {
  const params = await searchParams;
  const query = params.q?.trim() ?? "";
  const page = Math.max(1, Number.parseInt(params.page ?? "1", 10) || 1);

  return (
    <div className="mx-auto max-w-3xl px-4 py-10">
      <h1 className="mb-6 text-3xl font-bold tracking-tight">Search</h1>

      <Suspense fallback={<SearchFormFallback />}>
        <SearchForm initialQuery={query} />
      </Suspense>

      <div className="mt-10">
        {query ? (
          <SearchResults query={query} page={page} />
        ) : (
          <p className="text-muted-foreground">Enter a keyword to search articles (Examine index on the CMS).</p>
        )}
      </div>
    </div>
  );
}
