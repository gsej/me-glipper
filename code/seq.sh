docker run --rm -e ACCEPT_EULA=Y  \
    --name seq \
    -p 5341:80 \
    -d \
    datalust/seq:latest
