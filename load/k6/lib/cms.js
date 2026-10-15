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
 * Articles enrichment API (Demo__NaiveArticlesEndpoint must be on).
 * Same SLO family as Delivery list — tests do not encode an expected bug.
 */
export function cmsArticlesNaive(take = 10) {
  const url = `${cmsBaseUrl()}/umbraco/api/demo/articles-naive?take=${take}`;
  const res = http.get(url, {
    tags: { endpoint: 'articles_naive', service: 'umbraco-cms', feature: 'articles-enrichment' },
  });
  checkOk(res, 'articles enrichment');
  return res;
}

/**
 * Demo delay endpoint (Demo__SlowEndpoint must be on).
 * Called with a small delayMs; latency SLO should still be tight (catches oversized delay).
 */
export function cmsSlow(delayMs = 50) {
  const url = `${cmsBaseUrl()}/umbraco/api/demo/slow?delayMs=${delayMs}`;
  const res = http.get(url, {
    tags: { endpoint: 'demo_slow', service: 'umbraco-cms', feature: 'demo-slow' },
  });
  checkOk(res, 'demo slow');
  return res;
}

/**
 * CMS mix including post-release enrichment + slow routes.
 * Weights: list 30%, item 25%, search 15%, articles_naive 20%, slow 5%, health 5%.
 */
export function cmsMixedIteration(slugs, searchTerms) {
  const r = Math.random();

  if (r < 0.3) {
    const skip = Math.floor(Math.random() * 90);
    cmsDeliveryList(skip, 10);
  } else if (r < 0.55) {
    const slug = slugs[Math.floor(Math.random() * slugs.length)];
    cmsDeliveryItem(slug);
  } else if (r < 0.7) {
    const term = searchTerms[Math.floor(Math.random() * searchTerms.length)];
    cmsSearch(term);
  } else if (r < 0.9) {
    cmsArticlesNaive(10);
  } else if (r < 0.95) {
    cmsSlow(50);
  } else {
    cmsHealth();
  }
}
