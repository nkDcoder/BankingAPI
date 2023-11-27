# Create a Docker network
docker network create banknetwork

# Run the Docker container with port mapping
docker run --network banknetwork -p 5000:80 bankapi
