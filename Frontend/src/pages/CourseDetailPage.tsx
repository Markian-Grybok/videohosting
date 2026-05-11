import React, { useState, useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useCourse, useDeleteCourse, useUpdateCourse } from "../hooks/useCourses";
import { useLessons, useDeleteLesson } from "../hooks/useLessons";
import Layout from "../components/layout/Layout";
import Button from "../components/ui/Button";
import Card, { CardHeader, CardBody } from "../components/ui/Card";
import LessonCard from "../components/lessons/LessonCard";
import LessonForm from "../components/lessons/LessonForm";
import Input, { Textarea } from "../components/ui/Input";
import { Pencil } from "lucide-react";

const CourseDetailPage: React.FC = () => {
    const { courseId } = useParams<{ courseId: string }>();
    const navigate = useNavigate();
    const [showCreateModal, setShowCreateModal] = useState(false);

    // Edit course state
    const [editingCourse, setEditingCourse] = useState(false);
    const [editTitle, setEditTitle] = useState("");
    const [editDescription, setEditDescription] = useState("");

    const { data: course, isLoading: courseLoading } = useCourse(courseId!);
    const { data: lessons = [], isLoading: lessonsLoading } = useLessons(courseId);
    const deleteCourseMutation = useDeleteCourse();
    const deleteLessonMutation = useDeleteLesson();
    const updateCourseMutation = useUpdateCourse();

    // Initialize edit fields when course loads
    useEffect(() => {
        if (course) {
            setEditTitle(course.title);
            setEditDescription(course.description);
        }
    }, [course]);

    useEffect(() => {
        if (!course?.lessons) return;
        const lessonsWithVideo = course.lessons.filter(l => l.hasVideo);
        if (lessonsWithVideo.length === 0) return;

        Promise.all(
            lessonsWithVideo.map(l =>
                fetch(`/api/lessons/${l.id}`)
                    .then(r => r.json())
                    .then(data => ({ id: l.id, status: data.videoStatus ?? null }))
                    .catch(() => ({ id: l.id, status: null }))
            )
        ).then(results => {
            const map: Record<string, string> = {};
            results.forEach(r => { if (r.status) map[r.id] = r.status; });
        });
    }, [course?.lessons]);

    const handleUpdateCourse = async () => {
        if (!courseId || !editTitle.trim()) return;
        await updateCourseMutation.mutateAsync({
            id: courseId,
            data: { title: editTitle, description: editDescription }
        });
        setEditingCourse(false);
    };

    if (courseLoading) {
        return (
            <Layout>
                <div className="animate-pulse">
                    <div className="h-8 bg-gray-200 rounded w-32 mb-4"></div>
                    <div className="h-12 bg-gray-200 rounded w-96 mb-2"></div>
                    <div className="h-4 bg-gray-200 rounded w-64"></div>
                </div>
            </Layout>
        );
    }

    if (!course) {
        return (
            <Layout>
                <div className="text-center py-12">
                    <h2 className="text-xl font-semibold text-gray-900 mb-2">
                        Курс не знайдено
                    </h2>
                    <Button onClick={() => navigate("/courses")}>
                        Повернутися до курсів
                    </Button>
                </div>
            </Layout>
        );
    }

    const handleDeleteCourse = async () => {
        if (window.confirm("Ви впевнені, що хочете видалити цей курс?")) {
            await deleteCourseMutation.mutateAsync(course.id);
            navigate("/courses");
        }
    };

    const handleDeleteLesson = async (lessonId: string) => {
        if (window.confirm("Ви впевнені, що хочете видалити цей урок?")) {
            await deleteLessonMutation.mutateAsync(lessonId);
        }
    };

    const handleCreateLessonSuccess = () => {
        setShowCreateModal(false);
    };

    const nextOrder = lessons.length + 1;

    return (
        <Layout>
            <div className="mb-6">
                <Button
                    variant="ghost"
                    onClick={() => navigate("/courses")}
                    className="mb-4"
                >
                    ← Курси
                </Button>

                <div className="flex justify-between items-start">
                    <div className="flex-1">
                        {editingCourse ? (
                            <div className="flex flex-col gap-3 max-w-md">
                                <Input
                                    label="Назва курсу"
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
                                        loading={updateCourseMutation.isPending}
                                        onClick={handleUpdateCourse}
                                    >
                                        Зберегти
                                    </Button>
                                    <Button
                                        variant="secondary"
                                        size="sm"
                                        onClick={() => setEditingCourse(false)}
                                    >
                                        Скасувати
                                    </Button>
                                </div>
                            </div>
                        ) : (
                            <div className="flex items-start gap-2">
                                <div>
                                    <h1 className="text-3xl font-bold text-gray-900 mb-2">
                                        {course.title}
                                    </h1>
                                    <p className="text-gray-600">{course.description}</p>
                                </div>
                                <Button
                                    variant="ghost"
                                    size="sm"
                                    onClick={() => setEditingCourse(true)}
                                    className="mt-1"
                                >
                                    <Pencil className="w-4 h-4" />
                                </Button>
                            </div>
                        )}
                    </div>
                    <Button
                        variant="danger"
                        onClick={handleDeleteCourse}
                        disabled={deleteCourseMutation.isPending}
                    >
                        Видалити курс
                    </Button>
                </div>
            </div>

            <div className="mb-6">
                <div className="flex justify-between items-center mb-4">
                    <h2 className="text-xl font-semibold">Уроки</h2>
                    <Button onClick={() => setShowCreateModal(true)}>
                        Додати урок
                    </Button>
                </div>

                {lessonsLoading ? (
                    <div className="space-y-3">
                        {Array.from({ length: 3 }).map((_, i) => (
                            <Card key={i} className="animate-pulse">
                                <CardBody className="p-4">
                                    <div className="flex items-center gap-4">
                                        <div className="w-10 h-10 bg-gray-200 rounded-full"></div>
                                        <div className="flex-1">
                                            <div className="h-4 bg-gray-200 rounded w-48 mb-2"></div>
                                            <div className="h-3 bg-gray-200 rounded w-24"></div>
                                        </div>
                                    </div>
                                </CardBody>
                            </Card>
                        ))}
                    </div>
                ) : lessons.length === 0 ? (
                    <Card>
                        <CardBody className="text-center py-8">
                            <div className="text-4xl mb-4">📝</div>
                            <h3 className="text-lg font-medium text-gray-900 mb-2">
                                Уроків ще немає
                            </h3>
                            <Button onClick={() => setShowCreateModal(true)}>
                                Додати перший урок
                            </Button>
                        </CardBody>
                    </Card>
                ) : (
                    <div className="space-y-3">
                        {lessons.map((lesson) => (
                            <LessonCard
                                key={lesson.id}
                                lesson={{
                                    id: lesson.id,
                                    title: lesson.title,
                                    order: lesson.order,
                                    hasVideo: lesson.videoStatus === "Ready" || !!lesson.playbackUrl
                                }}
                                courseId={course.id}
                                onDelete={() => handleDeleteLesson(lesson.id)}
                            />
                        ))}
                    </div>
                )}
            </div>

            {showCreateModal && (
                <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4">
                    <Card className="max-w-lg w-full">
                        <CardHeader>
                            <h2 className="text-lg font-semibold">Новий урок</h2>
                        </CardHeader>
                        <CardBody>
                            <LessonForm
                                courseId={course.id}
                                nextOrder={nextOrder}
                                onSuccess={handleCreateLessonSuccess}
                                onCancel={() => setShowCreateModal(false)}
                            />
                        </CardBody>
                    </Card>
                </div>
            )}
        </Layout>
    );
};

export default CourseDetailPage;