import { NextRequest } from "next/server";

import { GET as proxyGet, OPTIONS, POST as proxyPost } from "./[[...path]]/route";

/** Same-origin collector root; Alloy upstream path is added in the catch-all handler. */
export async function GET(request: NextRequest) {
  return proxyGet(request, { params: Promise.resolve({ path: undefined }) });
}

export async function POST(request: NextRequest) {
  return proxyPost(request, { params: Promise.resolve({ path: undefined }) });
}

export { OPTIONS };
