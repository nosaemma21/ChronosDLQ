import { type DeadLetterMessage, type RabbitMqQueueInfo } from "../types";
import { MessageCard } from "./MessageCard";

interface MessageStreamProps {
  messages: DeadLetterMessage[];
  selectedMessageId?: string;
  isLoading: boolean;
  error: string | null;
  availableQueues: RabbitMqQueueInfo[];
  watchedQueues: string[];
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
  queueDraft,
  isQueueActionPending,
  onQueueDraftChange,
  onWatchQueue,
  onUnwatchQueue,
  onSelectMessage,
}: MessageStreamProps) {
  const dlqQueues = availableQueues.filter((queue) => queue.name.endsWith(".dlq"));

  return (
    <section className="col-span-4 flex min-h-0 flex-col gap-4 overflow-hidden border-r border-slate-900 bg-slate-950 p-4">
      <div className="shrink-0 space-y-3 rounded-lg border border-slate-900 bg-slate-900/30 p-3">
        <div className="font-mono text-xs tracking-wider text-slate-500 uppercase">
          Queue Watchlist
        </div>

        <div className="flex gap-2">
          <input
            list="available-queues"
            value={queueDraft}
            onChange={(event) => onQueueDraftChange(event.target.value)}
            placeholder="orders.dlq"
            className="min-w-0 flex-1 rounded-md border border-slate-800 bg-slate-950 px-3 py-2 font-mono text-xs text-slate-200 outline-none focus:border-emerald-500/50"
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
            className="flex min-w-20 items-center justify-center gap-2 rounded-md bg-emerald-600 px-3 py-2 text-xs font-semibold text-emerald-950 transition hover:bg-emerald-400 disabled:bg-slate-800 disabled:text-slate-500"
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
                className="rounded border border-slate-800 bg-slate-950 px-2 py-1 font-mono text-[11px] text-slate-400 transition hover:border-slate-700 hover:text-slate-200"
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
                className="rounded border border-rose-500/20 bg-rose-500/10 px-2 py-1 font-mono text-[11px] text-rose-300 transition hover:border-rose-500/40 disabled:opacity-60"
              >
                {queueName} x
              </button>
            ))
          )}
        </div>
      </div>

      <div className="shrink-0 px-1 font-mono text-xs tracking-wider text-slate-500 uppercase">
        Active Poison Stream ({messages.length})
      </div>

      <div className="min-h-0 flex-1 space-y-3 overflow-y-auto pr-1">
        {isLoading && messages.length === 0 ? (
          <div className="p-4 font-mono text-xs text-slate-600">
            Streaming index layers...
          </div>
        ) : error ? (
          <div className="rounded-lg border border-amber-900/30 bg-amber-950/20 p-4 font-mono text-xs text-amber-400">
            {error}
          </div>
        ) : messages.length === 0 ? (
          <div className="rounded-xl border border-dashed border-slate-900 p-8 text-center font-mono text-xs text-slate-600">
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
