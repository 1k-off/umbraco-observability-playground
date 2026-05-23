import { getServerEnv } from "@/lib/env";
import type {
  ArticleDetail,
  ArticleSummary,
  Category,
  DeliveryArticleItem,
  DeliveryAuthorRef,
  DeliveryCategoryItem,
  DeliveryMedia,
  DeliverySiteSettingsItem,
  SiteSettings,
} from "@/lib/cms/types";

export function cmsMediaUrl(path: string | undefined | null): string | null {
  if (!path) {
    return null;
  }

  const base = getServerEnv().CMS_BASE_URL.replace(/\/$/, "");
  return path.startsWith("http") ? path : `${base}${path.startsWith("/") ? "" : "/"}${path}`;
}

function firstMedia(media: DeliveryMedia[] | undefined): DeliveryMedia | undefined {
  return media?.[0];
}

function categoryFromArticle(item: DeliveryArticleItem): {
  name: string | null;
  slug: string | null;
} {
  const cat = item.properties.category;
  if (!cat) {
    return { name: null, slug: null };
  }

  return {
    name: cat.properties?.name ?? cat.name ?? null,
    slug: cat.properties?.slug ?? cat.route?.startItem.path ?? null,
  };
}

export function mapArticleSummary(item: DeliveryArticleItem): ArticleSummary {
  const image = firstMedia(item.properties.featuredImage);
  const { name: categoryName, slug: categorySlug } = categoryFromArticle(item);

  return {
    id: item.id,
    title: item.properties.title ?? item.name,
    slug: item.properties.slug,
    excerpt: item.properties.excerpt ?? "",
    publishDate: item.properties.publishDate ?? null,
    imageUrl: cmsMediaUrl(image?.url),
    categoryName,
    categorySlug,
    authorName: item.properties.author?.properties?.name ?? item.properties.author?.name ?? null,
  };
}

export function mapArticleDetail(item: DeliveryArticleItem): ArticleDetail {
  const summary = mapArticleSummary(item);
  const author = item.properties.author;

  return {
    ...summary,
    bodyHtml: item.properties.body?.markup ?? "",
    authorBio: author?.properties?.bio ?? null,
    authorAvatarUrl: cmsMediaUrl(firstMedia(author?.properties?.avatar)?.url),
  };
}

export function mapCategory(item: DeliveryCategoryItem): Category {
  return {
    id: item.id,
    name: item.properties.name,
    slug: item.properties.slug,
    description: item.properties.description ?? "",
  };
}

export function mapSiteSettings(item: DeliverySiteSettingsItem): SiteSettings {
  return {
    siteName: item.properties.siteName,
    tagline: item.properties.tagline ?? "",
    aboutHtml: item.properties.aboutBody?.markup ?? "",
    contactEmail: item.properties.contactEmail ?? "",
  };
}

export function authorDisplayName(author: DeliveryAuthorRef | undefined): string | null {
  if (!author) {
    return null;
  }

  return author.properties?.name ?? author.name ?? null;
}
