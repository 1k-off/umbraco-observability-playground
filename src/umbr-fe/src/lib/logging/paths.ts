/** Paths excluded from structured request logs and from noisy stderr. */
export function isNoisePath(pathname: string): boolean {
  if (pathname === "/api/health" || pathname === "/favicon.ico") {
    return true;
  }

  if (pathname.startsWith("/_next/")) {
    return true;
  }

  if (pathname.startsWith("/api/faro")) {
    return true;
  }

  return false;
}
