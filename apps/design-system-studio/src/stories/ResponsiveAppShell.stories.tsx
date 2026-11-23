import type { Meta, StoryObj } from "@storybook/react";
import { ResponsiveAppShell } from "@almomiaa/ui";

const itemsEnglish = [
  { id: "dashboard", label: "Dashboard", href: "/dashboard", active: true, icon: "D" },
  { id: "sales", label: "Sales", placeholder: true, placeholderLabel: "Soon", icon: "S" },
  { id: "customers", label: "Customers", placeholder: true, placeholderLabel: "Soon", icon: "C" },
  { id: "inventory", label: "Inventory", placeholder: true, placeholderLabel: "Soon", icon: "I" },
];

const itemsArabic = [
  { id: "dashboard", label: "لوحة التحكم", href: "/dashboard", active: true, icon: "D" },
  { id: "sales", label: "المبيعات", placeholder: true, placeholderLabel: "قريبًا", icon: "S" },
  { id: "customers", label: "العملاء", placeholder: true, placeholderLabel: "قريبًا", icon: "C" },
  { id: "inventory", label: "المخزون", placeholder: true, placeholderLabel: "قريبًا", icon: "I" },
];

const commonEnglish = {
  brandLabel: "AlMomiaa",
  items: itemsEnglish,
  title: "Dashboard",
  locale: "en-US" as const,
  theme: "light" as const,
  onLocaleChange: () => undefined,
  onThemeChange: () => undefined,
};

const commonArabic = {
  brandLabel: "المومياء",
  items: itemsArabic,
  title: "لوحة التحكم",
  locale: "ar-SA" as const,
  theme: "light" as const,
  onLocaleChange: () => undefined,
  onThemeChange: () => undefined,
};

const sampleEnglishContent = (
  <div className="space-y-4">
    <h2 className="text-xl font-semibold">Welcome to AlMomiaa</h2>
    <p className="text-text-secondary">Illustrative workspace preview</p>
    <div className="grid gap-3 sm:grid-cols-2">
      <div className="rounded-xl border border-surface-border bg-surface-card p-5">Sample summary</div>
      <div className="rounded-xl border border-surface-border bg-surface-card p-5">No live business data</div>
    </div>
  </div>
);

const sampleArabicContent = (
  <div className="space-y-4">
    <h2 className="text-xl font-semibold">مرحبًا بك في المومياء</h2>
    <p className="text-text-secondary">معاينة توضيحية لمساحة العمل</p>
    <div className="grid gap-3 sm:grid-cols-2">
      <div className="rounded-xl border border-surface-border bg-surface-card p-5">ملخص توضيحي</div>
      <div className="rounded-xl border border-surface-border bg-surface-card p-5">لا توجد بيانات أعمال حقيقية</div>
    </div>
  </div>
);

const meta = {
  title: "ERP/Layout/ResponsiveAppShell",
  component: ResponsiveAppShell,
  tags: ["autodocs"],
  parameters: { layout: "fullscreen" },
  args: { ...commonEnglish, children: sampleEnglishContent },
} satisfies Meta<typeof ResponsiveAppShell>;

export default meta;
type Story = StoryObj<typeof meta>;

export const ArabicRTL: Story = {
  args: { ...commonArabic, children: sampleArabicContent },
  parameters: { direction: "rtl", viewport: { defaultViewport: "desktop" } },
};

export const EnglishLTR: Story = {
  args: { ...commonEnglish, children: sampleEnglishContent },
  parameters: { direction: "ltr", viewport: { defaultViewport: "desktop" } },
};

export const ExpandedDesktop: Story = {
  args: { ...commonEnglish, children: sampleEnglishContent },
  parameters: { direction: "ltr", viewport: { defaultViewport: "desktop" } },
};

export const CompactMediumScreen: Story = {
  args: { ...commonEnglish, initialCompact: true, children: sampleEnglishContent },
  parameters: { direction: "ltr", viewport: { defaultViewport: "tablet" } },
};

export const ExpandedMediumScreen: Story = {
  args: { ...commonEnglish, initialCompact: false, children: sampleEnglishContent },
  parameters: { direction: "ltr", viewport: { defaultViewport: "tablet" } },
};

export const MobileDrawerClosed: Story = {
  args: { ...commonEnglish, initialDrawerOpen: false, children: sampleEnglishContent },
  parameters: { direction: "ltr", viewport: { defaultViewport: "mobile" } },
};

export const MobileDrawerOpen: Story = {
  args: { ...commonArabic, theme: "dark", initialDrawerOpen: true, children: sampleArabicContent },
  globals: { theme: "dark" },
  parameters: { direction: "rtl", viewport: { defaultViewport: "mobile" } },
};

export const Loading: Story = {
  args: { ...commonEnglish, contentState: "loading" },
  parameters: { direction: "ltr" },
};

export const Empty: Story = {
  args: { ...commonArabic, contentState: "empty", stateMessage: "لا يوجد محتوى لعرضه" },
  parameters: { direction: "rtl" },
};

export const Error: Story = {
  args: {
    ...commonArabic,
    contentState: "error",
    stateMessage: "تعذر تحميل مساحة العمل",
    onRetry: () => undefined,
  },
  parameters: { direction: "rtl" },
};
