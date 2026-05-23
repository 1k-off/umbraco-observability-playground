"use client";

import Link from "next/link";
import { useEffect } from "react";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";

export default function ArticleError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    toast.error("Could not load this article", {
      description: error.message,
    });
  }, [error]);

  return (
    <div className="mx-auto max-w-lg px-4 py-16 text-center">
      <h1 className="text-2xl font-semibold">Something went wrong</h1>
      <p className="mt-2 text-muted-foreground">We could not load this article. Try again or go back to the list.</p>
      <div className="mt-6 flex justify-center gap-3">
        <Button onClick={() => reset()}>Try again</Button>
        <Button variant="outline" nativeButton={false} render={<Link href="/articles" />}>
          All articles
        </Button>
      </div>
    </div>
  );
}
