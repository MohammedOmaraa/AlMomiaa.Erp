import type { Meta, StoryObj } from "@storybook/react";
import { SurfaceCard } from "@almomiaa/ui";

const meta = {
  title: "ERP/Surfaces/SurfaceCard",
  component: SurfaceCard,
  tags: ["autodocs"],
  args: {
    title: "ملخص لوحة التحكم",
    children: <p className="text-text-secondary">محتوى توضيحي فقط.</p>,
  },
} satisfies Meta<typeof SurfaceCard>;

export default meta;
type Story = StoryObj<typeof meta>;

export const ArabicRTL: Story = {
  args: {
    title: "ملخص لوحة التحكم",
    children: <p className="text-text-secondary">محتوى توضيحي فقط.</p>,
  },
  parameters: { direction: "rtl" },
};

export const EnglishLTR: Story = {
  args: {
    title: "Workspace summary",
    children: <p className="text-text-secondary">Illustrative content only.</p>,
  },
  parameters: { direction: "ltr" },
};

export const Loading: Story = {
  args: { title: "Summary", state: "loading", loadingLabel: "Loading summary" },
  parameters: { direction: "ltr" },
};

export const ArabicLoading: Story = {
  args: { title: "ملخص", state: "loading", loadingLabel: "جارٍ تحميل الملخص" },
  parameters: { direction: "rtl" },
};

export const Empty: Story = {
  args: {
    title: "لا توجد بيانات",
    state: "empty",
    emptyMessage: "لا يوجد محتوى لعرضه حاليًا",
  },
  parameters: { direction: "rtl" },
};

export const Error: Story = {
  args: {
    title: "Summary",
    state: "error",
    errorMessage: "Unable to show this section",
    onRetry: () => undefined,
  },
  parameters: { direction: "ltr" },
};

export const ArabicError: Story = {
  args: {
    title: "ملخص",
    state: "error",
    errorMessage: "تعذر عرض هذا القسم",
    retryLabel: "إعادة المحاولة",
    onRetry: () => undefined,
  },
  parameters: { direction: "rtl" },
};

export const DarkTheme: Story = {
  args: {
    title: "Workspace summary",
    description: "Illustrative preview",
    children: <p className="text-text-secondary">Illustrative content only.</p>,
  },
  globals: { theme: "dark" },
  parameters: { direction: "ltr" },
};
