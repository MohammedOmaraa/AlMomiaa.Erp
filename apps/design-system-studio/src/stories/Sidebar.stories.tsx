import type { Meta, StoryObj } from "@storybook/react";
import { Sidebar } from "@almomiaa/ui";

const navArabic = [
  { id: "dashboard", label: "لوحة التحكم", href: "/dashboard", active: true, icon: "D" },
  { id: "sales", label: "المبيعات", placeholder: true, placeholderLabel: "قريبًا", icon: "S" },
  { id: "customers", label: "العملاء", placeholder: true, placeholderLabel: "قريبًا", icon: "C" },
];

const navEnglish = [
  { id: "dashboard", label: "Dashboard", href: "/dashboard", active: true, icon: "D" },
  { id: "sales", label: "Sales", placeholder: true, placeholderLabel: "Soon", icon: "S" },
  { id: "customers", label: "Customers", placeholder: true, placeholderLabel: "Soon", icon: "C" },
];

const meta = {
  title: "ERP/Navigation/Sidebar",
  component: Sidebar,
  tags: ["autodocs"],
  render: (args) => (
    <div className={args.compact ? "h-[32rem] w-20" : "h-[32rem] w-72"}>
      <Sidebar {...args} />
    </div>
  ),
  args: { brandLabel: "AlMomiaa", items: navEnglish },
} satisfies Meta<typeof Sidebar>;

export default meta;
type Story = StoryObj<typeof meta>;

export const ArabicRTL: Story = {
  args: { brandLabel: "المومياء", items: navArabic },
  parameters: { direction: "rtl" },
};

export const EnglishLTR: Story = {
  args: { brandLabel: "AlMomiaa", items: navEnglish },
  parameters: { direction: "ltr" },
};

export const CompactRail: Story = {
  args: { brandLabel: "AlMomiaa", items: navEnglish, compact: true },
  parameters: { direction: "ltr", viewport: { defaultViewport: "tablet" } },
};

export const MobileDrawer: Story = {
  args: { brandLabel: "المومياء", items: navArabic },
  render: (args) => (
    <div className="relative h-[32rem] w-full overflow-hidden rounded-xl border border-surface-border bg-surface-canvas">
      <div className="absolute inset-y-0 start-0 z-10 w-72 shadow-lg">
        <Sidebar {...args} />
      </div>
      <div className="absolute inset-0 bg-surface-obsidian/60" />
    </div>
  ),
  parameters: { direction: "rtl", viewport: { defaultViewport: "mobile" } },
};
