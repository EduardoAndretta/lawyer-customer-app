#!/bin/bash

# Configuration
POD_NAME="lawyerapp_pod"
API_IMAGE_NAME="lawyer-api-img"
UI_IMAGE_NAME="lawyer-ui-img"
API_CONTAINER_NAME="lawyer-api-container"
UI_CONTAINER_NAME="lawyer-ui-container"

# Ports exposed on the host
HOST_API_PORT="5001" # API will be accessible at http://localhost:5001
HOST_UI_PORT="4200"  # UI will be accessible at http://localhost:4200

# Internal ports (these are EXPOSEd in Dockerfiles)
API_INTERNAL_PORT="8080"
UI_INTERNAL_PORT="80"

# URLs for build arguments
# Angular app in browser calls API via host-exposed port
EFFECTIVE_API_BASE_URL="http://localhost:${HOST_API_PORT}/api"
# API needs to allow CORS from where the browser accesses the UI
EFFECTIVE_UI_ORIGIN_URL="http://localhost:${HOST_UI_PORT}"

# Database volume name and path inside API container
DB_VOLUME_NAME="lawyer_api_db_data"
# This path is *inside* the Linux-based API container.
# It must match where appsettings.json expects it (Data Source=Database/app.db)
# and where it's copied in Dockerfile.api (WORKDIR /app; COPY --from=build ... ./Database)
DB_PATH_IN_CONTAINER="/app/Database"

# --- Cleanup previous instances (optional, use with care) ---
echo "Attempting to stop and remove existing containers and pod..."
podman container stop ${API_CONTAINER_NAME} ${UI_CONTAINER_NAME} > /dev/null 2>&1
podman container rm ${API_CONTAINER_NAME} ${UI_CONTAINER_NAME} > /dev/null 2>&1
podman pod stop ${POD_NAME} > /dev/null 2>&1
podman pod rm ${POD_NAME} > /dev/null 2>&1
# Optionally remove images if you want a full fresh build every time
# podman image rm ${API_IMAGE_NAME} ${UI_IMAGE_NAME} > /dev/null 2>&1
# Optionally remove volume (CAUTION: DELETES DATA in lawyer_api_db_data)
# podman volume rm ${DB_VOLUME_NAME} > /dev/null 2>&1

# --- Create a Pod ---
echo "Creating pod '${POD_NAME}' with port mappings..."
podman pod create \
  --name "${POD_NAME}" \
  -p "${HOST_API_PORT}:${API_INTERNAL_PORT}" \
  -p "${HOST_UI_PORT}:${UI_INTERNAL_PORT}"

if [ $? -ne 0 ]; then
  echo "Error creating pod. Exiting."
  exit 1
fi

# --- Build API Image ---
echo "Building API image '${API_IMAGE_NAME}'..."
podman build \
  --build-arg "UI_ORIGIN_URL_ARG=${EFFECTIVE_UI_ORIGIN_URL}" \
  -t "${API_IMAGE_NAME}" \
  -f Dockerfile.api . # Context is current directory

if [ $? -ne 0 ]; then
  echo "Error building API image. Exiting."
  podman pod rm -f ${POD_NAME} # Clean up pod
  exit 1
fi

# --- Run API Container in the Pod ---
echo "Running API container '${API_CONTAINER_NAME}' in pod '${POD_NAME}'..."
podman run -d --restart=always \
  --pod "${POD_NAME}" \
  --name "${API_CONTAINER_NAME}" \
  -e "ASPNETCORE_ENVIRONMENT=Production" \
  -e "ASPNETCORE_URLS=http://+:${API_INTERNAL_PORT}" \
  -v "${DB_VOLUME_NAME}:${DB_PATH_IN_CONTAINER}" \
  "${API_IMAGE_NAME}"
  # No :Z or :z for Windows compatibility with Podman Desktop

if [ $? -ne 0 ]; then
  echo "Error running API container. Exiting."
  podman pod rm -f ${POD_NAME} # Clean up pod
  exit 1
fi

# --- Build UI Image ---
echo "Building UI image '${UI_IMAGE_NAME}'..."
podman build \
  --build-arg "API_BASE_URL_ARG=${EFFECTIVE_API_BASE_URL}" \
  -t "${UI_IMAGE_NAME}" \
  -f Dockerfile.ui . # Context is current directory

if [ $? -ne 0 ]; then
  echo "Error building UI image. Exiting."
  podman container stop ${API_CONTAINER_NAME} && podman container rm ${API_CONTAINER_NAME} > /dev/null 2>&1
  podman pod rm -f ${POD_NAME} # Clean up pod
  exit 1
fi

# --- Run UI Container in the Pod ---
echo "Running UI container '${UI_CONTAINER_NAME}' in pod '${POD_NAME}'..."
podman run -d --restart=always \
  --pod "${POD_NAME}" \
  --name "${UI_CONTAINER_NAME}" \
  "${UI_IMAGE_NAME}"

if [ $? -ne 0 ]; then
  echo "Error running UI container. Exiting."
  podman container stop ${API_CONTAINER_NAME} && podman container rm ${API_CONTAINER_NAME} > /dev/null 2>&1
  podman pod rm -f ${POD_NAME} # Clean up pod
  exit 1
fi

echo ""
echo "Pod '${POD_NAME}' should be running."
echo "API accessible at: ${EFFECTIVE_API_BASE_URL}"
echo "UI accessible at:  ${EFFECTIVE_UI_ORIGIN_URL}"
echo "To see logs: podman logs -f ${API_CONTAINER_NAME}  OR  podman logs -f ${UI_CONTAINER_NAME}"
echo "To stop all: podman pod stop ${POD_NAME} && podman pod rm -f ${POD_NAME}"
echo "To list pods: podman pod ps"
echo "To list containers in the pod: podman ps -a --pod"
echo "Database is persisted in Podman volume: ${DB_VOLUME_NAME}"