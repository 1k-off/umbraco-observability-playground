import { sleep } from 'k6';
import { SharedArray } from 'k6/data';
import { cmsBaseUrl } from './lib/config.js';
import {
  cmsDeliveryItem,
  cmsDeliveryList,
  cmsHealth,
  cmsSearch,
} from './lib/cms.js';

const slugs = new SharedArray('slugs', () => JSON.parse(open('./data/slugs.json')));
const searchTerms = new SharedArray('search-terms', () =>
  JSON.parse(open('./data/search-terms.json')),
);

export const options = {
  vus: 3,
  duration: '1m',
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<2000'],
  },
};

export function setup() {
  console.log(`CMS smoke → ${cmsBaseUrl()}`);
}

export default function () {
  cmsHealth();
  cmsDeliveryList(0, 10);
  cmsDeliveryItem(slugs[Math.floor(Math.random() * slugs.length)]);
  cmsSearch(searchTerms[Math.floor(Math.random() * searchTerms.length)]);
  sleep(1);
}
