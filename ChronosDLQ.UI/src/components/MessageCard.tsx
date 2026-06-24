import { type DeadLetterMessage } from "../types";

interface MessageCardProps {
  message: DeadLetterMessage;
  isSelected: boolean;
  onSelect: (message: DeadLetterMessage) => void;
}

export function MessageCard({
  message,
  isSelected,
  onSelect,
}: MessageCardProps) {
  return (
    <div
      onClick={() => onSelect(message)}
      className={`group p-4 rounded-lg border transition-all duration-200 cursor-pointer ${
        isSelected
          ? "bg-slate-900/80 border-rose-500/40 active-glow"
          : "bg-slate-900/30 border-slate-900 hover:border-slate-800 hover:bg-slate-900/50"
      }`}
    >
      <div className="flex items-center justify-between gap-2 mb-2">
        <span className="text-xs font-mono font-medium text-rose-400 px-2 py-0.5 rounded bg-rose-500/10 border border-rose-500/20">
          {message.queueName}
        </span>
        <span className="text-[10px] font-mono text-slate-500">
          {new Date(message.timestamp).toLocaleTimeString()}
        </span>
      </div>
      <div className="text-sm font-mono text-slate-300 truncate mb-1">
        ID: {message.messageId}
      </div>
      <p className="text-xs text-slate-400 line-clamp-2 font-mono bg-slate-950/40 p-2 rounded border border-slate-900/80 group-hover:border-slate-800/40">
        {message.exceptionMessage}
      </p>
    </div>
  );
}
