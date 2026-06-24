import { type DeadLetterMessage } from "../types";
import { MessageCard } from "./MessageCard";

interface MessageStreamProps {
  messages: DeadLetterMessage[];
  selectedMessageId?: string;
  isLoading: boolean;
  error: string | null;
  onSelectMessage: (message: DeadLetterMessage) => void;
}

export function MessageStream({
  messages,
  selectedMessageId,
  isLoading,
  error,
  onSelectMessage,
}: MessageStreamProps) {
  return (
    <section className="col-span-4 border-r border-slate-900 bg-slate-950 p-4 overflow-y-auto space-y-3">
      <div className="text-xs font-mono tracking-wider text-slate-500 uppercase px-1 mb-2">
        Active Poison Stream ({messages.length})
      </div>

      {isLoading && messages.length === 0 ? (
        <div className="text-xs font-mono text-slate-600 p-4">
          Streaming index layers...
        </div>
      ) : error ? (
        <div className="p-4 bg-amber-950/20 border border-amber-900/30 rounded-lg text-xs font-mono text-amber-400">
          {error}
        </div>
      ) : messages.length === 0 ? (
        <div className="p-8 border border-dashed border-slate-900 rounded-xl text-center text-xs font-mono text-slate-600">
          Queue clear. Zero dead letters detected.
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
    </section>
  );
}
