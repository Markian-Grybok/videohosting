import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { contentApi } from "../api/contentApi";
import type { CreateCourseRequest } from "../types";

export function useCourses() {
    return useQuery({
        queryKey: ["courses"],
        queryFn: () => contentApi.getCourses(),
    });
}

export function useCourse(id: string) {
    return useQuery({
        queryKey: ["courses", id],
        queryFn: () => contentApi.getCourse(id),
        enabled: !!id,
    });
}

export function useCreateCourse() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: contentApi.createCourse,
        onSuccess: () => queryClient.invalidateQueries({ queryKey: ["courses"] }),
    });
}

export function useUpdateCourse() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: ({ id, data }: { id: string; data: CreateCourseRequest }) =>
            contentApi.updateCourse(id, data),
        onSuccess: (_, { id }) => {
            queryClient.invalidateQueries({ queryKey: ["courses", id] });
            queryClient.invalidateQueries({ queryKey: ["courses"] });
        },
    });
}

export function useDeleteCourse() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: contentApi.deleteCourse,
        onSuccess: () => queryClient.invalidateQueries({ queryKey: ["courses"] }),
    });
}