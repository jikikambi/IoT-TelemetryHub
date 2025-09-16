#!/usr/bin/env bash
# Simple simulation using grpcurl (requires grpcurl installed)
# Streams a few telemetry messages to the Gateway gRPC endpoint
# Usage: ./simulate-device.sh device-001
DEVICE_ID=${1:-device-001}
echo "Simulating device $DEVICE_ID"

for i in $(seq 1 5); do
  TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
  JSON=$(printf '{"deviceId":"%s","type":"temperature.reading","isoTimestamp":"%s","payloadJson":"{\"celsius\":%s}"}' "$DEVICE_ID" "$TIMESTAMP" "$(awk -v seed=$i 'BEGIN{srand(); print 20 + rand()*5}')")
  printf "%s\n" "$JSON" | grpcurl -plaintext -d @ -import-path ../proto -proto device.proto localhost:7019 device.DeviceGateway/SendTelemetry
done
