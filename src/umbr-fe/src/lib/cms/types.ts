export type DeliveryRoute = {
  path: string;
  startItem: { id: string; path: string };
};

export type DeliveryMedia = {
  url: string;
  name?: string;
  width?: number;
  height?: number;
};

export type DeliveryRichText = {
  markup: string;
};

export type DeliveryCategoryRef = {
  contentType: string;
  id: string;
  name: string;
  properties?: {
    name?: string;
    slug?: string;
    description?: string;
  };
  route?: DeliveryRoute;
};

export type DeliveryAuthorRef = {
  contentType: string;
  id: string;
  name: string;
  properties?: {
    name?: string;
    slug?: string;
    bio?: string;
    avatar?: DeliveryMedia[];
  };
  route?: DeliveryRoute;
};

export type DeliveryArticleItem = {
  contentType: string;
  id: string;
  name: string;
  createDate: string;
  updateDate: string;
  route: DeliveryRoute;
  properties: {
    title: string;
    slug: string;
    excerpt?: string;
    body?: DeliveryRichText;
    publishDate?: string;
    featuredImage?: DeliveryMedia[];
    category?: DeliveryCategoryRef;
    author?: DeliveryAuthorRef;
  };
};

export type DeliveryContentResponse<T> = {
  total: number;
  items: T[];
};

export type DeliverySiteSettingsItem = {
  contentType: string;
  id: string;
  properties: {
    siteName: string;
    tagline?: string;
    aboutBody?: DeliveryRichText;
    contactEmail?: string;
  };
};

export type DeliveryCategoryItem = {
  contentType: string;
  id: string;
  properties: {
    name: string;
    slug: string;
    description?: string;
  };
};

export type CmsSearchResponse = {
  query: string;
  page: number;
  pageSize: number;
  total: number;
  items: Array<{
    id: string;
    score: number;
    name: string | null;
    slug: string | null;
  }>;
};

export type ArticleSummary = {
  id: string;
  title: string;
  slug: string;
  excerpt: string;
  publishDate: string | null;
  imageUrl: string | null;
  categoryName: string | null;
  categorySlug: string | null;
  authorName: string | null;
};

export type ArticleDetail = ArticleSummary & {
  bodyHtml: string;
  authorBio: string | null;
  authorAvatarUrl: string | null;
};

export type SiteSettings = {
  siteName: string;
  tagline: string;
  aboutHtml: string;
  contactEmail: string;
};

export type Category = {
  id: string;
  name: string;
  slug: string;
  description: string;
};
