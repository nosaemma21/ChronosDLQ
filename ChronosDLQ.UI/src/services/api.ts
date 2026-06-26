import {
  type DeadLetterMessage,
  type JsonPatchOperation,
  type RabbitMqQueueInfo,
  type ReplayRequest,
} from "../types";

const BASE_URL = "http://localhost:5103/api";

export const api = {
  async getMessages(): Promise<DeadLetterMessage[]> {
    const response = await fetch(`${BASE_URL}/messages`);
    if (!response.ok) throw new Error("Failed to stream dead letter logs.");
    return response.json();
  },

  async getQueues(): Promise<RabbitMqQueueInfo[]> {
    const response = await fetch(`${BASE_URL}/queues`);
    if (!response.ok) throw new Error("Failed to discover RabbitMQ queues.");
    return response.json();
  },

  async getWatchedQueues(): Promise<string[]> {
    const response = await fetch(`${BASE_URL}/queues/watched`);
    if (!response.ok) throw new Error("Failed to load watched queues.");
    return response.json();
  },

  async watchQueue(queueName: string): Promise<{ message: string }> {
    const response = await fetch(`${BASE_URL}/queues/watched`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ queueName }),
    });
    if (!response.ok) throw new Error("Failed to watch queue.");
    return response.json();
  },

  async unwatchQueue(queueName: string): Promise<{ message: string }> {
    const response = await fetch(
      `${BASE_URL}/queues/watched/${encodeURIComponent(queueName)}`,
      { method: "DELETE" },
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
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(operations),
    });
    if (!response.ok) throw new Error("Failed to modify payload.");
    return response.json();
  },

  async replayMessage(request: ReplayRequest): Promise<{ message: string }> {
    const response = await fetch(`${BASE_URL}/messages/replay`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request),
    });
    if (!response.ok) throw new Error("Replay failed.");
    return response.json();
  },

  async discardMessage(messageId: string): Promise<{ message: string }> {
    const response = await fetch(`${BASE_URL}/messages/${messageId}`, {
      method: "DELETE",
    });
    if (!response.ok) throw new Error("Failed to remove message.");
    return response.json();
  },
};
