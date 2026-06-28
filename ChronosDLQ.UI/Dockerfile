FROM node:24-alpine AS build
WORKDIR /app

COPY ChronosDLQ.UI/packages.json ./
RUN npm ci

COPY ChronosDLQ.UI/ ./
RUN npm run build

FROM nginx:1.27-alpine AS runtime

COPY ChronosDLQ.UI/nginx.conf /etc/nging/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80