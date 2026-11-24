import { useState } from "react";
import type { Meta, StoryObj } from "@storybook/react";
import { NotificationItem } from "@almomiaa/ui";

const meta: Meta<typeof NotificationItem> = {
  title: "Feedback/NotificationItem",
  component: NotificationItem,
  tags: ["autodocs"],
};

export default meta;
type Story = StoryObj<typeof NotificationItem>;

function InteractiveNotificationsDemo() {
  const [readIds, setReadIds] = useState<string[]>(["n3"]);
  const items = [
    {
      id: "n1",
      severity: "warning" as const,
      title: "طلب بيع جديد بانتظار الاعتماد: SO-20261124-A1B2C3D4",
      message: "المندوب: أحمد العتيبي • الإجمالي: 14,250.00 ر.س",
      timestamp: "2026-11-24 11:40",
    },
    {
      id: "n2",
      severity: "success" as const,
      title: "تم اعتماد طلب البيع: SO-20261124-99887766",
      message: "الإجمالي: 8,400.00 ر.س — جاهز للتجهيز والشحن",
      timestamp: "2026-11-24 11:15",
    },
    {
      id: "n3",
      severity: "danger" as const,
      title: "تم رفض طلب البيع: SO-20261124-11223344",
      message: "السبب: تجاوز الحد الائتماني بدون موافقة مالية",
      timestamp: "2026-11-24 10:02",
    },
  ];

  return (
    <div className="max-w-lg space-y-2.5">
      {items.map((item) => (
        <NotificationItem
          key={item.id}
          title={item.title}
          message={item.message}
          severity={item.severity}
          timestamp={item.timestamp}
          isRead={readIds.includes(item.id)}
          markReadLabel="تحديد كمقروء"
          unreadLabel="جديد"
          onMarkRead={() => setReadIds((prev) => [...prev, item.id])}
        />
      ))}
    </div>
  );
}

export const Interactive: Story = {
  render: () => <InteractiveNotificationsDemo />,
};

export const ReadEnglishLtr: Story = {
  args: {
    title: "Sales Order Approved: SO-20261124-55667788",
    message: "Total: SAR 5,175.00 — Ready for warehouse shipment",
    severity: "success",
    timestamp: "2026-11-24 09:30",
    isRead: true,
  },
};
