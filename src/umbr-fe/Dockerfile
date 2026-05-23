FROM node:24-alpine AS deps
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci

FROM node:24-alpine AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
ARG CMS_BASE_URL=http://localhost:8080
ENV CMS_BASE_URL=$CMS_BASE_URL
RUN npm run build
# Next must emit the server instrumentation hook (src/instrumentation.ts).
RUN test -f .next/server/instrumentation.js || (echo "ERROR: .next/server/instrumentation.js missing — check src/instrumentation.ts" && exit 1)

FROM node:24-alpine AS runner
WORKDIR /app
ENV NODE_ENV=production
ENV PORT=3000
RUN addgroup -S nodejs && adduser -S nextjs -G nodejs
COPY --from=builder /app/public ./public
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static
COPY --chown=nextjs:nodejs docker-entrypoint.sh ./docker-entrypoint.sh
RUN chmod +x docker-entrypoint.sh
USER nextjs
EXPOSE 3000
CMD ["./docker-entrypoint.sh"]
