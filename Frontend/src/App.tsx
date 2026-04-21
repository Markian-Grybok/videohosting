import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import UploadPage from "./pages/UploadPage";
import LessonPage from "./pages/LessonPage";
import CoursesPage from "./pages/CoursesPage.tsx";
import CourseDetailPage from "./pages/CourseDetailPage.tsx";
// Імпортуйте інші сторінки, коли вони будуть готові
// import CoursesPage from "./pages/CoursesPage";
// import CourseDetailPage from "./pages/CourseDetailPage";

const queryClient = new QueryClient({
    defaultOptions: {
        queries: {
            retry: 1,
            staleTime: 30_000,
        },
    },
});

function App() {
    return (
        <QueryClientProvider client={queryClient}>
            <BrowserRouter>
                <Routes>
                    {/* Redirect root to courses */}
                    <Route path="/" element={<Navigate to="/courses" />} />

                    {/* Course pages (to be implemented) */}
                    <Route path="/courses" element={<CoursesPage />} />
                    <Route path="/courses/:courseId" element={<CourseDetailPage />} />

                    {/* Lesson page */}
                    <Route path="/courses/:courseId/lessons/:lessonId" element={<LessonPage />} />

                    {/* Upload page */}
                    <Route path="/upload" element={<UploadPage />} />

                    {/* 404 fallback */}
                    <Route path="*" element={<div>Сторінку не знайдено</div>} />
                </Routes>
            </BrowserRouter>
        </QueryClientProvider>
    );
}

export default App;