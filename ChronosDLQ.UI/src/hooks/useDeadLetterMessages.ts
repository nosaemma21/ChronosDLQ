import { useCallback, useEffect, useState } from "react";
import { api } from "../services/api";
import { type DeadLetterMessage } from "../types";
import { getReplayTargetQueue } from "../utils/queue";

export function useDeadLetterMessages() {
  const [messages, setMessages] = useState<DeadLetterMessage[]>([]);
  const [selectedMessage, setSelectedMessage] =
    useState<DeadLetterMessage | null>(null);
  const [editedPayload, setEditedPayload] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchMessages = useCallback(async () => {
    try {
      const data = await api.getMessages();
      setMessages(data);

      if (selectedMessage) {
        const stillExists = data.find(
          (message) => message.messageId === selectedMessage.messageId,
        );

        if (!stillExists) {
          setSelectedMessage(null);
          setEditedPayload("");
        }
      } else if (data.length > 0) {
        setSelectedMessage(data[0]);
        setEditedPayload(data[0].rawPayload);
      }

      setError(null);
    } catch (err: unknown) {
      setError(
        err instanceof Error
          ? err.message
          : "An unexpected network failure occurred.",
      );

      // clearing ui ghost shell (fix)
      setSelectedMessage(null);
      setEditedPayload("");
      setMessages([]);
    } finally {
      setIsLoading(false);
    }
  }, [selectedMessage]);

  useEffect(() => {
    const initialTimer = setTimeout(() => void fetchMessages(), 0);
    const interval = setInterval(fetchMessages, 3000);

    return () => {
      clearTimeout(initialTimer);
      clearInterval(interval);
    };
  }, [fetchMessages]);

  const selectMessage = (message: DeadLetterMessage) => {
    setSelectedMessage(message);
    setEditedPayload(message.rawPayload);
  };

  const replaySelectedMessage = async () => {
    if (!selectedMessage) return;

    try {
      JSON.parse(editedPayload);
    } catch {
      throw new Error(
        "Invalid JSON format. Please resolve the syntax before executing replay.",
      );
    }

    setIsSubmitting(true);

    try {
      await api.replayMessage({
        messageId: selectedMessage.messageId,
        targetQueue: getReplayTargetQueue(selectedMessage.queueName),
        modifiedPayload: editedPayload,
      });

      setSelectedMessage(null);
      setEditedPayload("");
      await fetchMessages();
    } finally {
      setIsSubmitting(false);
    }
  };

  return {
    messages,
    selectedMessage,
    editedPayload,
    isLoading,
    isSubmitting,
    error,
    setEditedPayload,
    selectMessage,
    replaySelectedMessage,
  };
}
