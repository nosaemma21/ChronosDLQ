interface AppHeaderProps {
  hasError: boolean;
}

export function AppHeader({ hasError }: AppHeaderProps) {
  return (
    <header className="flex items-center justify-between border-b border-slate-900 bg-slate-900/40 px-6 py-4 backdrop-blur-md">
      <div className="flex items-center gap-3">
        <div
          className={`active-glow h-3 w-3 rounded-full ${
            hasError ? "bg-amber-500" : "animate-pulse bg-green-500"
          }`}
        />
        <h1 className="bg-linear-to-r from-rose-400 to-amber-300 bg-clip-text text-xl font-bold tracking-tight text-transparent">
          Chronos DLQ // Surgeon Dashboard
        </h1>
      </div>
      <div
        className={`rounded border px-3 py-1.5 font-mono text-xs ${
          hasError
            ? "border-amber-900/50 bg-amber-950/20 text-amber-400"
            : "border-slate-800 bg-slate-900 text-slate-500"
        }`}
      >
        SYSTEM STATUS: {hasError ? "DEGRADED_ERR" : "ONLINE"}
      </div>
    </header>
  );
}
