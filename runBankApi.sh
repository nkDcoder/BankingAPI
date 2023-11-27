# Build the Docker image
docker build -t bankapi .

# Run the Docker container with port mapping
docker run -p 5000:80 bankapi
