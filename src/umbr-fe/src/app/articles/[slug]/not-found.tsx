import Link from "next/link";

import { Button } from "@/components/ui/button";

export default function ArticleNotFound() {
  return (
    <div className="mx-auto max-w-lg px-4 py-16 text-center">
      <h1 className="text-2xl font-semibold">Article not found</h1>
      <p className="mt-2 text-muted-foreground">This slug does not match any published article.</p>
      <Button className="mt-6" nativeButton={false} render={<Link href="/articles" />}>
        Browse articles
      </Button>
    </div>
  );
}
