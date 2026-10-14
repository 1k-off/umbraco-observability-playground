import { sleep } from 'k6';
import { SharedArray } from 'k6/data';
import { cmsBaseUrl } from './lib/config.js';
import { cmsMixedIteration } from './lib/cms.js';

const slugs = new SharedArray('slugs', () => JSON.parse(open('./data/slugs.json')));
const searchTerms = new SharedArray('search-terms', () =>
  JSON.parse(open('./data/search-terms.json')),
);

/**
 * Find degradation / knee point. No strict p95 — document results in BASELINE.md.
 */
export const options = {
  stages: [
    { duration: '2m', target: 20 },
    { duration: '3m', target: 50 },
    { duration: '3m', target: 80 },
    { duration: '3m', target: 120 },
    { duration: '2m', target: 150 },
    { duration: '2m', target: 0 },
  ],
  thresholds: {
    // Soft gate until knee is known; failures still surface in summary.
    http_req_failed: ['rate<0.15'],
  },
};

export function setup() {
  console.log(`CMS stress → ${cmsBaseUrl()}`);
}

export default function () {
  cmsMixedIteration(slugs, searchTerms);
  sleep(0.3);
}
