import {
  type DeadLetterMessage,
  type JsonPatchOperation,
  type RabbitMqQueueInfo,
  type ReplayRequest,
} from "../types";

const RABBITMQ_URL_STORAGE_KEY = "chronosdlq:rabbitmqUrl";
const BASE_URL =
  import.meta.env.VITE_CHRONOS_API_BASE_URL ?? "http://localhost:5103/api";
const API_KEY = import.meta.env.VITE_CHRONOS_API_KEY ?? "some_api_key";
const OPERATOR_KEY =
  import.meta.env.VITE_CHRONOS_OPERATOR_KEY ?? "some_chronos_operator_key";
const ACTOR = import.meta.env.VITE_CHRONOS_ACTOR ?? "local-operator";

export const rabbitMqUrlStore = {
  get(): string {
    return localStorage.getItem(RABBITMQ_URL_STORAGE_KEY) ?? "";
  },

  set(connectionUrl: string) {
    localStorage.setItem(RABBITMQ_URL_STORAGE_KEY, connectionUrl);
  },

  clear() {
    localStorage.removeItem(RABBITMQ_URL_STORAGE_KEY);
  },
};

const chronosHeaders = (extraHeaders?: HeadersInit): HeadersInit => {
  const rabbitMqUrl = rabbitMqUrlStore.get();

  return {
    "X-CHRONOS-API-KEY": API_KEY,
    ...(rabbitMqUrl ? { "X-CHRONOS-RABBITMQ-URL": rabbitMqUrl } : {}),
    ...extraHeaders,
  };
};

const chronosOperatorHeaders = (extraHeaders?: HeadersInit): HeadersInit => {
  return chronosHeaders({
    "X-CHRONOS-OPERATOR-KEY": OPERATOR_KEY,
    "X-CHRONOS-ACTOR": ACTOR,
    ...extraHeaders,
  });
};

export const api = {
  async getMessages(): Promise<DeadLetterMessage[]> {
    const response = await fetch(`${BASE_URL}/messages`, {
      headers: chronosHeaders(),
    });
    if (!response.ok) throw new Error("Failed to stream dead letter logs.");
    return response.json();
  },

  async getQueues(): Promise<RabbitMqQueueInfo[]> {
    const response = await fetch(`${BASE_URL}/queues`, {
      headers: chronosHeaders(),
    });
    if (!response.ok) throw new Error("Failed to discover RabbitMQ queues.");
    return response.json();
  },

  async getWatchedQueues(): Promise<string[]> {
    const response = await fetch(`${BASE_URL}/queues/watched`, {
      headers: chronosHeaders(),
    });
    if (!response.ok) throw new Error("Failed to load watched queues.");
    return response.json();
  },

  async watchQueue(queueName: string): Promise<{ message: string }> {
    const response = await fetch(`${BASE_URL}/queues/watched`, {
      method: "POST",
      headers: chronosHeaders({ "Content-Type": "application/json" }),
      body: JSON.stringify({ queueName }),
    });
    if (!response.ok) throw new Error("Failed to watch queue.");
    return response.json();
  },

  async unwatchQueue(queueName: string): Promise<{ message: string }> {
    const response = await fetch(
      `${BASE_URL}/queues/watched/${encodeURIComponent(queueName)}`,
      { method: "DELETE", headers: chronosOperatorHeaders() },
    );
    if (!response.ok) throw new Error("Failed to stop watching queue.");
    return response.json();
  },

  async patchMessage(
    messageId: string,
    operations: JsonPatchOperation[],
  ): Promise<DeadLetterMessage> {
    const response = await fetch(`${BASE_URL}/messages/${messageId}`, {
      method: "PATCH",
      headers: chronosHeaders({ "Content-Type": "application/json" }),
      body: JSON.stringify(operations),
    });
    if (!response.ok) throw new Error("Failed to modify payload.");
    return response.json();
  },

  async replayMessage(request: ReplayRequest): Promise<{ message: string }> {
    const response = await fetch(`${BASE_URL}/messages/replay`, {
      method: "POST",
      headers: chronosOperatorHeaders({ "Content-Type": "application/json" }),
      body: JSON.stringify(request),
    });
    if (!response.ok) throw new Error("Replay failed.");
    return response.json();
  },

  async discardMessage(messageId: string): Promise<{ message: string }> {
    const response = await fetch(`${BASE_URL}/messages/${messageId}`, {
      method: "DELETE",
      headers: chronosOperatorHeaders(),
    });
    if (!response.ok) throw new Error("Failed to remove message.");
    return response.json();
  },
};
