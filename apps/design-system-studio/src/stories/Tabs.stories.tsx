import { useState } from "react";
import type { Meta, StoryObj } from "@storybook/react";
import { Tabs, type TabsProps } from "@almomiaa/ui";

const meta = {
  title: "ERP/Navigation/Tabs",
  component: Tabs,
  tags: ["autodocs"],
  args: {
    activeId: "orders",
    onChange: () => undefined,
    items: [
      { id: "orders", label: "طلبات المبيعات", badge: 12 },
      { id: "invoices", label: "فواتير ZATCA", badge: 8 },
      { id: "returns", label: "المرتجعات" },
      { id: "delivery", label: "التوصيل و POD" },
    ],
  },
} satisfies Meta<typeof Tabs>;

export default meta;
type Story = StoryObj<typeof meta>;

function InteractiveTabsDemo(args: TabsProps) {
  const [active, setActive] = useState(args.activeId);
  return <Tabs {...args} activeId={active} onChange={setActive} />;
}

export const InteractiveRTL: Story = {
  render: (args) => <InteractiveTabsDemo {...args} />,
  parameters: { direction: "rtl" },
};

export const DarkLTR: Story = {
  args: {
    items: [
      { id: "orders", label: "Sales Orders", badge: 12 },
      { id: "invoices", label: "ZATCA Invoices", badge: 8 },
      { id: "returns", label: "Returns" },
    ],
  },
  globals: { theme: "dark" },
  parameters: { direction: "ltr" },
};
