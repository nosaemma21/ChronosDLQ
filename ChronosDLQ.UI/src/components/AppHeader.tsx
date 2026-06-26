interface AppHeaderProps {
  hasError: boolean;
}

export function AppHeader({ hasError }: AppHeaderProps) {
  return (
    <header className="pixel-frame flex shrink-0 items-center justify-between gap-4 bg-[#0b1726] px-3 py-2">
      <div className="flex min-w-0 items-center gap-3">
        <div
          className={`active-glow h-6 w-6 shrink-0 border-2 border-[#13243a] ${
            hasError ? "bg-amber-400" : "animate-pulse bg-[#6af052]"
          }`}
        />
        <h1 className="pixel-title truncate text-3xl font-bold tracking-normal">
          <span className="text-[#6af052]">Chronos DLQ</span>{" "}
          <span className="text-[#ffb454]">// Surgeon Dashboard</span>
        </h1>
      </div>
      <div
        className={`pixel-title shrink-0 border-2 px-3 py-1 text-base font-bold ${
          hasError
            ? "border-[#704f1d] bg-[#2b1f12] text-[#ffcf5c]"
            : "border-[#263e56] bg-[#09121e] text-[#6af052]"
        }`}
      >
        SYSTEM STATUS: {hasError ? "DEGRADED_ERR" : "ONLINE"}
      </div>
    </header>
  );
}
