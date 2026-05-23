import Link from "next/link";

import { Button } from "@/components/ui/button";

const nav = [
  { href: "/", label: "Home" },
  { href: "/articles", label: "Articles" },
  { href: "/search", label: "Search" },
  { href: "/about", label: "About" },
];

export function SiteHeader({ siteName }: { siteName: string }) {
  return (
    <header className="border-b bg-card">
      <div className="mx-auto flex h-14 max-w-5xl items-center justify-between gap-4 px-4">
        <Link href="/" className="font-semibold tracking-tight">
          {siteName}
        </Link>
        <nav className="flex items-center gap-1" aria-label="Main">
          {nav.map((item) => (
            <Button key={item.href} variant="ghost" size="sm" nativeButton={false} render={<Link href={item.href} />}>
              {item.label}
            </Button>
          ))}
        </nav>
      </div>
    </header>
  );
}
