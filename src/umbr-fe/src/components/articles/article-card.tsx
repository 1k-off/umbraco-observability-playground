import Image from "next/image";
import Link from "next/link";

import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import type { ArticleSummary } from "@/lib/cms/types";

function formatDate(iso: string | null): string {
  if (!iso) {
    return "";
  }

  return new Date(iso).toLocaleDateString("en-GB", {
    day: "numeric",
    month: "short",
    year: "numeric",
  });
}

export function ArticleCard({ article }: { article: ArticleSummary }) {
  return (
    <Card className="overflow-hidden pt-0">
      {article.imageUrl ? (
        <div className="relative aspect-[16/9] w-full bg-muted">
          <Image
            src={article.imageUrl}
            alt=""
            fill
            className="object-cover"
            sizes="(max-width: 768px) 100vw, 33vw"
          />
        </div>
      ) : null}
      <CardHeader>
        <CardTitle className="line-clamp-2 text-lg">
          <Link href={`/articles/${article.slug}`} className="hover:underline">
            {article.title}
          </Link>
        </CardTitle>
        <CardDescription className="flex flex-wrap gap-2 text-xs">
          {article.categoryName ? <span>{article.categoryName}</span> : null}
          {article.publishDate ? <span>{formatDate(article.publishDate)}</span> : null}
        </CardDescription>
      </CardHeader>
      <CardContent>
        <p className="line-clamp-3 text-sm text-muted-foreground">{article.excerpt}</p>
      </CardContent>
      <CardFooter>
        <Link href={`/articles/${article.slug}`} className="text-sm font-medium text-primary hover:underline">
          Read more
        </Link>
      </CardFooter>
    </Card>
  );
}
