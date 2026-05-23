export function SiteFooter({ siteName, tagline }: { siteName: string; tagline: string }) {
  return (
    <footer className="mt-auto border-t py-8">
      <div className="mx-auto max-w-5xl px-4 text-center text-sm text-muted-foreground">
        <p className="font-medium text-foreground">{siteName}</p>
        {tagline ? <p className="mt-1">{tagline}</p> : null}
      </div>
    </footer>
  );
}
