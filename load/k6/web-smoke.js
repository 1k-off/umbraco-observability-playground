import { sleep } from 'k6';
import { SharedArray } from 'k6/data';
import { webBaseUrl } from './lib/config.js';
import {
  webAbout,
  webArticleDetail,
  webArticles,
  webHealth,
  webHome,
  webSearch,
} from './lib/web.js';

const slugs = new SharedArray('slugs', () => JSON.parse(open('./data/slugs.json')));
const searchTerms = new SharedArray('search-terms', () =>
  JSON.parse(open('./data/search-terms.json')),
);

export const options = {
  vus: 2,
  duration: '1m',
  thresholds: {
    http_req_failed: ['rate<0.02'],
    http_req_duration: ['p(95)<3000'],
  },
};

export function setup() {
  console.log(`Web smoke → ${webBaseUrl()}`);
}

export default function () {
  webHealth();
  webHome();
  webArticles(1);
  webArticleDetail(slugs[Math.floor(Math.random() * slugs.length)]);
  webSearch(searchTerms[Math.floor(Math.random() * searchTerms.length)]);
  webAbout();
  sleep(1);
}
