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
    { duration: '30s', target: 5 },
    { duration: '30s', target: 100 },
    { duration: '2m', target: 100 },
    { duration: '30s', target: 5 },
    { duration: '90s', target: 5 },
  ],
  thresholds: {
    http_req_failed: ['rate<0.1'],
  },
};

export function setup() {
  console.log(`CMS spike → ${cmsBaseUrl()}`);
}

export default function () {
  cmsMixedIteration(slugs, searchTerms);
  sleep(0.2);
}
