import Image from "next/image";
import Link from "next/link";
import { notFound } from "next/navigation";

import { getArticleBySlug } from "@/lib/cms/client";

export const revalidate = 300;

type PageProps = {
  params: Promise<{ slug: string }>;
};

function formatDate(iso: string | null): string {
  if (!iso) {
    return "";
  }

  return new Date(iso).toLocaleDateString("en-GB", {
    day: "numeric",
    month: "long",
    year: "numeric",
  });
}

export default async function ArticleDetailPage({ params }: PageProps) {
  const { slug } = await params;
  const article = await getArticleBySlug(slug);

  if (!article) {
    notFound();
  }

  return (
    <article className="mx-auto max-w-3xl px-4 py-10">
      {article.imageUrl ? (
        <div className="relative mb-8 aspect-[16/9] overflow-hidden rounded-xl bg-muted">
          <Image src={article.imageUrl} alt="" fill className="object-cover" priority sizes="(max-width: 768px) 100vw, 768px" />
        </div>
      ) : null}

      <header className="mb-8 space-y-3">
        <p className="text-sm text-muted-foreground">
          {article.categorySlug ? (
            <Link href={`/articles?category=${encodeURIComponent(article.categorySlug)}`} className="hover:underline">
              {article.categoryName}
            </Link>
          ) : null}
          {article.publishDate ? (
            <>
              {article.categoryName ? " · " : null}
              <time dateTime={article.publishDate}>{formatDate(article.publishDate)}</time>
            </>
          ) : null}
        </p>
        <h1 className="text-4xl font-bold tracking-tight">{article.title}</h1>
        {article.authorName ? (
          <p className="text-muted-foreground">
            By <span className="font-medium text-foreground">{article.authorName}</span>
          </p>
        ) : null}
      </header>

      <div
        className="prose prose-neutral max-w-none dark:prose-invert"
        dangerouslySetInnerHTML={{ __html: article.bodyHtml }}
      />

      <footer className="mt-12 border-t pt-6">
        <Link href="/articles" className="text-sm text-primary hover:underline">
          ← Back to articles
        </Link>
      </footer>
    </article>
  );
}
