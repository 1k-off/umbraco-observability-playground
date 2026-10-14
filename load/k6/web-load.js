import { sleep } from 'k6';
import { SharedArray } from 'k6/data';
import { webBaseUrl } from './lib/config.js';
import { webMixedIteration } from './lib/web.js';

const slugs = new SharedArray('slugs', () => JSON.parse(open('./data/slugs.json')));
const searchTerms = new SharedArray('search-terms', () =>
  JSON.parse(open('./data/search-terms.json')),
);

/** Secondary web load — shorter than CMS load; not the article focus. */
export const options = {
  stages: [
    { duration: '30s', target: 5 },
    { duration: '2m', target: 15 },
    { duration: '1m', target: 15 },
    { duration: '30s', target: 0 },
  ],
  thresholds: {
    http_req_failed: ['rate<0.03'],
    http_req_duration: ['p(95)<2500'],
    'http_req_duration{page:home}': ['p(95)<2000'],
    'http_req_duration{page:search}': ['p(95)<3000'],
  },
};

export function setup() {
  console.log(`Web load → ${webBaseUrl()}`);
}

export default function () {
  webMixedIteration(slugs, searchTerms);
  sleep(0.8);
}
