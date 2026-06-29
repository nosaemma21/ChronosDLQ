import {
  type DeadLetterMessage,
  type RabbitMqQueueInfo,
} from "../types";

interface MessageStreamProps {
  messages: DeadLetterMessage[];
  selectedMessageId?: string;
  isLoading: boolean;
  error: string | null;
  rabbitMqUrl: string;
  connectionUrlDraft: string;
  isConnectionPending: boolean;
  availableQueues: RabbitMqQueueInfo[];
  watchedQueues: string[];
  hasWatchedQueues: boolean;
  queueDraft: string;
  isQueueActionPending: boolean;
  onConnectionUrlDraftChange: (connectionUrl: string) => void;
  onSaveConnection: () => void;
  onQueueDraftChange: (queueName: string) => void;
  onWatchQueue: () => void;
  onUnwatchQueue: (queueName: string) => void;
  onSelectMessage: (message: DeadLetterMessage) => void;
}

function formatTraceTime(timestamp: string) {
  return new Date(timestamp).toLocaleTimeString([], {
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    hour12: false,
  });
}

function formatTraceId(messageId: string) {
  return messageId.length > 12 ? `${messageId.slice(0, 12)}...` : messageId;
}

export function MessageStream({
  messages,
  selectedMessageId,
  isLoading,
  error,
  rabbitMqUrl,
  connectionUrlDraft,
  isConnectionPending,
  availableQueues,
  watchedQueues,
  hasWatchedQueues,
  queueDraft,
  isQueueActionPending,
  onConnectionUrlDraftChange,
  onSaveConnection,
  onQueueDraftChange,
  onWatchQueue,
  onUnwatchQueue,
  onSelectMessage,
}: MessageStreamProps) {
  const dlqQueues = availableQueues.filter((queue) =>
    queue.name.endsWith(".dlq"),
  );
  const isBrokerConfigured = rabbitMqUrl.trim().length > 0;

  return (
    <section className="col-span-5 flex min-h-0 flex-col gap-3 overflow-hidden">
      <div className="pixel-panel shrink-0 space-y-2 p-3">
        <div className="flex items-center justify-between gap-3">
          <div className="pixel-title text-xl font-bold text-[#f6f1dc] uppercase">
            Queue Watchlist
          </div>
          <span
            className={`border-2 px-2 py-1 font-mono text-[10px] font-bold uppercase ${
              isBrokerConfigured
                ? "border-[#315f29] bg-[#10210d] text-[#79d957]"
                : "border-[#704f1d] bg-[#2b1f12] text-[#ffcf5c]"
            }`}
          >
            {isBrokerConfigured ? "Connected" : "Unlinked"}
          </span>
        </div>

        <div className="grid grid-cols-[minmax(0,1fr)_auto] gap-2">
          <input
            value={connectionUrlDraft}
            onChange={(event) =>
              onConnectionUrlDraftChange(event.target.value)
            }
            placeholder="amqp://localhost:5672"
            className="min-w-0 border-2 border-[#52718e] bg-[#09121e] px-3 py-2 font-mono text-xs text-[#f6f1dc] shadow-[inset_0_0_0_2px_#020617] outline-none focus:border-[#6af052]"
          />
          <button
            type="button"
            onClick={onSaveConnection}
            disabled={isConnectionPending || !connectionUrlDraft.trim()}
            className="pixel-button flex min-w-18 items-center justify-center gap-2 bg-[#79d957] px-2.5 py-1.5 font-pixel text-xs font-medium text-[#10210d] uppercase transition hover:bg-[#9cff78] disabled:bg-[#263849] disabled:text-[#6d8fb0]"
          >
            {isConnectionPending ? "Linking..." : "Link"}
          </button>
        </div>

        {isBrokerConfigured ? (
          <div className="truncate font-mono text-xs text-[#8fb4dc]">
            {rabbitMqUrl.replace(/^amqps?:\/\//, "")}
          </div>
        ) : null}

        {error ? (
          <div className="border-2 border-[#704f1d] bg-[#2b1f12] px-3 py-2 font-mono text-xs text-[#ffcf5c]">
            {error}
          </div>
        ) : null}

        <div className="grid grid-cols-[minmax(0,1fr)_auto] gap-2 border-t-2 border-[#263e56] pt-2">
          <input
            list="available-queues"
            value={queueDraft}
            onChange={(event) => onQueueDraftChange(event.target.value)}
            placeholder="orders.dlq"
            className="min-w-0 border-2 border-[#52718e] bg-[#09121e] px-3 py-2 font-mono text-sm text-[#f6f1dc] shadow-[inset_0_0_0_2px_#020617] outline-none focus:border-[#6af052]"
          />
          <datalist id="available-queues">
            {availableQueues.map((queue) => (
              <option key={queue.name} value={queue.name}>
                {queue.name}
              </option>
            ))}
          </datalist>
          <button
            type="button"
            onClick={onWatchQueue}
            disabled={
              !isBrokerConfigured || isQueueActionPending || !queueDraft.trim()
            }
            className="pixel-button flex min-w-18 items-center justify-center gap-2 bg-[#79d957] px-2.5 py-1.5 font-pixel text-xs font-medium text-[#10210d] uppercase transition hover:bg-[#9cff78] disabled:bg-[#263849] disabled:text-[#6d8fb0]"
          >
            {isQueueActionPending ? (
              <>
                <span className="h-3 w-3 animate-spin rounded-full border-2 border-slate-500 border-t-transparent" />
                Watching...
              </>
            ) : (
              "Watch"
            )}
          </button>
        </div>

        {dlqQueues.length > 0 ? (
          <div className="flex max-h-16 flex-wrap gap-2 overflow-y-auto pr-1">
            {dlqQueues.map((queue) => (
              <button
                type="button"
                key={queue.name}
                onClick={() => onQueueDraftChange(queue.name)}
                className="border-2 border-[#1f2d3e] bg-[#102034] px-2 py-1 font-mono text-xs font-bold text-[#cfe3f5] shadow-[2px_2px_0_#020617] transition hover:border-[#6d8fb0] hover:text-[#f6f1dc]"
              >
                {queue.name}
              </button>
            ))}
          </div>
        ) : null}

        <div className="flex max-h-14 flex-wrap gap-2 overflow-y-auto pr-1">
          {watchedQueues.length === 0 ? (
            <span className="font-mono text-xs text-slate-600">
              No queues watched yet.
            </span>
          ) : (
            watchedQueues.map((queueName) => (
              <button
                type="button"
                key={queueName}
                onClick={() => onUnwatchQueue(queueName)}
                disabled={isQueueActionPending}
                className="border-2 border-[#7c1f2a] bg-[#3b1018] px-2 py-1 font-mono text-xs font-bold text-[#ff7b86] shadow-[2px_2px_0_#020617] transition hover:border-[#d13f4d] disabled:opacity-60"
              >
                {queueName} x
              </button>
            ))
          )}
        </div>
      </div>

      <div className="pixel-panel pixel-title shrink-0 px-3 py-2 text-xl font-bold text-[#f6f1dc] uppercase">
        Active Poison Stream ({messages.length})
      </div>

      <div className="pixel-panel min-h-0 flex-1 overflow-y-auto p-1.5">
        {!isBrokerConfigured ? (
          <div className="border-2 border-dashed border-[#263e56] p-8 text-center font-mono text-xs text-[#6d8fb0]">
            Link RabbitMQ to start streaming traces.
          </div>
        ) : !hasWatchedQueues ? (
          <div className="border-2 border-dashed border-[#263e56] p-8 text-center font-mono text-xs text-[#6d8fb0]">
            Select a queue and press Watch to start streaming traces.
          </div>
        ) : isLoading && messages.length === 0 ? (
          <div className="p-4 font-mono text-sm text-[#6d8fb0]">
            Streaming index layers...
          </div>
        ) : error ? (
          <div className="border-2 border-[#704f1d] bg-[#2b1f12] p-4 font-mono text-xs text-[#ffcf5c]">
            {error}
          </div>
        ) : messages.length === 0 ? (
          <div className="border-2 border-dashed border-[#263e56] p-8 text-center font-mono text-xs text-[#6d8fb0]">
            Watched queues are clear.
          </div>
        ) : (
          <div className="min-w-0 overflow-hidden border-2 border-[#263e56] bg-[#07111d]">
            <div className="grid grid-cols-[112px_minmax(150px,0.9fr)_minmax(0,1.3fr)_78px] border-b-2 border-[#263e56] bg-[#102034] font-mono text-[10px] font-bold uppercase text-[#8fb4dc]">
              <div className="border-r-2 border-[#263e56] px-2 py-1.5">
                Queue
              </div>
              <div className="border-r-2 border-[#263e56] px-2 py-1.5">
                Trace
              </div>
              <div className="border-r-2 border-[#263e56] px-2 py-1.5">
                Reason
              </div>
              <div className="px-2 py-1.5 text-right">Time</div>
            </div>

            <div className="divide-y-2 divide-[#17283c]">
              {messages.map((message) => {
                const isSelected = selectedMessageId === message.messageId;

                return (
                  <button
                    type="button"
                    key={message.messageId}
                    onClick={() => onSelectMessage(message)}
                    className={`grid h-10 w-full grid-cols-[112px_minmax(150px,0.9fr)_minmax(0,1.3fr)_78px] text-left font-mono text-xs transition ${
                      isSelected
                        ? "bg-[#14233a] text-[#f6f1dc] outline-2 outline-[#d13f4d]"
                        : "bg-[#09121e] text-[#cfe3f5] hover:bg-[#102034]"
                    }`}
                  >
                    <span className="truncate border-r-2 border-[#17283c] px-2 py-2 font-bold text-[#ff7b86]">
                      {message.queueName}
                    </span>
                    <span className="truncate border-r-2 border-[#17283c] px-2 py-2 text-[#f6f1dc]">
                      {formatTraceId(message.messageId)}
                    </span>
                    <span className="truncate border-r-2 border-[#17283c] px-2 py-2 text-[#9fb7cc]">
                      {message.exceptionMessage ?? "Unknown DLQ execution"}
                    </span>
                    <span className="px-2 py-2 text-right text-[10px] whitespace-nowrap text-[#8aa9c5]">
                      {formatTraceTime(message.timestamp)}
                    </span>
                  </button>
                );
              })}
            </div>
          </div>
        )}
      </div>
    </section>
  );
}
