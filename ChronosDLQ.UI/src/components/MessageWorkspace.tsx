import { useState } from "react";
import { type DeadLetterMessage } from "../types";
import { EmptyInspectionState } from "./EmptyInspectionState";
import { api } from "../services/api";
import Swal from "sweetalert2";

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

    // if (
    //   !window.confirm(
    //     "Are you sure you want to permanently discard this trace?",
    //   )
    // ) {
    //   return;
    // }

    // alerting with sweetalert
    const result = await Swal.fire({
      title: "Confirm Decapitation",
      text: "Are you sure you want to permanently discard this trace?",
      icon: "warning",
      showCancelButton: true,
      confirmButtonColor: "#dc2626",
      cancelButtonColor: "#374151",
      confirmButtonText: "Yes, purge trace",
      background: "#0f172a",
      color: "#f8fafc",
    });

    if (!result.isConfirmed) return;

    setIsDiscarding(true);
    try {
      await api.discardMessage(selectedMessage.messageId);
      // alert("Message trace successfully purged from control plane.");
      await Swal.fire({
        title: "Purged!",
        text: "Message trace successfully purged from control plane.",
        icon: "success",
        timer: 2000,
        showConfirmButton: false,
        background: "#0f172a",
        color: "#f8fafc",
      });
    } catch (err: unknown) {
      // alert(
      //   err instanceof Error ? err.message : "Purge routine execution failure.",
      // );
      await Swal.fire({
        title: "Execution Failure",
        text:
          err instanceof Error
            ? err.message
            : "Purge routine execution failure.",
        icon: "error",
        background: "#0f172a",
        color: "#f8fafc",
      });
    } finally {
      setIsDiscarding(false);
    }
  };

  const anyActiveNetworkAction = isSubmitting || isDiscarding;

  return (
    <section className="col-span-8 flex flex-col space-y-6 overflow-y-auto bg-slate-950/20 p-6">
      {selectedMessage ? (
        <>
          <div className="flex items-start justify-between rounded-xl border border-slate-900 bg-slate-900/30 p-4">
            <div>
              <h2 className="text-lg font-semibold text-slate-200">
                Surgical Inspection Module
              </h2>
              <p className="mt-1 font-mono text-xs text-slate-500">
                Target Trace Boundary: {selectedMessage.messageId}
              </p>
            </div>
            <div className="flex gap-2">
              <button
                onClick={handleDiscardMessage}
                disabled={anyActiveNetworkAction}
                className="cursor-pointer rounded-md border border-rose-950 bg-slate-900 px-3 py-1.5 text-xs font-medium text-rose-400 transition-all hover:border-rose-900 hover:bg-slate-800 disabled:opacity-50"
              >
                {isDiscarding ? "Purging..." : "Discard Trace"}
              </button>
              <button
                onClick={onReplay}
                disabled={anyActiveNetworkAction}
                className="cursor-pointer rounded-md bg-emerald-600 px-3 py-1.5 text-xs font-semibold text-emerald-950 shadow transition-all hover:bg-emerald-50 disabled:bg-slate-800 disabled:text-slate-500"
              >
                {isSubmitting ? "Replaying..." : "Execute Replay Wizard"}
              </button>
            </div>
          </div>

          <div className="space-y-2">
            <label className="block font-mono text-xs tracking-wider text-slate-500 uppercase">
              Diagnostics / Exception Log
            </label>
            <div className="overflow-x-auto rounded-xl border border-rose-900/20 bg-rose-950/10 p-4 font-mono text-xs leading-relaxed whitespace-pre-wrap text-rose-300/90">
              {selectedMessage.exceptionMessage}
            </div>
          </div>

          <div className="flex min-h-75 flex-1 flex-col space-y-2">
            <label className="block font-mono text-xs tracking-wider text-slate-500 uppercase">
              Modify Execution Payload
            </label>
            <textarea
              value={editedPayload}
              onChange={(event) => onPayloadChange(event.target.value)}
              className="flex-1 resize-none rounded-xl border border-slate-900 bg-slate-900/40 p-4 font-mono text-xs leading-relaxed text-emerald-400/90 selection:bg-slate-800 focus:border-emerald-500/40 focus:outline-hidden"
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
