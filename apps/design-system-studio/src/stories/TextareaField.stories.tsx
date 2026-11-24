import type { Meta, StoryObj } from "@storybook/react";
import { TextareaField } from "@almomiaa/ui";

const meta = {
  title: "ERP/Forms/TextareaField",
  component: TextareaField,
  tags: ["autodocs"],
  args: {
    label: "سبب الرفض أو المرتجع",
    placeholder: "اكتب تفاصيل السبب هنا...",
    hint: "الحد الأقصى 500 حرف",
  },
} satisfies Meta<typeof TextareaField>;

export default meta;
type Story = StoryObj<typeof meta>;

export const ArabicRTL: Story = {
  parameters: { direction: "rtl" },
};

export const InvalidDarkLTR: Story = {
  args: {
    label: "Rejection Reason",
    required: true,
    error: "RejectionReason is required.",
  },
  globals: { theme: "dark" },
  parameters: { direction: "ltr" },
};
