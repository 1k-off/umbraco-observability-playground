/**
 * Shared k6 env config. Defaults target the demo k8s Ingress hosts.
 */
export function cmsBaseUrl() {
  return (__ENV.K6_CMS_BASE_URL || 'https://demo-cms.1node.tech').replace(/\/$/, '');
}

export function webBaseUrl() {
  return (__ENV.K6_WEB_BASE_URL || 'https://demo-umbr-fe.1node.tech').replace(/\/$/, '');
}

/**
 * Map seed property slug (article-001) → Delivery API item path (article-1).
 */
export function slugToDeliveryItemPath(slug) {
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
