import React, { useState, useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useQueryClient } from "@tanstack/react-query";
import { Upload, Film, Loader2, AlertCircle, Pencil, Trash2 } from "lucide-react";
import Layout from "../components/layout/Layout";
import Card, { CardBody } from "../components/ui/Card";
import Button from "../components/ui/Button";
import VideoPlayer from "../components/lessons/VideoPlayer";
import { fileApi } from "../api/fileApi";
import VideoUploadSection from "../components/lessons/VideoUploadSection";
import Input, { Textarea } from "../components/ui/Input";
import { useLesson, useUpdateLesson } from "../hooks/useLessons";

const LessonPage: React.FC = () => {
    const { courseId, lessonId } = useParams<{ courseId: string; lessonId: string }>();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const [showUpload, setShowUpload] = useState(false);
    const [availableQualities, setAvailableQualities] = useState<string[]>([]);

    // Edit lesson state
    const [editingLesson, setEditingLesson] = useState(false);
    const [editTitle, setEditTitle] = useState("");
    const [editDescription, setEditDescription] = useState("");

    // Delete video state
    const [isDeletingVideo, setIsDeletingVideo] = useState(false);

    const { data: lesson, isLoading, error } = useLesson(lessonId!);
    const { mutateAsync: updateLesson, isPending: isUpdatingLesson } = useUpdateLesson();

    // Initialize edit fields when lesson loads
    useEffect(() => {
        if (lesson) {
            setEditTitle(lesson.title);
            setEditDescription(lesson.description);
        }
    }, [lesson]);

    // Fetch available qualities for the lesson video (if any)
    useEffect(() => {
        if (!lesson?.videoFileId) {
            setAvailableQualities([]);
            return;
        }

        fileApi.getAvailableQualities(lesson.videoFileId)
            .then(data => setAvailableQualities(data.qualities || []))
            .catch(() => setAvailableQualities([]));
    }, [lesson?.videoFileId]);

    const formatDate = (dateString: string) => {
        return new Date(dateString).toLocaleDateString("uk-UA", {
            year: "numeric",
            month: "long",
            day: "numeric",
        });
    };

    const handleUpdateLesson = async () => {
        if (!lessonId || !editTitle.trim()) return;
        await updateLesson({
            id: lessonId,
            data: {
                title: editTitle,
                description: editDescription,
                order: lesson!.order,
                videoFileId: lesson!.videoFileId ?? undefined,
            }
        });
        setEditingLesson(false);
    };

    const handleDeleteVideo = async () => {
        if (!lesson?.videoFileId) return;

        const confirmed = window.confirm(
            "Видалити відео з цього уроку? Урок залишиться, але відео буде видалено безповоротно."
        );
        if (!confirmed) return;

        setIsDeletingVideo(true);
        try {
            // Step 1: delete file from FileService
            const res = await fetch(`/api/files/${lesson.videoFileId}`, {
                method: "DELETE",
            });

            if (!res.ok && res.status !== 404) {
                throw new Error("Failed to delete file from FileService");
            }

            // Step 2: unlink video from lesson (set videoFileId to null)
            await fetch(`/api/lessons/${lessonId}`, {
                method: "PUT",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    title: lesson.title,
                    description: lesson.description,
                    order: lesson.order,
                    videoFileId: null,
                }),
            });

            // Step 3: ВИПРАВЛЕНИЙ КЛЮЧ!
            queryClient.invalidateQueries({ queryKey: ["lessons", "detail", lessonId] });
            // або ще краще — інвалідувати всі, що починаються з "lessons":
            // queryClient.invalidateQueries({ queryKey: ["lessons"] });

        } catch (err) {
            console.error("Failed to delete video:", err);
            window.alert("Помилка при видаленні відео. Спробуйте ще раз.");
        } finally {
            setIsDeletingVideo(false);
        }
    };

    if (isLoading) {
        return (
            <Layout>
                <div className="animate-pulse">
                    <div className="h-8 w-48 bg-gray-200 rounded mb-6" />
                    <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
                        <div className="lg:col-span-2">
                            <div className="bg-gray-200 rounded-xl aspect-video" />
                        </div>
                        <div className="space-y-4">
                            <div className="h-6 w-24 bg-gray-200 rounded" />
                            <div className="h-8 w-full bg-gray-200 rounded" />
                            <div className="h-20 w-full bg-gray-200 rounded" />
                        </div>
                    </div>
                </div>
            </Layout>
        );
    }

    if (error || !lesson) {
        return (
            <Layout>
                <div className="text-center py-12">
                    <div className="w-16 h-16 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-4">
                        <AlertCircle className="w-8 h-8 text-red-600" />
                    </div>
                    <h2 className="text-xl font-semibold text-gray-900 mb-2">Урок не знайдено</h2>
                    <p className="text-gray-500 mb-6">
                        Можливо, урок було видалено або ви перейшли за неправильним посиланням
                    </p>
                    <Button variant="primary" onClick={() => navigate(`/courses/${courseId}`)}>
                        ← Повернутися до курсу
                    </Button>

                </div>
            </Layout>
        );
    }

    const videoStatus = lesson.videoStatus;
    const playbackUrl = lesson.playbackUrl;
    const hasVideo = !!lesson.videoFileId;
    const canDeleteVideo = hasVideo && videoStatus !== "Processing";

    const renderVideoContent = () => {
        if (videoStatus === "Ready" && playbackUrl) {
            return (
                <>
                    <VideoPlayer
                        fileId={lesson.videoFileId!}
                        initialUrl={playbackUrl}
                        availableQualities={availableQualities}
                    />
                    {canDeleteVideo && (
                        <div className="mt-3 flex justify-end">
                            <Button
                                variant="danger"
                                size="sm"
                                loading={isDeletingVideo}
                                onClick={handleDeleteVideo}
                            >
                                <Trash2 className="w-4 h-4 mr-1" />
                                Видалити відео
                            </Button>
                        </div>
                    )}
                </>
            );
        }

        if (videoStatus === "Processing") {
            return (
                <div className="bg-gray-50 rounded-xl border border-gray-200 p-12 text-center">
                    <div className="w-16 h-16 bg-blue-100 rounded-full flex items-center justify-center mx-auto mb-4">
                        <Loader2 className="w-8 h-8 text-blue-600 animate-spin" />
                    </div>
                    <h3 className="text-lg font-semibold text-gray-900 mb-2">Відео обробляється</h3>
                    <p className="text-gray-500">
                        Будь ласка, зачекайте. Це може зайняти кілька хвилин.
                    </p>
                </div>
            );
        }

        if (videoStatus === "Failed") {
            return (
                <div className="bg-red-50 rounded-xl border border-red-200 p-12 text-center">
                    <div className="w-16 h-16 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-4">
                        <AlertCircle className="w-8 h-8 text-red-600" />
                    </div>
                    <h3 className="text-lg font-semibold text-red-700 mb-2">
                        Помилка обробки відео
                    </h3>
                    <p className="text-red-600 mb-4">
                        Не вдалося обробити відео. Спробуйте завантажити його знову.
                    </p>
                    <Button variant="primary" onClick={() => setShowUpload(true)}>
                        Завантажити нове відео
                    </Button>
                </div>
            );
        }

        if (!hasVideo) {
            return (
                <div className="bg-gray-50 rounded-xl border-2 border-dashed border-gray-200 p-12 text-center">
                    <div className="w-16 h-16 bg-gray-100 rounded-full flex items-center justify-center mx-auto mb-4">
                        <Film className="w-8 h-8 text-gray-400" />
                    </div>
                    <h3 className="text-lg font-semibold text-gray-900 mb-2">
                        Відео ще не завантажено
                    </h3>
                    <p className="text-gray-500 mb-4">Додайте відео до цього уроку</p>
                    <Button variant="primary" onClick={() => setShowUpload(true)}>
                        <Upload className="w-4 h-4 mr-2" />
                        Завантажити відео
                    </Button>
                </div>
            );
        }

        return null;
    };

    return (
        <Layout>
            {/* Back button */}
            <button
                onClick={() => navigate(`/courses/${courseId}`)}
                className="flex items-center gap-2 text-gray-600 hover:text-gray-900 mb-6 transition-colors"
            >
                ← Урок {lesson.order}: {lesson.title}
            </button>

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
                {/* Left column - Video player */}
                <div className="lg:col-span-2">
                    {!hasVideo && showUpload ? (
                        <VideoUploadSection
                            lessonId={lessonId!}
                            lessonTitle={lesson.title}
                            lessonDescription={lesson.description}
                            lessonOrder={lesson.order}
                            onSuccess={() => {
                                setShowUpload(false);
                                // ВИПРАВЛЕНО: правильний ключ для інвалідації
                                queryClient.invalidateQueries({ queryKey: ["lessons"] });
                                // або конкретний ключ:
                                // queryClient.invalidateQueries({ queryKey: ["lessons", "detail", lessonId] });
                            }}
                        />
                    ) : (
                        renderVideoContent()
                    )}
                </div>

                {/* Right column - Lesson info */}
                <div className="lg:col-span-1">
                    <Card>
                        <CardBody>
                            <div className="mb-4">
                                <span className="text-sm text-gray-500">Урок {lesson.order}</span>
                                {editingLesson ? (
                                    <div className="flex flex-col gap-3 mt-2">
                                        <Input
                                            label="Назва уроку"
                                            value={editTitle}
                                            onChange={e => setEditTitle(e.target.value)}
                                        />
                                        <Textarea
                                            label="Опис"
                                            value={editDescription}
                                            onChange={e => setEditDescription(e.target.value)}
                                        />
                                        <div className="flex gap-2">
                                            <Button
                                                variant="primary"
                                                size="sm"
                                                loading={isUpdatingLesson}
                                                onClick={handleUpdateLesson}
                                            >
                                                Зберегти
                                            </Button>
                                            <Button
                                                variant="secondary"
                                                size="sm"
                                                onClick={() => setEditingLesson(false)}
                                            >
                                                Скасувати
                                            </Button>
                                        </div>
                                    </div>
                                ) : (
                                    <div className="flex items-start gap-2">
                                        <h1 className="text-2xl font-bold text-gray-900 mt-1">
                                            {lesson.title}
                                        </h1>
                                        <Button
                                            variant="ghost"
                                            size="sm"
                                            onClick={() => setEditingLesson(true)}
                                            className="mt-1"
                                        >
                                            <Pencil className="w-4 h-4" />
                                        </Button>
                                    </div>
                                )}
                            </div>

                            {!editingLesson && (
                                <p className="text-gray-600 leading-relaxed mb-6">{lesson.description}</p>
                            )}

                            <hr className="border-gray-100 my-4" />

                            <div className="space-y-3">
                                {/* <div className="flex justify-between items-center">
                                    <span className="text-sm text-gray-500">Статус відео</span>
                                    <Badge status={videoStatus || "Pending"} />
                                </div>

                                {lesson.videoFileId && (
                                    <div className="flex justify-between items-center">
                                        <span className="text-sm text-gray-500">ID файлу</span>
                                        <span className="text-xs font-mono text-gray-400">
                      {lesson.videoFileId.slice(0, 8)}...
                    </span>
                                    </div>
                                )} */}

                                <div className="flex justify-between items-center">
                                    <span className="text-sm text-gray-500">Створено</span>
                                    <span className="text-sm text-gray-600">
                    {formatDate(lesson.createdAt)}
                  </span>
                                </div>

                                {lesson.updatedAt !== lesson.createdAt && (
                                    <div className="flex justify-between items-center">
                                        <span className="text-sm text-gray-500">Оновлено</span>
                                        <span className="text-sm text-gray-600">
                      {formatDate(lesson.updatedAt)}
                    </span>
                                    </div>
                                )}
                            </div>
                        </CardBody>
                    </Card>
                </div>
            </div>
        </Layout>
    );
};

export default LessonPage;