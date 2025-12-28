# Stage 1: Build
FROM node:22-alpine AS build
WORKDIR /app

# Copy package files for better layer caching
COPY package*.json ./

# Install dependencies
RUN npm ci

# Copy source code
COPY . .

# Build production application
RUN npm run build -- --configuration production

# Stage 2: Production with nginx
FROM nginx:alpine AS runtime

# Remove default nginx config
RUN rm -rf /etc/nginx/conf.d/*

# Copy custom nginx configuration as template
COPY nginx.conf /etc/nginx/nginx.conf.template

# Copy built application from build stage
COPY --from=build /app/dist/property-manager/browser /usr/share/nginx/html

# Create non-root user for security
RUN adduser -D -H -u 1000 -s /sbin/nologin nginx-user && \
    chown -R nginx-user:nginx-user /usr/share/nginx/html && \
    chown -R nginx-user:nginx-user /var/cache/nginx && \
    chown -R nginx-user:nginx-user /var/log/nginx && \
    touch /var/run/nginx.pid && \
    chown -R nginx-user:nginx-user /var/run/nginx.pid && \
    chown nginx-user:nginx-user /etc/nginx/nginx.conf.template

# Default API URL for local development
ENV API_URL=http://api:8080

# Expose port
EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:80/ || exit 1

# Use envsubst to substitute API_URL at runtime, then start nginx
CMD envsubst '${API_URL}' < /etc/nginx/nginx.conf.template > /etc/nginx/nginx.conf && nginx -g 'daemon off;'
