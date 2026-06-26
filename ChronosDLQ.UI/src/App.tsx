import { useCallback, useEffect, useMemo, useState } from "react";
import { AppHeader } from "./components/AppHeader";
import { MessageStream } from "./components/MessageStream";
import { MessageWorkspace } from "./components/MessageWorkspace";
import { useDeadLetterMessages } from "./hooks/useDeadLetterMessages";
import { api } from "./services/api";
import { type RabbitMqQueueInfo } from "./types";
import { showPixelToast } from "./utils/alerts";

export default function App() {
  const [availableQueues, setAvailableQueues] = useState<RabbitMqQueueInfo[]>(
    [],
  );
  const [watchedQueues, setWatchedQueues] = useState<string[]>([]);
  const [queueDraft, setQueueDraft] = useState("");
  const [queueError, setQueueError] = useState<string | null>(null);
  const [isQueueActionPending, setIsQueueActionPending] = useState(false);

  const {
    messages,
    selectedMessage,
    editedPayload,
    isLoading,
    isSubmitting,
    error,
    setEditedPayload,
    selectMessage,
    replaySelectedMessage,
  } = useDeadLetterMessages();

  const loadQueues = useCallback(async () => {
    try {
      const [queues, watched] = await Promise.all([
        api.getQueues(),
        api.getWatchedQueues(),
      ]);

      setAvailableQueues(queues);
      setWatchedQueues(watched);
      setQueueError(null);

      if (!queueDraft && queues.length > 0) {
        const firstDlq = queues.find((queue) => queue.name.endsWith(".dlq"));
        setQueueDraft(firstDlq?.name ?? queues[0].name);
      }
    } catch (err: unknown) {
      setQueueError(
        err instanceof Error ? err.message : "Failed to load RabbitMQ queues.",
      );
    }
  }, [queueDraft]);

  useEffect(() => {
    void loadQueues();
  }, [loadQueues]);

  const visibleMessages = useMemo(() => {
    const watchedQueueSet = new Set(watchedQueues);
    return messages.filter((message) => watchedQueueSet.has(message.queueName));
  }, [messages, watchedQueues]);

  const handleWatchQueue = async () => {
    const queueName = queueDraft.trim();
    if (!queueName) return;

    setIsQueueActionPending(true);
    try {
      await api.watchQueue(queueName);
      await loadQueues();
    } catch (err: unknown) {
      setQueueError(
        err instanceof Error ? err.message : "Failed to watch queue.",
      );
    } finally {
      setIsQueueActionPending(false);
    }
  };

  const handleUnwatchQueue = async (queueName: string) => {
    setIsQueueActionPending(true);
    try {
      await api.unwatchQueue(queueName);
      await loadQueues();
    } catch (err: unknown) {
      setQueueError(
        err instanceof Error ? err.message : "Failed to stop watching queue.",
      );
    } finally {
      setIsQueueActionPending(false);
    }
  };

  const handleExecuteReplay = () => {
    void replaySelectedMessage()
      .then(() => {
        void showPixelToast({
          icon: "success",
          title: "Telemetry Re-queued",
          text: "Payload successfully processed and re-queued!",
        });
      })
      .catch((err: unknown) => {
        void showPixelToast({
          icon: "error",
          title: "Engine Failure",
          text: err instanceof Error ? err.message : "Replay failed",
        });
      });
  };

  return (
    <div className="font-display flex h-screen min-h-0 flex-col overflow-hidden p-3 text-[#f6f1dc]">
      <AppHeader hasError={Boolean(error ?? queueError)} />

      <main className="mt-3 grid min-h-0 flex-1 grid-cols-12 gap-3 overflow-hidden">
        <MessageStream
          messages={visibleMessages}
          selectedMessageId={selectedMessage?.messageId}
          isLoading={isLoading}
          error={error ?? queueError}
          availableQueues={availableQueues}
          watchedQueues={watchedQueues}
          queueDraft={queueDraft}
          isQueueActionPending={isQueueActionPending}
          onQueueDraftChange={setQueueDraft}
          onWatchQueue={handleWatchQueue}
          onUnwatchQueue={handleUnwatchQueue}
          onSelectMessage={selectMessage}
        />
        <MessageWorkspace
          selectedMessage={selectedMessage}
          editedPayload={editedPayload}
          isSubmitting={isSubmitting}
          onPayloadChange={setEditedPayload}
          onReplay={handleExecuteReplay}
        />
      </main>
    </div>
  );
}
