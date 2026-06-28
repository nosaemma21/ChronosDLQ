export interface DeadLetterMessage {
  messageId: string;
  queueName: string;
  rawPayload: string;
  exceptionMessage?: string;
  timestamp: string;
}

export interface ReplayRequest {
  messageId: string;
  targetQueue: string;
  modifiedPayload: string;
}

export interface RabbitMqQueueInfo {
  name: string;
  ready: number;
  unacked: number;
  total: number;
  state: string;
}

export interface RabbitMqConfiguration {
  isConfigured: boolean;
  hostName?: string;
  virtualHost?: string;
  managementBaseUrl?: string;
  updatedAtUtc?: string;
}

export interface RabbitMqConfigurationRequest {
  connectionUrl: string;
  managementBaseUrl?: string;
}

export type JsonValue =
  | string
  | number
  | boolean
  | null
  | { [key: string]: JsonValue }
  | JsonValue[];

export interface JsonPatchOperation {
  op: "add" | "remove" | "replace" | "move" | "copy" | "test";
  path: string;
  value?: JsonValue;
}
