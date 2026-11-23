import type { Meta, StoryObj } from "@storybook/react";
import { SidebarNavItem } from "@almomiaa/ui";

const meta = {
  title: "ERP/Navigation/SidebarNavItem",
  component: SidebarNavItem,
  tags: ["autodocs"],
  decorators: [
    (Story) => (
      <div className="w-64 rounded-lg bg-surface-obsidian p-2">
        <Story />
      </div>
    ),
  ],
  args: {
    label: "لوحة التحكم",
    icon: <span aria-hidden="true">D</span>,
    href: "/dashboard",
  },
} satisfies Meta<typeof SidebarNavItem>;

export default meta;
type Story = StoryObj<typeof meta>;

export const ArabicRTL: Story = {
  args: { label: "لوحة التحكم", active: true },
  parameters: { direction: "rtl" },
};

export const EnglishLTR: Story = {
  args: { label: "Dashboard", active: true },
  parameters: { direction: "ltr" },
};

export const Inactive: Story = {
  args: { label: "Inventory", active: false, href: undefined },
  parameters: { direction: "ltr" },
};

export const Placeholder: Story = {
  args: { label: "التقارير", placeholder: true, placeholderLabel: "قريبًا", href: undefined },
  parameters: { direction: "rtl" },
};

export const Collapsed: Story = {
  args: { label: "لوحة التحكم", active: true, collapsed: true },
  decorators: [
    (Story) => (
      <div className="w-20 rounded-lg bg-surface-obsidian p-2">
        <Story />
      </div>
    ),
  ],
  parameters: { direction: "rtl" },
};
