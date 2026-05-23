"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { FormEvent, useState } from "react";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

export function SearchForm({ initialQuery = "" }: { initialQuery?: string }) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [query, setQuery] = useState(initialQuery);

  function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const trimmed = query.trim();
    const params = new URLSearchParams(searchParams.toString());

    if (trimmed) {
      params.set("q", trimmed);
    } else {
      params.delete("q");
    }

    params.delete("page");
    const qs = params.toString();
    router.push(qs ? `/search?${qs}` : "/search");
  }

  return (
    <form onSubmit={onSubmit} className="flex gap-2" role="search">
      <Input
        type="search"
        name="q"
        placeholder="Search articles…"
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        aria-label="Search query"
        className="max-w-md flex-1"
      />
      <Button type="submit">Search</Button>
    </form>
  );
}
