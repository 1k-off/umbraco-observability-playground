import Link from "next/link";

import { Button } from "@/components/ui/button";
import type { Category } from "@/lib/cms/types";

export function CategoryFilter({
  categories,
  activeSlug,
}: {
  categories: Category[];
  activeSlug: string | null;
}) {
  return (
    <div className="flex flex-wrap gap-2">
      <Button
        variant={activeSlug ? "outline" : "default"}
        size="sm"
        nativeButton={false}
        render={<Link href="/articles" />}
      >
        All
      </Button>
      {categories.map((cat) => (
        <Button
          key={cat.id}
          variant={activeSlug === cat.slug ? "default" : "outline"}
          size="sm"
          nativeButton={false}
          render={<Link href={`/articles?category=${encodeURIComponent(cat.slug)}`} />}
        >
          {cat.name}
        </Button>
      ))}
    </div>
  );
}
