#!/usr/bin/env bash

# Deploys PostgreSQL and all services of the Car Rental System to the current kubectl context.
#   NAMESPACE        - target namespace (default: test)
#   DOCKER_REGISTRY  - registry host (default: docker.io)
#   DOCKER_NAMESPACE - Docker Hub account (default: aivanspooky)
#   IMAGE_TAG        - image tag (default: latest)

set -euo pipefail

namespace=${NAMESPACE:-test}
registry=${DOCKER_REGISTRY:-docker.io}
owner=${DOCKER_NAMESPACE:-aivanspooky}
tag=${IMAGE_TAG:-latest}
timeout=${HELM_TIMEOUT:-5m}

root=$(cd "$(dirname "$0")" && pwd)

printf "=== Deploy postgres to namespace %s ===\n" "$namespace"
helm upgrade --install postgres "$root/postgres-chart" \
  --namespace "$namespace" \
  --create-namespace \
  --wait \
  --timeout "$timeout"

for service in cars rental payment gateway; do
  printf "=== Deploy %s (%s/%s/car-rental-%s:%s) ===\n" "$service" "$registry" "$owner" "$service" "$tag"
  helm upgrade --install "$service" "$root/service-chart" \
    --namespace "$namespace" \
    --values "$root/values/$service.yaml" \
    --set image.repository="$registry/$owner/car-rental-$service" \
    --set-string image.tag="$tag" \
    --wait \
    --timeout "$timeout"
done

kubectl get pods,services,ingress --namespace "$namespace"
