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
    <section className="pixel-panel col-span-8 flex min-h-0 min-w-0 flex-col gap-4 overflow-hidden p-4">
      {selectedMessage ? (
        <>
          <div className="flex shrink-0 items-start justify-between gap-4 border-b-2 border-[#263e56] pb-3">
            <div className="min-w-0">
              <h2 className="pixel-title text-3xl font-bold text-[#f6f1dc]">
                Surgical Inspection Module
              </h2>
              <p className="mt-1 truncate font-mono text-sm text-[#d8e6f2]">
                Target Trace Boundary: {selectedMessage.messageId}
              </p>
            </div>
            <div className="flex shrink-0 gap-2">
              <button
                onClick={handleDiscardMessage}
                disabled={anyActiveNetworkAction}
                className="pixel-button pixel-title cursor-pointer bg-[#8f4f52] px-5 py-2 text-xl font-bold leading-none text-[#1f0b0d] transition-all hover:bg-[#b86c6f] disabled:bg-[#263849] disabled:text-[#6d8fb0]"
              >
                {isDiscarding ? "Purging..." : "Discard Trace"}
              </button>
              <button
                onClick={onReplay}
                disabled={anyActiveNetworkAction}
                className="pixel-button pixel-title cursor-pointer bg-[#79d957] px-5 py-2 text-xl font-bold leading-none text-[#10210d] transition-all hover:bg-[#9cff78] disabled:bg-[#263849] disabled:text-[#6d8fb0]"
              >
                {isSubmitting ? "Replaying..." : "Execute Replay Wizard"}
              </button>
            </div>
          </div>

          <div className="shrink-0 border-2 border-[#a51f31] bg-[#0b1523] shadow-[3px_3px_0_#020617]">
            <label className="pixel-title block border-b-2 border-[#a51f31] bg-[#8d1c2b] px-3 py-1 text-xl font-bold uppercase text-[#f6f1dc]">
              Diagnostics / Exception Log
            </label>
            <div className="max-h-36 overflow-auto p-3 font-mono text-sm leading-relaxed whitespace-pre-wrap text-[#ff6d76]">
              {selectedMessage.exceptionMessage}
            </div>
          </div>

          <div className="flex min-h-0 flex-1 flex-col border-2 border-[#52718e] bg-[#09121e] shadow-[3px_3px_0_#020617]">
            <label className="pixel-title block border-b-2 border-[#52718e] bg-[#345b78] px-3 py-1 text-xl font-bold uppercase text-[#f6f1dc]">
              Modify Execution Payload
            </label>
            <textarea
              value={editedPayload}
              onChange={(event) => onPayloadChange(event.target.value)}
              className="min-h-0 flex-1 resize-none overflow-auto border-0 bg-[#08111f] p-4 font-mono text-sm leading-relaxed text-[#5ef0c2] selection:bg-[#263e56] focus:outline-hidden"
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
