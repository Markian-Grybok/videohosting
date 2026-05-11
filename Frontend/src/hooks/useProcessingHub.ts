import { useState, useEffect } from "react";
import * as signalR from "@microsoft/signalr";

interface ProcessingUpdate {
    fileId: string;
    status: string;
    progressPercent: number;
}

export function useProcessingHub(fileId: string | null) {
    const [processingStatus, setProcessingStatus] = useState<string>("Pending");
    const [processingProgress, setProcessingProgress] = useState(0);

    useEffect(() => {
        if (!fileId) {
            console.log("🔌 No fileId, skipping SignalR connection");
            return;
        }

        console.log("🔌 Connecting to SignalR for file:", fileId);

        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/processing")
            .withAutomaticReconnect()
            .build();

        connection.on("ProcessingUpdate", (update: ProcessingUpdate) => {
            console.log("📡 [SignalR] Raw update received:", update);
            console.log(`📡 [SignalR] FileId: ${update.fileId}, Status: ${update.status}, Progress: ${update.progressPercent}`);

            if (update.fileId === fileId) {
                console.log(`✅ [SignalR] Match! Updating state: ${update.status} @ ${update.progressPercent}%`);
                setProcessingStatus(update.status);
                setProcessingProgress(update.progressPercent);
            } else {
                console.log(`⚠️ [SignalR] FileId mismatch: got ${update.fileId}, expected ${fileId}`);
            }
        });

        connection.start()
            .then(() => {
                console.log("✅ SignalR connected");
                return connection.invoke("SubscribeToFile", fileId);
            })
            .then(() => console.log("✅ Subscribed to file:", fileId))
            .catch(err => console.error("❌ SignalR error:", err));

        return () => {
            console.log("🔌 Disconnecting SignalR for file:", fileId);
            connection.stop();
        };
    }, [fileId]);

    console.log(`📊 Current state: status=${processingStatus}, progress=${processingProgress}`);

    return { processingStatus, processingProgress };
}