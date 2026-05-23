import Link from "next/link";

import { ArticleCard } from "@/components/articles/article-card";
import { Button } from "@/components/ui/button";
import { getLatestArticles, getSiteSettings } from "@/lib/cms/client";

export const revalidate = 60;

export default async function HomePage() {
  const [settings, articles] = await Promise.all([getSiteSettings(), getLatestArticles()]);

  return (
    <div className="mx-auto max-w-5xl px-4 py-10">
      <section className="mb-12 space-y-4">
        <p className="text-sm font-medium uppercase tracking-wide text-muted-foreground">Tech Blog Demo</p>
        <h1 className="text-4xl font-bold tracking-tight">{settings.siteName}</h1>
        <p className="max-w-2xl text-lg text-muted-foreground">{settings.tagline}</p>
        <Button nativeButton={false} render={<Link href="/articles" />}>
          Browse all articles
        </Button>
      </section>

      <section>
        <div className="mb-6 flex items-end justify-between gap-4">
          <h2 className="text-2xl font-semibold">Latest articles</h2>
          <Link href="/articles" className="text-sm text-primary hover:underline">
            View all
          </Link>
        </div>
        <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
          {articles.map((article) => (
            <ArticleCard key={article.id} article={article} />
          ))}
        </div>
      </section>
    </div>
  );
}
