import { check } from 'k6';

export function checkOk(res, name) {
  return check(res, {
    [`${name} status 200`]: (r) => r.status === 200,
  });
}
