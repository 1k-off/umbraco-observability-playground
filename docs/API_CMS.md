# CMS API (Umbraco 17.4.2)

Base URL (local Podman): `http://localhost:8080`

## Health

```http
GET /health
GET /ready
```

## Delivery API (headless)

Public content (after seed and Delivery API exposure):

```http
GET /umbraco/delivery/api/v2/content?skip=0&take=10
GET /umbraco/delivery/api/v2/content/item/{id-or-path}
```

Filter by document type alias via Delivery API query parameters (see [Content Delivery API](https://docs.umbraco.com/umbraco-cms/develop-with-umbraco/headless-and-apis/content-delivery-api)).

Document type aliases: `article`, `category`, `author`, `siteSettings`.

## Custom search (Examine)

```http
GET /umbraco/api/search?q=article&page=1&pageSize=10
```

## Demo endpoints (config `Demo` section)

```http
GET /umbraco/api/demo/slow?delayMs=100
GET /umbraco/api/demo/fault?rate=0.1
```

Enable via environment:

```text
Demo__SlowEndpoint=true
Demo__FaultEndpoint=true
```

## Backoffice

```http
GET /umbraco
```

Default unattended user (override in production): `admin@demo.local` / see `Umbraco__CMS__Unattended__UnattendedUserPassword`.
