import { context, propagation } from "@opentelemetry/api";

import { getServerEnv } from "@/lib/env";
import { withCmsFetchSpan } from "@/lib/observability/tracer";
import {
  mapArticleDetail,
  mapArticleSummary,
  mapCategory,
  mapSiteSettings,
} from "@/lib/cms/mappers";
import { slugToDeliveryItemPath } from "@/lib/cms/slug";
import type {
  ArticleDetail,
  ArticleSummary,
  Category,
  CmsSearchResponse,
  DeliveryArticleItem,
  DeliveryCategoryItem,
  DeliveryContentResponse,
  DeliverySiteSettingsItem,
  SiteSettings,
} from "@/lib/cms/types";

export const REVALIDATE_LIST = 60;
export const REVALIDATE_DETAIL = 300;
export const REVALIDATE_ABOUT = 3600;
export const ARTICLES_PAGE_SIZE = 12;
export const HOME_LATEST_COUNT = 6;
const MAX_ARTICLES_FOR_CATEGORY_FILTER = 100;

type FetchOptions = {
  revalidate?: number | false;
  tags?: string[];
};

async function cmsFetch<T>(path: string, options: FetchOptions = {}): Promise<T> {
  return withCmsFetchSpan("fetch", { "http.route": path }, async () => {
    const base = getServerEnv().CMS_BASE_URL.replace(/\/$/, "");
    const url = `${base}${path.startsWith("/") ? path : `/${path}`}`;

    const headers = new Headers({ Accept: "application/json" });
    // Continue the active SSR span into Umbraco (W3C traceparent → ASP.NET → SQL).
    propagation.inject(context.active(), headers);

    const init: RequestInit & { next?: { revalidate?: number | false; tags?: string[] } } = {
      headers,
    };

    if (options.revalidate === false) {
      init.cache = "no-store";
    } else {
      init.next = {
        revalidate: options.revalidate ?? REVALIDATE_LIST,
        tags: options.tags,
      };
    }

    const response = await fetch(url, init);

    if (!response.ok) {
      throw new Error(`CMS ${response.status} ${response.statusText} for ${path}`);
    }

    return response.json() as Promise<T>;
  });
}

export async function getSiteSettings(): Promise<SiteSettings> {
  const data = await cmsFetch<DeliveryContentResponse<DeliverySiteSettingsItem>>(
    "/umbraco/delivery/api/v2/content?filter=contentType:siteSettings&take=1",
    { revalidate: REVALIDATE_ABOUT, tags: ["site-settings"] },
  );

  const item = data.items[0];
  if (!item) {
    throw new Error("Site settings not found in CMS");
  }

  return mapSiteSettings(item);
}

export async function getCategories(): Promise<Category[]> {
  const data = await cmsFetch<DeliveryContentResponse<DeliveryCategoryItem>>(
    "/umbraco/delivery/api/v2/content?filter=contentType:category&take=10&sort=name:asc",
    { revalidate: REVALIDATE_LIST, tags: ["categories"] },
  );

  return data.items.map(mapCategory);
}

async function fetchArticleItems(skip: number, take: number): Promise<DeliveryContentResponse<DeliveryArticleItem>> {
  return cmsFetch<DeliveryContentResponse<DeliveryArticleItem>>(
    `/umbraco/delivery/api/v2/content?filter=contentType:article&skip=${skip}&take=${take}&sort=createDate:desc`,
    { revalidate: REVALIDATE_LIST, tags: ["articles"] },
  );
}

export async function getLatestArticles(count = HOME_LATEST_COUNT): Promise<ArticleSummary[]> {
  const data = await fetchArticleItems(0, count);
  return data.items.map(mapArticleSummary);
}

export async function getArticlesPage(options: {
  page: number;
  pageSize?: number;
  categorySlug?: string | null;
}): Promise<{ articles: ArticleSummary[]; total: number; page: number; pageSize: number }> {
  const pageSize = options.pageSize ?? ARTICLES_PAGE_SIZE;
  const page = Math.max(1, options.page);

  if (options.categorySlug) {
    const data = await fetchArticleItems(0, MAX_ARTICLES_FOR_CATEGORY_FILTER);
    const filtered = data.items
      .map(mapArticleSummary)
      .filter((a) => a.categorySlug === options.categorySlug);

    const total = filtered.length;
    const start = (page - 1) * pageSize;

    return {
      articles: filtered.slice(start, start + pageSize),
      total,
      page,
      pageSize,
    };
  }

  const skip = (page - 1) * pageSize;
  const data = await fetchArticleItems(skip, pageSize);

  return {
    articles: data.items.map(mapArticleSummary),
    total: data.total,
    page,
    pageSize,
  };
}

export async function getArticleBySlug(slug: string): Promise<ArticleDetail | null> {
  const paths = [slug, slugToDeliveryItemPath(slug)];
  const uniquePaths = [...new Set(paths)];

  for (const path of uniquePaths) {
    try {
      const item = await cmsFetch<DeliveryArticleItem>(
        `/umbraco/delivery/api/v2/content/item/${encodeURIComponent(path)}`,
        { revalidate: REVALIDATE_DETAIL, tags: [`article-${slug}`] },
      );

      if (item.contentType === "article") {
        return mapArticleDetail(item);
      }
    } catch {
      // try next path variant
    }
  }

  const list = await fetchArticleItems(0, MAX_ARTICLES_FOR_CATEGORY_FILTER);
  const found = list.items.find((i) => i.properties.slug === slug);
  return found ? mapArticleDetail(found) : null;
}

export async function searchArticles(
  query: string,
  page = 1,
  pageSize = 10,
): Promise<CmsSearchResponse> {
  const params = new URLSearchParams({
    q: query,
    page: String(page),
    pageSize: String(pageSize),
  });

  return cmsFetch<CmsSearchResponse>(`/umbraco/api/search?${params}`, {
    revalidate: false,
  });
}
