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
      className={`group cursor-pointer border-2 p-2 transition-all duration-200 ${
        isSelected
          ? "active-glow border-[#d13f4d] bg-[#14233a]"
          : "border-[#263e56] bg-[#09121e] shadow-[3px_3px_0_#020617] hover:border-[#6d8fb0] hover:bg-[#102034]"
      }`}
    >
      <div className="min-w-0">
        <div className="mb-1 flex items-center justify-between gap-2">
          <span className="border-2 border-[#7c1f2a] bg-[#3b1018] px-2 py-0.5 font-mono text-xs font-bold text-[#ff7b86]">
            {message.queueName}
          </span>
          <span className="font-mono text-[10px] text-[#8aa9c5]">
            {new Date(message.timestamp).toLocaleTimeString()}
          </span>
        </div>
        <div className="mb-1 truncate font-mono text-xs text-[#f6f1dc]">
          {message.messageId}
        </div>
        <p className="truncate font-mono text-xs text-[#9fb7cc]">
          {message.exceptionMessage}
        </p>
      </div>
    </div>
  );
}
