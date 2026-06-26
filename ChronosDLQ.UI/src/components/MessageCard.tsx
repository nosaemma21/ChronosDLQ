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
      className={`group grid cursor-pointer grid-cols-[32px_1fr] gap-3 border-2 p-2 transition-all duration-200 ${
        isSelected
          ? "active-glow border-[#d13f4d] bg-[#14233a]"
          : "border-[#263e56] bg-[#09121e] shadow-[3px_3px_0_#020617] hover:border-[#6d8fb0] hover:bg-[#102034]"
      }`}
    >
      <div className="flex h-16 w-8 flex-col items-center justify-between border-2 border-[#52718e] bg-[#122033] p-1 shadow-[2px_2px_0_#020617]">
        <div className="h-8 w-4 border border-[#7c1f2a] bg-[#a51f31]" />
        <div className="flex gap-0.5">
          <span className="h-1 w-1 bg-[#ffcf5c]" />
          <span className="h-1 w-1 bg-[#ffcf5c]" />
          <span className="h-1 w-1 bg-[#ffcf5c]" />
        </div>
      </div>
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
