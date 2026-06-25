import { useState } from "react";
import { type DeadLetterMessage } from "../types";
import { EmptyInspectionState } from "./EmptyInspectionState";
import { api } from "../services/api"; // Pulling in your api client service directly

interface MessageWorkspaceProps {
  selectedMessage: DeadLetterMessage | null;
  editedPayload: string;
  isSubmitting: boolean;
  onPayloadChange: (payload: string) => void;
  onReplay: () => void;
}

export function MessageWorkspace({
  selectedMessage,
  editedPayload,
  isSubmitting,
  onPayloadChange,
  onReplay,
}: MessageWorkspaceProps) {
  // Local UI lock specifically for handling the discard network phase
  const [isDiscarding, setIsDiscarding] = useState<boolean>(false);

  const handleDiscardMessage = async () => {
    if (!selectedMessage) return;

    if (
      !window.confirm(
        "Are you sure you want to permanently discard this trace?",
      )
    ) {
      return;
    }

    setIsDiscarding(true);
    try {
      // Execute direct backend service eviction call using the ID prop we have
      await api.discardMessage(selectedMessage.messageId);
      alert("Message trace successfully purged from control plane.");

      // No state setters needed here!
      // Your App.tsx 3-second polling loop will instantly clear this card on its next tick.
    } catch (err: unknown) {
      alert(
        err instanceof Error ? err.message : "Purge routine execution failure.",
      );
    } finally {
      setIsDiscarding(false);
    }
  };

  // Combine both submission states to keep buttons disabled during any active network traffic
  const anyActiveNetworkAction = isSubmitting || isDiscarding;

  return (
    <section className="col-span-8 bg-slate-950/20 p-6 overflow-y-auto flex flex-col space-y-6">
      {selectedMessage ? (
        <>
          <div className="bg-slate-900/30 border border-slate-900 p-4 rounded-xl flex justify-between items-start">
            <div>
              <h2 className="text-lg font-semibold text-slate-200">
                Surgical Inspection Module
              </h2>
              <p className="text-xs font-mono text-slate-500 mt-1">
                Target Trace Boundary: {selectedMessage.messageId}
              </p>
            </div>
            <div className="flex gap-3">
              <button
                onClick={handleDiscardMessage}
                disabled={anyActiveNetworkAction}
                className="px-4 py-2 bg-slate-900 hover:bg-slate-800 text-rose-400 border border-rose-950 hover:border-rose-900 disabled:opacity-50 font-medium text-sm rounded-lg transition-all cursor-pointer"
              >
                {isDiscarding ? "Purging..." : "Discard Trace"}
              </button>
              <button
                onClick={onReplay}
                disabled={anyActiveNetworkAction}
                className="px-4 py-2 bg-emerald-600 hover:bg-emerald-50 text-emerald-950 disabled:bg-slate-800 disabled:text-slate-500 font-semibold text-sm rounded-lg transition-all shadow-lg cursor-pointer"
              >
                {isSubmitting ? "Replaying..." : "Execute Replay Wizard"}
              </button>
            </div>
          </div>

          <div className="space-y-2">
            <label className="text-xs font-mono text-slate-500 uppercase tracking-wider block">
              Diagnostics / Exception Log
            </label>
            <div className="p-4 bg-rose-950/10 border border-rose-900/20 rounded-xl font-mono text-xs text-rose-300/90 leading-relaxed overflow-x-auto whitespace-pre-wrap">
              {selectedMessage.exceptionMessage}
            </div>
          </div>

          <div className="flex-1 flex flex-col space-y-2 min-h-[300px]">
            <label className="text-xs font-mono text-slate-500 uppercase tracking-wider block">
              Modify Execution Payload
            </label>
            <textarea
              value={editedPayload}
              onChange={(event) => onPayloadChange(event.target.value)}
              className="flex-1 p-4 bg-slate-900/40 border border-slate-900 focus:border-emerald-500/40 focus:outline-hidden rounded-xl font-mono text-xs text-emerald-400/90 leading-relaxed resize-none selection:bg-slate-800"
              spellCheck="false"
            />
          </div>
        </>
      ) : (
        <EmptyInspectionState />
      )}
    </section>
  );
}
