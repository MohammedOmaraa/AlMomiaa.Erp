import type { Meta, StoryObj } from "@storybook/react";
import { TextField } from "@almomiaa/ui";

const meta = {
  title: "ERP/Forms/TextField",
  component: TextField,
  tags: ["autodocs"],
  args: {
    label: "رقم الجوال أو الرقم الوظيفي",
    placeholder: "مثال: 05XXXXXXXX أو EMP-0001",
    required: true,
  },
} satisfies Meta<typeof TextField>;

export default meta;
type Story = StoryObj<typeof meta>;

export const ArabicRTL: Story = {
  args: {
    label: "رقم الجوال أو الرقم الوظيفي",
    placeholder: "مثال: 05XXXXXXXX أو EMP-0001",
    hint: "استخدم رقم الجوال المسجل أو الرقم الوظيفي للموظف",
  },
  parameters: { direction: "rtl" },
};

export const EnglishLTR: Story = {
  args: {
    label: "Phone number or employee code",
    placeholder: "e.g. 05XXXXXXXX or EMP-0001",
    hint: "Use your registered phone number or employee code",
  },
  parameters: { direction: "ltr" },
};

export const Focused: Story = {
  args: {
    label: "Employee code",
    placeholder: "EMP-0001",
    autoFocus: true,
  },
  parameters: { direction: "ltr" },
};

export const Invalid: Story = {
  args: {
    label: "رقم الجوال أو الرقم الوظيفي",
    defaultValue: "",
    error: "يرجى إدخال رقم الجوال أو الرقم الوظيفي",
  },
  parameters: { direction: "rtl" },
};

export const Disabled: Story = {
  args: {
    label: "Employee code",
    defaultValue: "EMP-0001",
    disabled: true,
  },
  parameters: { direction: "ltr" },
};

export const DarkTheme: Story = {
  args: {
    label: "Phone number or employee code",
    placeholder: "EMP-0001",
    hint: "Dark surface preview",
  },
  globals: { theme: "dark" },
  parameters: { direction: "ltr" },
};
