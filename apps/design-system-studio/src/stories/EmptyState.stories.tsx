import type { Meta, StoryObj } from "@storybook/react";
import { Button, EmptyState } from "@almomiaa/ui";

const meta = {
  title: "ERP/Data/EmptyState",
  component: EmptyState,
  tags: ["autodocs"],
  args: {
    title: "لا توجد سجلات مطابقة",
    description: "جرّب تعديل معايير البحث أو إضافة سجل جديد.",
  },
} satisfies Meta<typeof EmptyState>;

export default meta;
type Story = StoryObj<typeof meta>;

export const ArabicWithAction: Story = {
  args: {
    action: <Button>إضافة منتج جديد</Button>,
  },
  parameters: { direction: "rtl" },
};

export const EnglishDark: Story = {
  args: {
    title: "No invoices yet",
    description: "Confirm shipment on an approved sales order to issue a ZATCA Phase 2 invoice.",
    action: <Button variant="secondary">Refresh</Button>,
  },
  globals: { theme: "dark" },
  parameters: { direction: "ltr" },
};
