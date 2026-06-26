import { useCallback, useEffect, useMemo, useState } from "react";
import Swal from "sweetalert2";
import { AppHeader } from "./components/AppHeader";
import { MessageStream } from "./components/MessageStream";
import { MessageWorkspace } from "./components/MessageWorkspace";
import { useDeadLetterMessages } from "./hooks/useDeadLetterMessages";
import { api } from "./services/api";
import { type RabbitMqQueueInfo } from "./types";

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
        void Swal.fire({
          toast: true,
          position: "bottom-end",
          icon: "success",
          title: "Telemetry Re-queued",
          text: "Payload successfully processed and re-queued!",
          showConfirmButton: false,
          timer: 3000,
          timerProgressBar: true,
          background: "#0f172a",
          color: "#f8fafc",
          customClass: {
            popup: "border border-emerald-500/20 max-w-sm rounded-xl",
          },
        });
      })
      .catch((err: unknown) => {
        void Swal.fire({
          toast: true,
          position: "bottom-end",
          icon: "error",
          title: "Engine Failure",
          text: err instanceof Error ? err.message : "Replay failed",
          showConfirmButton: false,
          timer: 3000,
          timerProgressBar: true,
          background: "#0f172a",
          color: "#f8fafc",
          customClass: {
            popup: "border border-rose-500/20 max-w-sm rounded-xl",
          },
        });
      });
  };

  return (
    <div className="font-display flex min-h-screen flex-col bg-slate-950 text-slate-100">
      <AppHeader hasError={Boolean(error ?? queueError)} />

      <main className="grid h-[calc(100vh-69px)] flex-1 grid-cols-12 overflow-hidden">
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
