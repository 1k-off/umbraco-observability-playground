import { sleep } from 'k6';
import { SharedArray } from 'k6/data';
import { cmsBaseUrl } from './lib/config.js';
import { cmsMixedIteration } from './lib/cms.js';

const slugs = new SharedArray('slugs', () => JSON.parse(open('./data/slugs.json')));
const searchTerms = new SharedArray('search-terms', () =>
  JSON.parse(open('./data/search-terms.json')),
);

export const options = {
  stages: [
    { duration: '1m', target: 10 },
    { duration: '2m', target: 30 },
    { duration: '5m', target: 50 },
    { duration: '1m', target: 50 },
    { duration: '1m', target: 0 },
  ],
  thresholds: {
    http_req_failed: ['rate<0.02'],
    http_req_duration: ['p(95)<1500'],
    'http_req_duration{endpoint:delivery_list}': ['p(95)<1200'],
    'http_req_duration{endpoint:search}': ['p(95)<2000'],
  },
};

export function setup() {
  console.log(`CMS load → ${cmsBaseUrl()}`);
}

export default function () {
  cmsMixedIteration(slugs, searchTerms);
  sleep(0.5);
}
