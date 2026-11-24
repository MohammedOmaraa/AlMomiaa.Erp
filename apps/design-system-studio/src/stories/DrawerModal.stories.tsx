import { useState } from "react";
import type { Meta, StoryObj } from "@storybook/react";
import { Button, DrawerModal, TextField } from "@almomiaa/ui";

const meta = {
  title: "ERP/Overlays/DrawerModal",
  component: DrawerModal,
  tags: ["autodocs"],
  args: {
    open: true,
    onClose: () => undefined,
    title: "إضافة عميل جديد",
    subtitle: "تعيين التصنيف الائتماني والحد المالي والمندوب المسؤول",
    children: null,
  },
} satisfies Meta<typeof DrawerModal>;

export default meta;
type Story = StoryObj<typeof meta>;

function InteractiveDrawerRTL() {
  const [open, setOpen] = useState(true);
  return (
    <div>
      <Button onClick={() => setOpen(true)}>فتح اللوحة الجانبية</Button>
      <DrawerModal
        open={open}
        onClose={() => setOpen(false)}
        title="إضافة عميل جديد"
        subtitle="يرجى تعبئة بيانات العميل بدقة"
        footer={
          <>
            <Button variant="secondary" onClick={() => setOpen(false)}>
              إلغاء
            </Button>
            <Button onClick={() => setOpen(false)}>حفظ العميل</Button>
          </>
        }
      >
        <div className="space-y-4">
          <TextField label="رمز العميل" placeholder="CUST-RYD-001" required />
          <TextField label="الاسم بالعربية" placeholder="أسواق النخبة" required />
        </div>
      </DrawerModal>
    </div>
  );
}

function InteractiveDrawerLTR() {
  const [open, setOpen] = useState(true);
  return (
    <div>
      <Button onClick={() => setOpen(true)}>Open Drawer</Button>
      <DrawerModal
        open={open}
        onClose={() => setOpen(false)}
        title="Create Warehouse"
        subtitle="Main, Branch, or CashVan Mobile Warehouse"
        footer={<Button onClick={() => setOpen(false)}>Save</Button>}
      >
        <TextField label="Warehouse Code" placeholder="WH-RYD-01" required />
      </DrawerModal>
    </div>
  );
}

export const InteractiveRTL: Story = {
  render: () => <InteractiveDrawerRTL />,
  parameters: { direction: "rtl" },
};

export const DarkLTR: Story = {
  render: () => <InteractiveDrawerLTR />,
  globals: { theme: "dark" },
  parameters: { direction: "ltr" },
};
