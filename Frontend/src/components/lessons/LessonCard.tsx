import React from "react";
import { useNavigate } from "react-router-dom";
import { Trash2 } from "lucide-react";
import Card, { CardBody } from "../ui/Card";
import type { LessonSummary } from "../../types";

interface LessonCardProps {
  lesson: LessonSummary;
  courseId: string;
  videoStatus?: string | null;
  onDelete: () => void;
}

const LessonCard: React.FC<LessonCardProps> = ({ lesson, courseId, onDelete }) => {
  const navigate = useNavigate();

  const handleDelete = (e: React.MouseEvent) => {
    e.stopPropagation();
    onDelete();
  };

  return (
    <Card
      hover
      onClick={() => navigate(`/courses/${courseId}/lessons/${lesson.id}`)}
    >
      <CardBody className="p-4">
        <div className="flex items-center gap-4">
          <div className="flex-shrink-0">
            <div className="w-10 h-10 rounded-full bg-primary-100 text-primary-700 flex items-center justify-center text-sm font-medium">
              {lesson.order.toString().padStart(2, '0')}
            </div>
          </div>

          <div className="flex-1 min-w-0">
            <h3 className="text-sm font-medium text-gray-900 truncate">
              {lesson.title}
            </h3>

          </div>

          <div className="flex-shrink-0">
            <button
              onClick={handleDelete}
              className="text-gray-400 hover:text-red-500 transition-colors p-1"
            >
              <Trash2 size={16} />
            </button>
          </div>
        </div>
      </CardBody>
    </Card>
  );
};

export default LessonCard;
