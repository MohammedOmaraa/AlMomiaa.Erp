import type { Meta, StoryObj } from "@storybook/react";
import { SummaryCard } from "@almomiaa/ui";

const meta = {
  title: "ERP/Surfaces/SummaryCard",
  component: SummaryCard,
  tags: ["autodocs"],
  args: {
    label: "العناصر التوضيحية",
    value: "—",
    supportingText: "قيم تجريبية غير مرتبطة ببيانات حقيقية",
  },
} satisfies Meta<typeof SummaryCard>;

export default meta;
type Story = StoryObj<typeof meta>;

export const ArabicRTL: Story = {
  args: {
    label: "العناصر التوضيحية",
    value: "—",
    supportingText: "قيم تجريبية غير مرتبطة ببيانات حقيقية",
  },
  parameters: { direction: "rtl" },
};

export const EnglishLTR: Story = {
  args: {
    label: "Illustrative items",
    value: "—",
    supportingText: "No live business data",
  },
  parameters: { direction: "ltr" },
};

export const Loading: Story = {
  args: { label: "Illustrative items", state: "loading", loadingLabel: "Loading summary" },
  parameters: { direction: "ltr" },
};

export const ArabicLoading: Story = {
  args: { label: "عناصر توضيحية", state: "loading", loadingLabel: "جارٍ تحميل الملخص" },
  parameters: { direction: "rtl" },
};

export const Empty: Story = {
  args: { label: "القيمة", state: "empty", emptyMessage: "لا توجد قيمة متاحة" },
  parameters: { direction: "rtl" },
};

export const Error: Story = {
  args: { label: "Illustrative items", state: "error", errorMessage: "Summary unavailable" },
  parameters: { direction: "ltr" },
};

export const DarkTheme: Story = {
  args: {
    label: "Illustrative items",
    value: "—",
    supportingText: "No live business data",
  },
  globals: { theme: "dark" },
  parameters: { direction: "ltr" },
};
