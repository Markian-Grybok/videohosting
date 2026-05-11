import React from "react";
import { NavLink } from "react-router-dom";

const Navbar: React.FC = () => {
  return (
    <nav className="bg-white border-b border-gray-200 px-6 h-16 flex items-center justify-between sticky top-0 z-50 shadow-sm">
      <div className="text-xl font-bold text-primary-600">
        🎬 VideoHosting
      </div>
      <div className="flex space-x-6">
        <NavLink
          to="/courses"
          className={({ isActive }) =>
            `px-3 py-2 rounded-md text-sm font-medium transition-colors ${
              isActive
                ? "text-primary-600 border-b-2 border-primary-600"
                : "text-gray-600 hover:text-primary-600"
            }`
          }
        >
          Курси
        </NavLink>
      </div>
    </nav>
  );
};

export default Navbar;
