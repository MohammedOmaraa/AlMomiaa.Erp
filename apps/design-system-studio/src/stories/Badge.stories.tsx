import type { Meta, StoryObj } from "@storybook/react";
import { Badge } from "@almomiaa/ui";

const meta = {
  title: "ERP/Data/Badge",
  component: Badge,
  tags: ["autodocs"],
  args: {
    children: "معتمد",
    tone: "success",
  },
} satisfies Meta<typeof Badge>;

export default meta;
type Story = StoryObj<typeof meta>;

export const AllTonesRTL: Story = {
  render: () => (
    <div className="flex flex-wrap gap-2">
      <Badge tone="neutral">مسودة</Badge>
      <Badge tone="gold">الدرجة A</Badge>
      <Badge tone="success">معتمد</Badge>
      <Badge tone="warning">بانتظار مراجعة السعر</Badge>
      <Badge tone="danger">تجاوز حد الائتمان</Badge>
      <Badge tone="info" mono>ZATCA ICV #14</Badge>
    </div>
  ),
  parameters: { direction: "rtl" },
};

export const EnglishDark: Story = {
  render: () => (
    <div className="flex flex-wrap gap-2">
      <Badge tone="gold">SUPER_ADMIN</Badge>
      <Badge tone="success">ShippedAndInvoiced</Badge>
      <Badge tone="warning">PendingApproval</Badge>
      <Badge tone="danger">Rejected</Badge>
    </div>
  ),
  globals: { theme: "dark" },
  parameters: { direction: "ltr" },
};
