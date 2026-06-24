interface AppHeaderProps {
  hasError: boolean;
}

export function AppHeader({ hasError }: AppHeaderProps) {
  return (
    <header className="border-b border-slate-900 bg-slate-900/40 backdrop-blur-md px-6 py-4 flex items-center justify-between">
      <div className="flex items-center gap-3">
        <div
          className={`h-3 w-3 rounded-full active-glow ${
            hasError ? "bg-amber-500" : "bg-rose-500 animate-pulse"
          }`}
        />
        <h1 className="text-xl font-bold tracking-tight bg-linear-to-r from-rose-400 to-amber-300 bg-clip-text text-transparent">
          ChronosDLQ // Surgeon Dashboard
        </h1>
      </div>
      <div
        className={`text-xs font-mono px-3 py-1.5 rounded border ${
          hasError
            ? "text-amber-400 bg-amber-950/20 border-amber-900/50"
            : "text-slate-500 bg-slate-900 border-slate-800"
        }`}
      >
        SYSTEM STATUS: {hasError ? "DEGRADED_ERR" : "ONLINE"}
      </div>
    </header>
  );
}
