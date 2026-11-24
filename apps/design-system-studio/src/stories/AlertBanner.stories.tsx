import type { Meta, StoryObj } from "@storybook/react";
import { AlertBanner } from "@almomiaa/ui";

const meta = {
  title: "ERP/Feedback/AlertBanner",
  component: AlertBanner,
  tags: ["autodocs"],
  args: {
    tone: "success",
    title: "تم حفظ الطلب بنجاح",
    message: "تم إنشاء طلب المبيعات وحجز الكمية لمدة 24 ساعة.",
  },
} satisfies Meta<typeof AlertBanner>;

export default meta;
type Story = StoryObj<typeof meta>;

export const AllTonesRTL: Story = {
  render: () => (
    <div className="space-y-3">
      <AlertBanner tone="info" title="تنبيه" message="تم تحديث بيانات الفواتير من الخادم." />
      <AlertBanner tone="success" title="نجاح" message="تم إصدار فاتورة ZATCA بنجاح." />
      <AlertBanner tone="warning" title="تحذير ائتمان (MR-03)" message="العميل تجاوز الحد الائتماني ويتطلب موافقة مالية." />
      <AlertBanner
        tone="danger"
        title="فشل التحقق من الحقول (400)"
        message="يرجى مراجعة الأخطاء التالية:"
        details={["الرقم الضريبي يجب أن يتكون من 15 رقمًا.", "سعر البيع أقل من الحد الأدنى المسموح."]}
        onDismiss={() => undefined}
      />
    </div>
  ),
  parameters: { direction: "rtl" },
};

export const DarkLTR: Story = {
  args: {
    tone: "danger",
    title: "Business Rule Violation (422)",
    message: "Fiscal period 2026-11 is closed and locked against postings.",
  },
  globals: { theme: "dark" },
  parameters: { direction: "ltr" },
};
