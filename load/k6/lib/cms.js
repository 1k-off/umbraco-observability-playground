import http from 'k6/http';
import { cmsBaseUrl, slugToDeliveryItemPath } from './config.js';
import { checkOk } from './checks.js';

export function cmsHealth() {
  const res = http.get(`${cmsBaseUrl()}/health`, {
    tags: { endpoint: 'health', service: 'umbraco-cms' },
  });
  checkOk(res, 'cms health');
  return res;
}

export function cmsDeliveryList(skip = 0, take = 10) {
  const url = `${cmsBaseUrl()}/umbraco/delivery/api/v2/content?filter=contentType:article&skip=${skip}&take=${take}&sort=createDate:desc`;
  const res = http.get(url, {
    tags: { endpoint: 'delivery_list', service: 'umbraco-cms' },
  });
  checkOk(res, 'delivery list');
  return res;
}

export function cmsDeliveryItem(slug) {
  const path = slugToDeliveryItemPath(slug);
  const url = `${cmsBaseUrl()}/umbraco/delivery/api/v2/content/item/${encodeURIComponent(path)}`;
  const res = http.get(url, {
    tags: { endpoint: 'delivery_item', service: 'umbraco-cms' },
  });
  checkOk(res, 'delivery item');
  return res;
}

export function cmsSearch(term, page = 1, pageSize = 10) {
  const url = `${cmsBaseUrl()}/umbraco/api/search?q=${encodeURIComponent(term)}&page=${page}&pageSize=${pageSize}`;
  const res = http.get(url, {
    tags: { endpoint: 'search', service: 'umbraco-cms' },
  });
  checkOk(res, 'search');
  return res;
}

/**
 * Weighted CMS traffic mix — public APIs only (Delivery, search, health).
 * Demo / antipattern endpoints are not part of the suite; toggle them on the CMS and probe separately.
 */
export function cmsMixedIteration(slugs, searchTerms) {
  const r = Math.random();

  if (r < 0.4) {
    const skip = Math.floor(Math.random() * 90);
    cmsDeliveryList(skip, 10);
  } else if (r < 0.7) {
    const slug = slugs[Math.floor(Math.random() * slugs.length)];
    cmsDeliveryItem(slug);
  } else if (r < 0.95) {
    const term = searchTerms[Math.floor(Math.random() * searchTerms.length)];
    cmsSearch(term);
  } else {
    cmsHealth();
  }
}
