/**
 * CMS-only isolation suite (plan DoD name). Same mix as cms-load with fixed VUs.
 */
import { sleep } from 'k6';
import { SharedArray } from 'k6/data';
import { cmsBaseUrl } from './lib/config.js';
import { cmsMixedIteration } from './lib/cms.js';

const slugs = new SharedArray('slugs', () => JSON.parse(open('./data/slugs.json')));
const searchTerms = new SharedArray('search-terms', () =>
  JSON.parse(open('./data/search-terms.json')),
);

export const options = {
  vus: 20,
  duration: '10m',
  thresholds: {
    http_req_failed: ['rate<0.02'],
    http_req_duration: ['p(95)<1500'],
    'http_req_duration{endpoint:search}': ['p(95)<2000'],
    'http_req_duration{endpoint:delivery_list}': ['p(95)<1200'],
    'http_req_duration{endpoint:articles_naive}': ['p(95)<1200'],
    'http_req_duration{endpoint:demo_slow}': ['p(95)<200'],
  },
};

export function setup() {
  console.log(`CMS direct → ${cmsBaseUrl()}`);
}

export default function () {
  cmsMixedIteration(slugs, searchTerms);
  sleep(0.5);
}
