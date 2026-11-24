import type { AxiosInstance } from "axios";

export type NotificationSeverity = "info" | "success" | "warning" | "danger";

export interface UserNotificationDto {
  id: string;
  eventType: string;
  severity: NotificationSeverity;
  titleAr: string;
  titleEn: string;
  messageAr: string;
  messageEn: string;
  entityName: string | null;
  entityId: string | null;
  isRead: boolean;
  readAtUtc: string | null;
  createdAtUtc: string;
}

export interface NotificationsPageDto {
  items: UserNotificationDto[];
  totalCount: number;
  unreadCount: number;
  nextCursor: string | null;
}

export interface MarkNotificationReadResponse {
  notification: UserNotificationDto;
  unreadCount: number;
}

export interface MarkAllNotificationsReadResponse {
  markedCount: number;
  unreadCount: number;
}

export function createNotificationsService(http: AxiosInstance) {
  return {
    getNotifications: async (params?: {
      unreadOnly?: boolean;
      page?: number;
      pageSize?: number;
    }) => (await http.get<NotificationsPageDto>("/notifications", { params })).data,

    markRead: async (notificationId: string) =>
      (await http.patch<MarkNotificationReadResponse>(`/notifications/${notificationId}/read`)).data,

    markAllRead: async () =>
      (await http.post<MarkAllNotificationsReadResponse>("/notifications/mark-all-read")).data,
  };
}
