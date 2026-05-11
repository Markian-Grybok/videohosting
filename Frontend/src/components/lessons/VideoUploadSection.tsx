import React, { useState, useRef, useEffect } from "react";
import { Upload, CheckCircle, XCircle, Check } from "lucide-react";
import Button from "../ui/Button";
import ProgressBar from "../ui/ProgressBar";
import Badge from "../ui/Badge";
import Card from "../ui/Card";
import { useProcessingHub } from "../../hooks/useProcessingHub";
import { fileApi } from "../../api/fileApi";

interface VideoUploadSectionProps {
    lessonId: string;
    lessonTitle: string;
    lessonDescription: string;
    lessonOrder: number;
    onSuccess: () => void;
}

type Phase = "select" | "uploading" | "processing" | "error";

const VideoUploadSection: React.FC<VideoUploadSectionProps> = ({
                                                                   lessonId,
                                                                   lessonTitle,
                                                                   lessonDescription,
                                                                   lessonOrder,
                                                                   onSuccess,
                                                               }) => {
    const fileInputRef = useRef<HTMLInputElement>(null);
    const [selectedFile, setSelectedFile] = useState<File | null>(null);
    const [dragOver, setDragOver] = useState(false);
    const [phase, setPhase] = useState<Phase>("select");
    const [uploadProgress, setUploadProgress] = useState(0);
    const [fileId, setFileId] = useState<string | null>(null);
    const { processingStatus, processingProgress } = useProcessingHub(fileId);

    const formatFileSize = (bytes: number): string => {
        const mb = bytes / 1024 / 1024;
        return mb.toFixed(1) + " МБ";
    };

    const handleDragOver = (e: React.DragEvent) => {
        e.preventDefault();
        setDragOver(true);
    };

    const handleDragLeave = () => {
        setDragOver(false);
    };

    const handleDrop = (e: React.DragEvent) => {
        e.preventDefault();
        setDragOver(false);
        const file = e.dataTransfer.files[0];
        if (file && file.type.startsWith("video/")) {
            setSelectedFile(file);
        }
    };

    const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
        const file = e.target.files?.[0];
        if (file) {
            setSelectedFile(file);
        }
    };

    const handleUpload = async () => {
        if (!selectedFile) return;
        setPhase("uploading");
        setUploadProgress(0);

        try {
            const result = await fileApi.uploadVideo(selectedFile, setUploadProgress);
            setFileId(result.fileId);
            setPhase("processing");
        } catch (err) {
            console.error("Upload failed:", err);
            setPhase("error");
        }
    };

    const handleCancel = () => {
        setSelectedFile(null);
        if (fileInputRef.current) {
            fileInputRef.current.value = "";
        }
    };

    const handleRetry = () => {
        setPhase("select");
        setUploadProgress(0);
        setFileId(null);
        setSelectedFile(null);
    };

    // Оновлення уроку після успішної обробки відео
    useEffect(() => {
        const updateLessonWithVideo = async () => {
            if (processingStatus === "Ready" && fileId && phase === "processing") {
                try {
                    await fetch(`/api/lessons/${lessonId}`, {
                        method: "PUT",
                        headers: { "Content-Type": "application/json" },
                        body: JSON.stringify({
                            title: lessonTitle,
                            description: lessonDescription,
                            order: lessonOrder,
                            videoFileId: fileId,
                        }),
                    });
                    console.log("✅ Lesson updated with video");

                    // Затримка перед викликом onSuccess
                    setTimeout(() => {
                        onSuccess();
                    }, 1500);
                } catch (err) {
                    console.error("Failed to update lesson:", err);
                }
            }
        };

        updateLessonWithVideo();
    }, [processingStatus, fileId, phase, lessonId, lessonTitle, lessonDescription, lessonOrder, onSuccess]);

    useEffect(() => {
        if (processingStatus === "Failed") {
            setPhase("error");
        }
    }, [processingStatus]);

    const renderSteps = () => {
        const steps = [
            { label: "Файл отримано сервером", threshold: 5 },
            { label: "Конвертація у HLS", threshold: 60 },
            { label: "Збереження сегментів", threshold: 90 },
            { label: "Готово до перегляду", threshold: 100 },
        ];

        // Якщо статус "Ready" — всі галочки зелені
        if (processingStatus === "Ready") {
            return (
                <div className="space-y-3 mt-4">
                    {steps.map((step) => (
                        <div key={step.label} className="flex items-center gap-3">
                            <div className="flex-shrink-0">
                                <div className="w-6 h-6 bg-green-500 rounded-full flex items-center justify-center">
                                    <Check className="w-4 h-4 text-white" />
                                </div>
                            </div>
                            <span className="text-sm text-gray-900 font-medium">
                {step.label}
              </span>
                        </div>
                    ))}
                </div>
            );
        }

        // Якщо статус "Processing" або "Pending" — показуємо прогрес
        return (
            <div className="space-y-3 mt-4">
                {steps.map((step) => {
                    const isDone = processingProgress >= step.threshold;
                    const isActive = !isDone && processingProgress >= step.threshold - 10 && processingStatus === "Processing";

                    return (
                        <div key={step.label} className="flex items-center gap-3">
                            <div className="flex-shrink-0">
                                {isDone ? (
                                    <div className="w-6 h-6 bg-green-500 rounded-full flex items-center justify-center">
                                        <Check className="w-4 h-4 text-white" />
                                    </div>
                                ) : isActive ? (
                                    <div className="w-6 h-6 bg-primary-500 rounded-full animate-pulse" />
                                ) : (
                                    <div className="w-6 h-6 bg-gray-200 rounded-full" />
                                )}
                            </div>
                            <span className={`text-sm ${isDone ? "text-gray-900 font-medium" : "text-gray-500"}`}>
                {step.label}
              </span>
                        </div>
                    );
                })}
            </div>
        );
    };

    // Phase: select
    if (phase === "select") {
        return (
            <div className="space-y-4">
                <div
                    className={`border-2 border-dashed rounded-xl p-10 text-center transition-colors cursor-pointer ${
                        dragOver
                            ? "border-primary-500 bg-primary-50"
                            : "border-gray-300 bg-gray-50 hover:border-primary-400 hover:bg-primary-50"
                    }`}
                    onDragOver={handleDragOver}
                    onDragLeave={handleDragLeave}
                    onDrop={handleDrop}
                    onClick={() => fileInputRef.current?.click()}
                >
                    <Upload className={`w-10 h-10 mx-auto mb-3 transition-colors ${
                        dragOver ? "text-primary-500" : "text-gray-400"
                    }`} />
                    <p className="text-sm font-medium text-gray-600 mb-1">Перетягніть відео сюди</p>
                    <p className="text-xs text-gray-400 mb-3">або</p>
                    <Button variant="secondary" size="sm">
                        Вибрати файл
                    </Button>
                    <p className="text-xs text-gray-400 mt-3">MP4, MOV, AVI · до 500 МБ</p>
                </div>

                <input
                    ref={fileInputRef}
                    type="file"
                    accept="video/mp4,video/quicktime,video/x-msvideo"
                    onChange={handleFileSelect}
                    style={{ display: "none" }}
                />

                {selectedFile && (
                    <Card>
                        <div className="p-4">
                            <div className="flex items-center gap-3 mb-4">
                                <Upload className="w-6 h-6 text-gray-400" />
                                <div className="flex-1">
                                    <p className="text-sm font-medium text-gray-900">{selectedFile.name}</p>
                                    <p className="text-xs text-gray-500">{formatFileSize(selectedFile.size)}</p>
                                </div>
                            </div>
                            <div className="flex gap-3">
                                <Button
                                    variant="primary"
                                    size="md"
                                    className="flex-1"
                                    onClick={handleUpload}
                                >
                                    Завантажити
                                </Button>
                                <Button
                                    variant="ghost"
                                    size="md"
                                    onClick={handleCancel}
                                >
                                    Скасувати
                                </Button>
                            </div>
                        </div>
                    </Card>
                )}
            </div>
        );
    }

    // Phase: uploading
    if (phase === "uploading") {
        return (
            <Card>
                <div className="p-6">
                    <div className="flex items-center gap-2 mb-4">
                        <p className="text-sm font-medium text-gray-700">Завантаження на сервер...</p>
                    </div>
                    <ProgressBar
                        value={uploadProgress}
                        status="processing"
                        label="Завантаження файлу"
                        showPercent
                    />
                </div>
            </Card>
        );
    }

    // Phase: processing (показуємо прогрес і галочки, навіть коли Ready)
    if (phase === "processing") {
        const isReady = processingStatus === "Ready";

        return (
            <Card>
                <div className="p-6">
                    <div className="flex items-center gap-2 mb-4">
                        <Badge status={processingStatus as "Pending" | "Processing" | "Ready" | "Failed"} />
                        <p className="text-sm font-semibold text-gray-900">
                            {isReady ? "Відео готове" : "Обробка відео"}
                        </p>
                    </div>

                    {!isReady && (
                        <ProgressBar
                            value={processingProgress}
                            status="processing"
                            showPercent
                        />
                    )}

                    {renderSteps()}

                    {isReady && (
                        <div className="mt-6 text-center pt-4 border-t border-gray-100">
                            <div className="w-14 h-14 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-3">
                                <CheckCircle className="w-8 h-8 text-green-600" />
                            </div>
                            <p className="text-sm text-green-700 font-medium">Завершено!</p>
                            <p className="text-xs text-gray-500 mt-1">Урок оновлено автоматично</p>
                        </div>
                    )}
                </div>
            </Card>
        );
    }

    // Phase: error
    if (phase === "error") {
        return (
            <div className="bg-white rounded-xl border border-red-200 shadow-sm p-8 text-center">
                <div className="w-14 h-14 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-4">
                    <XCircle className="w-8 h-8 text-red-600" />
                </div>
                <h3 className="text-lg font-semibold text-red-700 mb-4">
                    Помилка обробки відео
                </h3>
                <Button variant="secondary" onClick={handleRetry}>
                    Спробувати знову
                </Button>
            </div>
        );
    }

    return null;
};

export default VideoUploadSection;