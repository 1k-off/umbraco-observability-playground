import { getSiteSettings } from "@/lib/cms/client";

export const revalidate = 3600;

export default async function AboutPage() {
  const settings = await getSiteSettings();

  return (
    <div className="mx-auto max-w-3xl px-4 py-10">
      <h1 className="mb-6 text-3xl font-bold tracking-tight">About</h1>
      <div
        className="prose prose-neutral max-w-none dark:prose-invert"
        dangerouslySetInnerHTML={{ __html: settings.aboutHtml }}
      />
      {settings.contactEmail ? (
        <p className="mt-8 text-muted-foreground">
          Contact:{" "}
          <a href={`mailto:${settings.contactEmail}`} className="text-primary hover:underline">
            {settings.contactEmail}
          </a>
        </p>
      ) : null}
    </div>
  );
}
