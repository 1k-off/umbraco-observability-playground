import { ArticleGridSkeleton } from "@/components/articles/article-grid-skeleton";
import { Skeleton } from "@/components/ui/skeleton";

export default function ArticlesLoading() {
  return (
    <div className="mx-auto max-w-5xl px-4 py-10">
      <header className="mb-8 space-y-4">
        <Skeleton className="h-9 w-48" />
        <Skeleton className="h-4 w-64" />
        <div className="flex flex-wrap gap-2">
          {Array.from({ length: 6 }, (_, i) => (
            <Skeleton key={i} className="h-7 w-24" />
          ))}
        </div>
      </header>
      <ArticleGridSkeleton count={12} />
      <Skeleton className="mx-auto mt-10 h-9 w-64" />
    </div>
  );
}
