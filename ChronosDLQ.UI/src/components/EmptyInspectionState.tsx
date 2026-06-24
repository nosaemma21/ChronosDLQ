export function EmptyInspectionState() {
  return (
    <div className="flex-1 flex flex-col items-center justify-center text-center p-12 border-2 border-dashed border-slate-900 rounded-2xl">
      <p className="text-sm text-slate-500 font-mono">
        No active target trace selected for inspection.
      </p>
    </div>
  );
}
