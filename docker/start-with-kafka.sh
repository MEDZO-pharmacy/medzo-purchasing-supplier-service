#!/usr/bin/env bash
set -euo pipefail

export KAFKA_NODE_ID="${KAFKA_NODE_ID:-1}"
export KAFKA_PROCESS_ROLES="${KAFKA_PROCESS_ROLES:-broker,controller}"
export KAFKA_LISTENERS="${KAFKA_LISTENERS:-PLAINTEXT://:9092,CONTROLLER://:9093}"
export KAFKA_ADVERTISED_LISTENERS="${KAFKA_ADVERTISED_LISTENERS:-PLAINTEXT://localhost:9092}"
export KAFKA_CONTROLLER_LISTENER_NAMES="${KAFKA_CONTROLLER_LISTENER_NAMES:-CONTROLLER}"
export KAFKA_LISTENER_SECURITY_PROTOCOL_MAP="${KAFKA_LISTENER_SECURITY_PROTOCOL_MAP:-CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT}"
export KAFKA_CONTROLLER_QUORUM_VOTERS="${KAFKA_CONTROLLER_QUORUM_VOTERS:-1@localhost:9093}"
export KAFKA_INTER_BROKER_LISTENER_NAME="${KAFKA_INTER_BROKER_LISTENER_NAME:-PLAINTEXT}"
export KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR="${KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR:-1}"
export KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR="${KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR:-1}"
export KAFKA_TRANSACTION_STATE_LOG_MIN_ISR="${KAFKA_TRANSACTION_STATE_LOG_MIN_ISR:-1}"
export KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS="${KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS:-0}"
export KAFKA_AUTO_CREATE_TOPICS_ENABLE="${KAFKA_AUTO_CREATE_TOPICS_ENABLE:-true}"
export KAFKA_HEAP_OPTS="${KAFKA_HEAP_OPTS:--Xms128m -Xmx256m}"
export KAFKA_LOG_DIRS="${KAFKA_LOG_DIRS:-/tmp/kraft-combined-logs}"
export CLUSTER_ID="${CLUSTER_ID:-MkU3OEVBNTcwNTJENDM2Qk}"

kafka_pid=""
api_pid=""

shutdown() {
  if [[ -n "$api_pid" ]]; then kill -TERM "$api_pid" 2>/dev/null || true; fi
  if [[ -n "$kafka_pid" ]]; then kill -TERM "$kafka_pid" 2>/dev/null || true; fi
}

trap shutdown TERM INT EXIT

/etc/kafka/docker/run &
kafka_pid=$!

kafka_ready=false
for _ in $(seq 1 60); do
  if ! kill -0 "$kafka_pid" 2>/dev/null; then
    wait "$kafka_pid"
    exit $?
  fi

  if (echo > /dev/tcp/127.0.0.1/9092) >/dev/null 2>&1; then
    kafka_ready=true
    break
  fi

  sleep 1
done

if [[ "$kafka_ready" != "true" ]]; then
  echo "Kafka did not become ready on localhost:9092 within 60 seconds." >&2
  exit 1
fi

dotnet /app/Medzo.PurchasingSupplier.Api.dll &
api_pid=$!

set +e
wait -n "$kafka_pid" "$api_pid"
exit_code=$?
set -e

shutdown
wait "$api_pid" 2>/dev/null || true
wait "$kafka_pid" 2>/dev/null || true
exit "$exit_code"
