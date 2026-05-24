// Type definitions
export interface Course {
  id: string;
  title: string;
  description: string;
  lessonCount: number;
  createdAt: string;
}

export interface CourseDetails {
  id: string;
  title: string;
  description: string;
  createdAt: string;
  updatedAt: string;
  lessons: LessonSummary[];
}

export interface LessonSummary {
  id: string;
  title: string;
  order: number;
  hasVideo: boolean;
}

export interface LessonDetails {
  id: string;
  title: string;
  description: string;
  courseId: string;
  order: number;
  videoFileId: string | null;
  playbackUrl: string | null;
  videoStatus: "Pending" | "Processing" | "Ready" | "Failed" | null;
  createdAt: string;
  updatedAt: string;
}

export interface FileStatus {
  fileId: string;
  status: "Pending" | "Processing" | "Ready" | "Failed";
  hlsManifestPath: string | null;
  createdAt: string;
  processedAt: string | null;
}

export interface UploadResult {
  fileId: string;
  originalFileName: string;
  status: string;
}

export interface CreateCourseRequest {
  title: string;
  description: string;
}

export interface CreateLessonRequest {
  title: string;
  description: string;
  courseId: string;
  order: number;
  videoFileId?: string;
}

export interface UpdateLessonRequest {
  title: string;
  description: string;
  order: number;
  videoFileId?: string;
}

export interface QualityOption {
  label: string; // "Auto", "1080p", "720p", "480p", "360p"
  value: string; // "auto", "1080p", "720p", "480p", "360p"
}

export interface PlaybackInfo {
  fileId: string;
  url: string;
  expiresIn: number;
  availableQualities: string[]; // ["360p", "480p", "720p", "1080p"]
}
