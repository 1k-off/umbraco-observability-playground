import { z } from "zod";

const serverSchema = z.object({
  CMS_BASE_URL: z.string().url(),
});

const publicSchema = z.object({
  NEXT_PUBLIC_SITE_URL: z.string().url().optional(),
});

export type ServerEnv = z.infer<typeof serverSchema>;

let cached: ServerEnv | null = null;

export function getServerEnv(): ServerEnv {
  if (cached) {
    return cached;
  }

  const parsed = serverSchema.safeParse({
    CMS_BASE_URL: process.env.CMS_BASE_URL ?? "http://localhost:8080",
  });

  if (!parsed.success) {
    throw new Error(
      `Invalid server environment: ${parsed.error.flatten().fieldErrors.CMS_BASE_URL?.join(", ") ?? parsed.error.message}`,
    );
  }

  cached = parsed.data;
  return parsed.data;
}

export function getPublicSiteUrl(): string {
  const parsed = publicSchema.safeParse({
    NEXT_PUBLIC_SITE_URL: process.env.NEXT_PUBLIC_SITE_URL,
  });

  if (parsed.success && parsed.data.NEXT_PUBLIC_SITE_URL) {
    return parsed.data.NEXT_PUBLIC_SITE_URL;
  }

  return "http://localhost:3000";
}

/** Validates env at build time (import from next.config). */
export function assertBuildEnv(): void {
  getServerEnv();
}
