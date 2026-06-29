import { useState } from "react";
import { type DeadLetterMessage } from "../types";
import { EmptyInspectionState } from "./EmptyInspectionState";
import { api } from "../services/api";
import { showPixelAlert, showPixelConfirm } from "../utils/alerts";

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

    const result = await showPixelConfirm({
      title: "Confirm Decapitation",
      text: "Are you sure you want to permanently discard this trace?",
      confirmButtonText: "Yes, purge trace",
    });

    if (!result.isConfirmed) return;

    setIsDiscarding(true);
    try {
      await api.discardMessage(selectedMessage.messageId);
      await showPixelAlert({
        icon: "success",
        title: "Purged!",
        text: "Message trace successfully purged from control plane.",
        timer: 2000,
      });
    } catch (err: unknown) {
      await showPixelAlert({
        icon: "error",
        title: "Execution Failure",
        text:
          err instanceof Error
            ? err.message
            : "Purge routine execution failure.",
      });
    } finally {
      setIsDiscarding(false);
    }
  };

  const anyActiveNetworkAction = isSubmitting || isDiscarding;

  return (
    <section className="pixel-panel col-span-7 flex min-h-0 min-w-0 flex-col gap-4 overflow-hidden p-4">
      {selectedMessage ? (
        <>
          <div className="flex shrink-0 flex-col gap-3 border-b-2 border-[#263e56] pb-3">
            <div className="flex items-start justify-between gap-4">
              <div className="min-w-0">
                <h2 className="pixel-title text-2xl font-bold text-[#f6f1dc]">
                  Surgical Inspection Module
                </h2>
              </div>
              <div className="flex shrink-0 gap-2">
                <button
                  onClick={handleDiscardMessage}
                  disabled={anyActiveNetworkAction}
                  className="pixel-button cursor-pointer bg-[#8f4f52] px-3.5 py-1.5 font-pixel text-sm font-medium uppercase leading-none text-[#1f0b0d] transition-all hover:bg-[#b86c6f] disabled:bg-[#263849] disabled:text-[#6d8fb0]"
                >
                  {isDiscarding ? "Purging..." : "Discard Trace"}
                </button>
                <button
                  onClick={onReplay}
                  disabled={anyActiveNetworkAction}
                  className="pixel-button cursor-pointer bg-[#79d957] px-3.5 py-1.5 font-pixel text-sm font-medium uppercase leading-none text-[#10210d] transition-all hover:bg-[#9cff78] disabled:bg-[#263849] disabled:text-[#6d8fb0]"
                >
                  {isSubmitting ? "Replaying..." : "Execute Replay Wizard"}
                </button>
              </div>
            </div>
            <p className="break-all font-mono text-xs leading-relaxed text-[#9fb7cc]">
              Target Trace Boundary: {selectedMessage.messageId}
            </p>
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
