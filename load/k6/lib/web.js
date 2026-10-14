import http from 'k6/http';
import { webBaseUrl } from './config.js';
import { checkOk } from './checks.js';

export function webHealth() {
  const res = http.get(`${webBaseUrl()}/api/health`, {
    tags: { endpoint: 'web_health', service: 'umbraco-web' },
  });
  checkOk(res, 'web health');
  return res;
}

export function webHome() {
  const res = http.get(`${webBaseUrl()}/`, {
    tags: { endpoint: 'home', page: 'home', service: 'umbraco-web' },
  });
  checkOk(res, 'home');
  return res;
}

export function webArticles(page = 1) {
  const res = http.get(`${webBaseUrl()}/articles?page=${page}`, {
    tags: { endpoint: 'articles', page: 'articles', service: 'umbraco-web' },
  });
  checkOk(res, 'articles');
  return res;
}

export function webArticleDetail(slug) {
  const res = http.get(`${webBaseUrl()}/articles/${encodeURIComponent(slug)}`, {
    tags: { endpoint: 'article_detail', page: 'detail', service: 'umbraco-web' },
  });
  checkOk(res, 'article detail');
  return res;
}

export function webSearch(term) {
  const res = http.get(`${webBaseUrl()}/search?q=${encodeURIComponent(term)}`, {
    tags: { endpoint: 'search', page: 'search', service: 'umbraco-web' },
  });
  checkOk(res, 'web search');
  return res;
}

export function webAbout() {
  const res = http.get(`${webBaseUrl()}/about`, {
    tags: { endpoint: 'about', page: 'about', service: 'umbraco-web' },
  });
  checkOk(res, 'about');
  return res;
}

/** Light mixed web traffic (secondary to CMS suites). */
export function webMixedIteration(slugs, searchTerms) {
  const r = Math.random();
  if (r < 0.25) {
    webHome();
  } else if (r < 0.5) {
    webArticles(1 + Math.floor(Math.random() * 5));
  } else if (r < 0.8) {
    const slug = slugs[Math.floor(Math.random() * slugs.length)];
    webArticleDetail(slug);
  } else if (r < 0.95) {
    const term = searchTerms[Math.floor(Math.random() * searchTerms.length)];
    webSearch(term);
  } else {
    webAbout();
  }
}
