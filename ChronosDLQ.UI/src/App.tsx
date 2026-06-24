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
      .then(() => alert("Payload successfully processed and re-queued!"))
      .catch((err: unknown) => {
        alert(
          err instanceof Error ? err.message : "Replay engine execution failure.",
        );
      });
  };

  return (
    <div className="min-h-screen flex flex-col bg-slate-950 font-display text-slate-100">
      <AppHeader hasError={Boolean(error)} />

      <main className="flex-1 grid grid-cols-12 overflow-hidden h-[calc(100vh-69px)]">
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
