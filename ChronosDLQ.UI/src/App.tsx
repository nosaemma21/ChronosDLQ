import { useCallback, useEffect, useMemo, useState } from "react";
import { AppHeader } from "./components/AppHeader";
import { MessageStream } from "./components/MessageStream";
import { MessageWorkspace } from "./components/MessageWorkspace";
import { useDeadLetterMessages } from "./hooks/useDeadLetterMessages";
import { api, rabbitMqUrlStore } from "./services/api";
import { type RabbitMqQueueInfo } from "./types";
import { showPixelToast } from "./utils/alerts";

const WATCHED_QUEUE_STORAGE_KEY = "chronosdlq:watchedQueues";

function readStoredWatchedQueues(): string[] {
  try {
    const storedValue = localStorage.getItem(WATCHED_QUEUE_STORAGE_KEY);
    const parsedValue = JSON.parse(storedValue ?? "[]") as unknown;

    return Array.isArray(parsedValue)
      ? parsedValue.filter(
          (queueName): queueName is string => typeof queueName === "string",
        )
      : [];
  } catch {
    return [];
  }
}

function storeWatchedQueues(queueNames: string[]) {
  localStorage.setItem(WATCHED_QUEUE_STORAGE_KEY, JSON.stringify(queueNames));
}

function isRabbitMqUrl(connectionUrl: string) {
  try {
    const parsedUrl = new URL(connectionUrl);
    return parsedUrl.protocol === "amqp:" || parsedUrl.protocol === "amqps:";
  } catch {
    return false;
  }
}

function readStoredRabbitMqUrl() {
  const storedUrl = rabbitMqUrlStore.get();
  if (!storedUrl || isRabbitMqUrl(storedUrl)) return storedUrl;

  rabbitMqUrlStore.clear();
  return "";
}

export default function App() {
  const [availableQueues, setAvailableQueues] = useState<RabbitMqQueueInfo[]>(
    [],
  );
  const [watchedQueues, setWatchedQueues] = useState<string[]>([]);
  const [queueDraft, setQueueDraft] = useState("");
  const [queueError, setQueueError] = useState<string | null>(null);
  const [isQueueActionPending, setIsQueueActionPending] = useState(false);
  const [rabbitMqUrl, setRabbitMqUrl] = useState(readStoredRabbitMqUrl);
  const [connectionUrlDraft, setConnectionUrlDraft] = useState(rabbitMqUrl);
  const [isConnectionPending, setIsConnectionPending] = useState(false);
  const isBrokerConfigured = rabbitMqUrl.trim().length > 0;

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
  const appStatus = error ? "error" : isBrokerConfigured ? "online" : "setup";

  const loadQueues = useCallback(async () => {
    if (!rabbitMqUrlStore.get()) {
      setAvailableQueues([]);
      setWatchedQueues([]);
      setQueueError(null);
      return;
    }

    try {
      const [queues, brokerWatchedQueues] = await Promise.all([
        api.getQueues(),
        api.getWatchedQueues(),
      ]);
      const storedWatchedQueues = readStoredWatchedQueues();
      const queuesToRestore = storedWatchedQueues.filter(
        (queueName) => !brokerWatchedQueues.includes(queueName),
      );

      if (queuesToRestore.length > 0) {
        await Promise.allSettled(
          queuesToRestore.map((queueName) => api.watchQueue(queueName)),
        );
      }

      const watched =
        queuesToRestore.length > 0
          ? await api.getWatchedQueues()
          : brokerWatchedQueues;

      setAvailableQueues(queues);
      setWatchedQueues(watched);
      storeWatchedQueues(watched);
      setQueueError(null);

      setQueueDraft((currentQueueDraft) => {
        if (currentQueueDraft || queues.length === 0) return currentQueueDraft;

        const firstDlq = queues.find((queue) => queue.name.endsWith(".dlq"));
        return firstDlq?.name ?? queues[0].name;
      });
    } catch (err: unknown) {
      setQueueError(
        err instanceof Error ? err.message : "Failed to load RabbitMQ queues.",
      );
    }
  }, []);

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

  const handleSaveConnection = async () => {
    const connectionUrl = connectionUrlDraft.trim();
    if (!connectionUrl) return;

    setIsConnectionPending(true);
    try {
      if (!isRabbitMqUrl(connectionUrl)) {
        throw new Error("Use an AMQP URL, for example amqp://localhost:5672");
      }

      rabbitMqUrlStore.set(connectionUrl);
      const queues = await api.getQueues();
      const brokerWatchedQueues = await api.getWatchedQueues();

      setRabbitMqUrl(connectionUrl);
      setAvailableQueues(queues);
      setWatchedQueues(brokerWatchedQueues);
      storeWatchedQueues(brokerWatchedQueues);
      setQueueError(null);

      setQueueDraft((currentQueueDraft) => {
        if (currentQueueDraft || queues.length === 0) return currentQueueDraft;

        const firstDlq = queues.find((queue) => queue.name.endsWith(".dlq"));
        return firstDlq?.name ?? queues[0].name;
      });

      void showPixelToast({
        icon: "success",
        title: "Broker Linked",
        text: "RabbitMQ connection verified.",
      });
    } catch (err: unknown) {
      rabbitMqUrlStore.clear();
      setRabbitMqUrl("");
      setAvailableQueues([]);
      setWatchedQueues([]);
      setQueueError(
        err instanceof Error ? err.message : "Failed to connect to RabbitMQ.",
      );
    } finally {
      setIsConnectionPending(false);
    }
  };

  const handleUnwatchQueue = async (queueName: string) => {
    setIsQueueActionPending(true);
    try {
      await api.unwatchQueue(queueName);
      const nextWatchedQueues = watchedQueues.filter(
        (watchedQueueName) => watchedQueueName !== queueName,
      );
      setWatchedQueues(nextWatchedQueues);
      storeWatchedQueues(nextWatchedQueues);
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
      <AppHeader status={appStatus} />

      <main className="mt-3 grid min-h-0 flex-1 grid-cols-12 gap-3 overflow-hidden">
        <MessageStream
          messages={visibleMessages}
          selectedMessageId={selectedMessage?.messageId}
          isLoading={isLoading}
          error={error ?? queueError}
          rabbitMqUrl={rabbitMqUrl}
          connectionUrlDraft={connectionUrlDraft}
          isConnectionPending={isConnectionPending}
          availableQueues={availableQueues}
          watchedQueues={watchedQueues}
          hasWatchedQueues={watchedQueues.length > 0}
          queueDraft={queueDraft}
          isQueueActionPending={isQueueActionPending}
          onQueueDraftChange={setQueueDraft}
          onConnectionUrlDraftChange={setConnectionUrlDraft}
          onSaveConnection={handleSaveConnection}
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
