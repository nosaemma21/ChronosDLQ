import Swal from "sweetalert2";
import { AppHeader } from "./components/AppHeader";
import { MessageStream } from "./components/MessageStream";
import { MessageWorkspace } from "./components/MessageWorkspace";
import { useDeadLetterMessages } from "./hooks/useDeadLetterMessages";

export default function App() {
  const {
    messages,
    selectedMessage,
    editedPayload,
    isLoading,
    isSubmitting,
    error,
    setEditedPayload,
    selectMessage,
    replaySelectedMessage,
  } = useDeadLetterMessages();

  const handleExecuteReplay = () => {
    void replaySelectedMessage()
      .then(() => {
        void Swal.fire({
          toast: true,
          position: "bottom-end",
          icon: "success",
          title: "Telemetry Re-queued",
          text: "Payload successfully processed and re-queued!",
          showConfirmButton: false,
          timer: 3000,
          timerProgressBar: true,
          background: "#0f172a",
          color: "#f8fafc",
          customClass: {
            popup: "border border-emerald-500/20 max-w-sm rounded-xl",
          },
        });
      })
      .catch((err: unknown) => {
        void Swal.fire({
          toast: true,
          position: "bottom-end",
          icon: "error",
          title: "Engine Failure",
          text: err instanceof Error ? err.message : "Replay failed",
          showConfirmButton: false,
          timer: 3000,
          timerProgressBar: true,
          background: "#0f172a",
          color: "#f8fafc",
          customClass: {
            popup: "border border-rose-500/20 max-w-sm rounded-xl",
          },
        });
      });
  };

  return (
    <div className="font-display flex min-h-screen flex-col bg-slate-950 text-slate-100">
      <AppHeader hasError={Boolean(error)} />

      <main className="grid h-[calc(100vh-69px)] flex-1 grid-cols-12 overflow-hidden">
        <MessageStream
          messages={messages}
          selectedMessageId={selectedMessage?.messageId}
          isLoading={isLoading}
          error={error}
          onSelectMessage={selectMessage}
        />
        <MessageWorkspace
          selectedMessage={selectedMessage}
          editedPayload={editedPayload}
          isSubmitting={isSubmitting}
          onPayloadChange={setEditedPayload}
          onReplay={handleExecuteReplay}
        />
      </main>
    </div>
  );
}
