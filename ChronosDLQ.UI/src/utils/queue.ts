export function getReplayTargetQueue(queueName: string): string {
  return queueName.endsWith(".dlq") ? queueName.slice(0, -4) : queueName;
}
