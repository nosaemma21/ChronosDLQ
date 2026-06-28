interface AppHeaderProps {
  status: "online" | "setup" | "error";
}

export function AppHeader({ status }: AppHeaderProps) {
  const statusStyles = {
    online: "border-[#263e56] bg-[#09121e] text-[#6af052]",
    setup: "border-[#704f1d] bg-[#2b1f12] text-[#ffcf5c]",
    error: "border-[#704f1d] bg-[#2b1f12] text-[#ffcf5c]",
  };
  const statusLabels = {
    online: "ONLINE",
    setup: "SETUP_REQ",
    error: "DEGRADED_ERR",
  };

  return (
    <header className="pixel-frame flex shrink-0 items-center justify-between gap-4 bg-[#0b1726] px-3 py-2">
      <div className="flex min-w-0 items-center gap-3">
        <div
          className={`active-glow h-6 w-6 shrink-0 border-2 border-[#13243a] ${
            status === "online" ? "animate-pulse bg-[#6af052]" : "bg-amber-400"
          }`}
        />
        <h1 className="pixel-title truncate text-3xl font-bold tracking-normal">
          <span className="text-[#6af052]">Chronos DLQ</span>{" "}
          <span className="text-[#ffb454]">// Surgeon Dashboard</span>
        </h1>
      </div>
      <div
        className={`pixel-title shrink-0 border-2 px-3 py-1 text-base font-bold ${statusStyles[status]}`}
      >
        SYSTEM STATUS: {statusLabels[status]}
      </div>
    </header>
  );
}
