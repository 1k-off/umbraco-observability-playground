/**
 * Seed slugs use zero-padded numbers (article-001) while Delivery API item paths
 * use Umbraco route segments (article-1). Map property slug → delivery path.
 */
export function slugToDeliveryItemPath(slug: string): string {
  const match = /^article-(\d+)$/.exec(slug);
  if (!match) {
    return slug;
  }

  const n = Number.parseInt(match[1], 10);
  if (Number.isNaN(n)) {
    return slug;
  }

  return `article-${n}`;
}
