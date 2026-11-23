import type { Meta, StoryObj } from "@storybook/react";
import { PasswordField } from "@almomiaa/ui";

const meta = {
  title: "ERP/Forms/PasswordField",
  component: PasswordField,
  tags: ["autodocs"],
  args: {
    label: "كلمة المرور",
    placeholder: "أدخل كلمة المرور",
    showPasswordLabel: "إظهار كلمة المرور",
    hidePasswordLabel: "إخفاء كلمة المرور",
  },
} satisfies Meta<typeof PasswordField>;

export default meta;
type Story = StoryObj<typeof meta>;

export const ArabicRTL: Story = {
  args: {
    label: "كلمة المرور",
    placeholder: "أدخل كلمة المرور",
    showPasswordLabel: "إظهار كلمة المرور",
    hidePasswordLabel: "إخفاء كلمة المرور",
    hint: "أدخل كلمة المرور الخاصة بحساب الموظف",
  },
  parameters: { direction: "rtl" },
};

export const EnglishLTR: Story = {
  args: {
    label: "Password",
    placeholder: "Enter your password",
    showPasswordLabel: "Show password",
    hidePasswordLabel: "Hide password",
    hint: "Enter your employee account password",
  },
  parameters: { direction: "ltr" },
};

export const RevealBehavior: Story = {
  args: {
    label: "Password",
    defaultValue: "SampleValueOnly",
    showPasswordLabel: "Show password",
    hidePasswordLabel: "Hide password",
  },
  parameters: { direction: "ltr" },
};

export const Focused: Story = {
  args: {
    label: "Password",
    autoFocus: true,
    showPasswordLabel: "Show password",
    hidePasswordLabel: "Hide password",
  },
  parameters: { direction: "ltr" },
};

export const Invalid: Story = {
  args: {
    label: "كلمة المرور",
    defaultValue: "",
    error: "يرجى إدخال كلمة المرور",
    showPasswordLabel: "إظهار كلمة المرور",
    hidePasswordLabel: "إخفاء كلمة المرور",
  },
  parameters: { direction: "rtl" },
};

export const Disabled: Story = {
  args: {
    label: "Password",
    defaultValue: "Unavailable",
    disabled: true,
    showPasswordLabel: "Show password",
    hidePasswordLabel: "Hide password",
  },
  parameters: { direction: "ltr" },
};

export const DarkTheme: Story = {
  args: {
    label: "Password",
    placeholder: "Enter your password",
    showPasswordLabel: "Show password",
    hidePasswordLabel: "Hide password",
  },
  globals: { theme: "dark" },
  parameters: { direction: "ltr" },
};
