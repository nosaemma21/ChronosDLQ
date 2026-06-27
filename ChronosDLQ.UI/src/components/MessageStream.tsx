import { type DeadLetterMessage, type RabbitMqQueueInfo } from "../types";
import { MessageCard } from "./MessageCard";

interface MessageStreamProps {
  messages: DeadLetterMessage[];
  selectedMessageId?: string;
  isLoading: boolean;
  error: string | null;
  availableQueues: RabbitMqQueueInfo[];
  watchedQueues: string[];
  hasWatchedQueues: boolean;
  queueDraft: string;
  isQueueActionPending: boolean;
  onQueueDraftChange: (queueName: string) => void;
  onWatchQueue: () => void;
  onUnwatchQueue: (queueName: string) => void;
  onSelectMessage: (message: DeadLetterMessage) => void;
}

export function MessageStream({
  messages,
  selectedMessageId,
  isLoading,
  error,
  availableQueues,
  watchedQueues,
  hasWatchedQueues,
  queueDraft,
  isQueueActionPending,
  onQueueDraftChange,
  onWatchQueue,
  onUnwatchQueue,
  onSelectMessage,
}: MessageStreamProps) {
  const dlqQueues = availableQueues.filter((queue) =>
    queue.name.endsWith(".dlq"),
  );

  return (
    <section className="col-span-4 flex min-h-0 flex-col gap-3 overflow-hidden">
      <div className="pixel-panel shrink-0 space-y-3 p-3">
        <div className="pixel-title text-xl font-bold text-[#f6f1dc] uppercase">
          Queue Watchlist
        </div>

        <div className="flex gap-2">
          <input
            list="available-queues"
            value={queueDraft}
            onChange={(event) => onQueueDraftChange(event.target.value)}
            placeholder="orders.dlq"
            className="min-w-0 flex-1 border-2 border-[#52718e] bg-[#09121e] px-3 py-2 font-mono text-sm text-[#f6f1dc] shadow-[inset_0_0_0_2px_#020617] outline-none focus:border-[#6af052]"
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
            disabled={isQueueActionPending || !queueDraft.trim()}
            className="pixel-button flex min-w-20 items-center justify-center gap-2 bg-[#79d957] px-2.5 py-1.5 font-pixel text-sm font-medium text-[#10210d] uppercase transition hover:bg-[#9cff78] disabled:bg-[#263849] disabled:text-[#6d8fb0]"
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
          <div className="flex max-h-24 flex-wrap gap-2 overflow-y-auto pr-1">
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

        <div className="flex max-h-20 flex-wrap gap-2 overflow-y-auto pr-1">
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

      <div className="pixel-panel min-h-0 flex-1 space-y-3 overflow-y-auto p-3">
        {!hasWatchedQueues ? (
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
          messages.map((message) => (
            <MessageCard
              key={message.messageId}
              message={message}
              isSelected={selectedMessageId === message.messageId}
              onSelect={onSelectMessage}
            />
          ))
        )}
      </div>
    </section>
  );
}
