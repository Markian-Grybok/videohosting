import React from "react";

interface BadgeProps {
  status: "Pending" | "Processing" | "Ready" | "Failed" | string;
}

const Badge: React.FC<BadgeProps> = ({ status }) => {
  const statusConfig = {
    Pending: {
      classes: "bg-gray-100 text-gray-600",
      animate: false
    },
    Processing: {
      classes: "bg-blue-100 text-blue-700",
      animate: true
    },
    Ready: {
      classes: "bg-green-100 text-green-700",
      animate: false
    },
    Failed: {
      classes: "bg-red-100 text-red-700",
      animate: false
    },
    "Без відео": {
      classes: "bg-gray-100 text-gray-600",
      animate: false
    },
    "Завантажено": {
      classes: "bg-blue-100 text-blue-700",
      animate: false
    },
    "Обробляється...": {
      classes: "bg-blue-100 text-blue-700",
      animate: true
    },
    "Готово": {
      classes: "bg-green-100 text-green-700",
      animate: false
    },
    "Помилка": {
      classes: "bg-red-100 text-red-700",
      animate: false
    }
  };

  const config = statusConfig[status as keyof typeof statusConfig] || statusConfig.Pending;
  const animateClass = config.animate ? "animate-pulse" : "";

  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${config.classes} ${animateClass}`}>
      {status}
    </span>
  );
};

export default Badge;
