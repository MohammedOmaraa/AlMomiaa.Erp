import { useState } from "react";
import type { Meta, StoryObj } from "@storybook/react";
import { StepProgress, Button } from "@almomiaa/ui";

const meta: Meta<typeof StepProgress> = {
  title: "Navigation/StepProgress",
  component: StepProgress,
  tags: ["autodocs"]
};

export default meta;
type Story = StoryObj<typeof StepProgress>;

const arabicSteps = [
  { id: "customer", label: "العميل والائتمان", description: "اختيار العميل وفحص الرصيد" },
  { id: "products", label: "المنتجات والكميات", description: "التصنيف والسعر والمخزون" },
  { id: "review", label: "المراجعة والإرسال", description: "إرسال لمدير المبيعات" }
];

function InteractiveWizardDemo() {
  const [step, setStep] = useState(1);
  return (
    <div className="space-y-4 rounded-2xl border border-surface-border bg-surface-card p-4">
      <StepProgress steps={arabicSteps} activeStepIndex={step} onStepSelect={setStep} />
      <div className="flex items-center justify-between pt-2">
        <Button variant="secondary" disabled={step === 0} onClick={() => setStep((s) => Math.max(0, s - 1))}>
          السابق
        </Button>
        <Button disabled={step === arabicSteps.length - 1} onClick={() => setStep((s) => Math.min(arabicSteps.length - 1, s + 1))}>
          التالي
        </Button>
      </div>
    </div>
  );
}

export const Interactive: Story = {
  render: () => <InteractiveWizardDemo />
};

export const EnglishLtr: Story = {
  args: {
    activeStepIndex: 1,
    steps: [
      { id: "customer", label: "Customer & Credit", description: "Select customer & warehouse" },
      { id: "catalog", label: "Products & Stock", description: "Tier price & quantities" },
      { id: "submit", label: "Review & Submit", description: "Send to Sales Manager" }
    ]
  }
};
