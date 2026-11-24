import type { Meta, StoryObj } from "@storybook/react";
import { Badge, KeyValueList } from "@almomiaa/ui";

const meta = {
  title: "ERP/Data/KeyValueList",
  component: KeyValueList,
  tags: ["autodocs"],
  args: {
    columns: 2,
    items: [
      { id: "code", label: "رمز العميل", value: "CUST-RYD-001", mono: true, dir: "ltr" },
      { id: "grade", label: "التصنيف الائتماني", value: <Badge tone="gold">Grade B (60d)</Badge> },
      { id: "limit", label: "الحد الائتماني", value: "50,000.00 ر.س", mono: true },
      { id: "balance", label: "الرصيد الحالي", value: "12,450.00 ر.س", mono: true },
    ],
  },
} satisfies Meta<typeof KeyValueList>;

export default meta;
type Story = StoryObj<typeof meta>;

export const ArabicRTL: Story = {
  parameters: { direction: "rtl" },
};

export const ThreeColumnDarkLTR: Story = {
  args: { columns: 3 },
  globals: { theme: "dark" },
  parameters: { direction: "ltr" },
};
