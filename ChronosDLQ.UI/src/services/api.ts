import {
  type DeadLetterMessage,
  type JsonPatchOperation,
  type ReplayRequest,
} from "../types";

const BASE_URL = "https://localhost:5103/api";

export const api = {
  // Fetching all dead letter messages
  async getMessages(): Promise<DeadLetterMessage[]> {
    const response = await fetch(`${BASE_URL}/messages`);
    if (!response.ok) throw new Error("Failed to stream dead letter logs 😔.");
    return response.json();
  },

  // applying jsonpatch modfication to active message payload
  async patchMessage(
    messageId: string,
    operations: JsonPatchOperation[],
  ): Promise<DeadLetterMessage> {
    const response = await fetch(`${BASE_URL}/messages/${messageId}`, {
      method: "PATCH",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(operations),
    });
    if (!response.ok)
      throw new Error("Failed to commit payload patch adjustments 😔");
    return response.json();
  },

  async replayMessage(request: ReplayRequest): Promise<{ message: string }> {
    const response = await fetch(`${BASE_URL}/messages/replay`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request),
    });
    if (!response.ok) throw new Error("Replay engine execution failure 💔");
    return response.json();
  },
};
