# Stage 1: install dependencies
FROM python:3.11-slim AS builder

WORKDIR /app

# Install build-time dependencies (if you end up adding any C‑extensions)
RUN apt-get update \
    && apt-get install -y --no-install-recommends gcc libffi-dev \
    && rm -rf /var/lib/apt/lists/*

# Copy and install Python dependencies
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Stage 2: build final image
FROM python:3.11-slim

WORKDIR /app

# Copy installed packages from builder
COPY --from=builder /usr/local/lib/python3.11/site-packages /usr/local/lib/python3.11/site-packages
COPY --from=builder /usr/local/bin /usr/local/bin

# Copy your application code
COPY . .

# Expose the port FastAPI will listen on
EXPOSE 80

# Use environment variables for configuration
# e.g. RPC_URL, CHUNK_SIZE, MAX_TRANSACTIONS
ENV RPC_URL="https://monad-testnet.hypersync.xyz"
ENV CHUNK_SIZE=5000
ENV MAX_TRANSACTIONS=1000000

# Use Uvicorn with multiple workers for concurrency
CMD ["uvicorn", "main:app", \
    "--host", "0.0.0.0", \
    "--port", "80", \
    "--workers", "4", \
    "--limit-concurrency", "100", \
    "--timeout-keep-alive", "5"]
