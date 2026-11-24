import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { z } from "zod";
import {
  createIdempotencyKey,
  type CategoryNodeDto,
  type CategoryProductPriceDto,
  type CustomerCreditStatusDto,
  type CustomerDto,
  type MasterProductDto,
  type SalesOrderDto,
  type WarehouseDto,
} from "@almomiaa/api-client";
import {
  AlertBanner,
  Badge,
  Button,
  EmptyState,
  KeyValueList,
  SelectField,
  StepProgress,
  SurfaceCard,
  TextField,
} from "@almomiaa/ui";
import { erpApi, formatApiError } from "../lib/api-client";
import { formatDateTime, formatNumber, formatSar } from "../lib/formatters";
import { useApp } from "../providers/AppProviders";
import { RepMobileShell } from "../components/RepMobileShell";

interface CartLine {
  masterProductId: string;
  productUnitId: string;
  sku: string;
  productNameAr: string;
  productNameEn: string;
  unitNameAr: string;
  unitNameEn: string;
  conversionFactor: number;
  standardTierPrice: number;
  minAllowedPrice: number;
  unitPrice: number;
  orderedQty: number;
  availableQty: number;
  vatRate: number;
}

const submitOrderSchema = z.object({
  customerId: z.string().min(1),
  warehouseId: z.number().int().positive(),
  orderChannel: z.enum(["PreSales", "CashVan"]),
  items: z
    .array(
      z.object({
        masterProductId: z.string().min(1),
        productUnitId: z.string().min(1),
        orderedQty: z.number().positive(),
        unitPrice: z.number().positive(),
      }),
    )
    .min(1),
});

function flattenCategories(nodes: CategoryNodeDto[]): CategoryNodeDto[] {
  const flat: CategoryNodeDto[] = [];
  for (const node of nodes) {
    flat.push(node);
    if (node.children?.length) {
      flat.push(...flattenCategories(node.children));
    }
  }
  return flat;
}

export function NewSellRequestPage() {
  const { locale, isAr, t, user, isOnline, hasPermission } = useApp();
  const navigate = useNavigate();

  const [step, setStep] = useState(0);
  const [loadingInit, setLoadingInit] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [customers, setCustomers] = useState<CustomerDto[]>([]);
  const [warehouses, setWarehouses] = useState<WarehouseDto[]>([]);
  const [categories, setCategories] = useState<CategoryNodeDto[]>([]);
  const [customerSearch, setCustomerSearch] = useState("");
  const [selectedCustomerId, setSelectedCustomerId] = useState("");
  const [selectedWarehouseId, setSelectedWarehouseId] = useState<number>(
    user?.defaultWarehouseId ?? 0,
  );
  const [orderChannel, setOrderChannel] = useState<"PreSales" | "CashVan">(
    user?.repType === "CashVan" ? "CashVan" : "PreSales",
  );
  const [creditStatus, setCreditStatus] = useState<CustomerCreditStatusDto | null>(null);
  const [loadingCredit, setLoadingCredit] = useState(false);

  const [selectedCategoryId, setSelectedCategoryId] = useState<number>(0);
  const [categoryPrices, setCategoryPrices] = useState<CategoryProductPriceDto[]>([]);
  const [masterProducts, setMasterProducts] = useState<Record<string, MasterProductDto>>({});
  const [loadingProducts, setLoadingProducts] = useState(false);
  const [productSearch, setProductSearch] = useState("");
  const [draftInputs, setDraftInputs] = useState<
    Record<string, { unitId: string; qty: string; price: string }>
  >({});

  const [cart, setCart] = useState<CartLine[]>([]);
  const [idempotencyKey, setIdempotencyKey] = useState<string>(() => createIdempotencyKey());
  const [submitting, setSubmitting] = useState(false);
  const [submittedOrder, setSubmittedOrder] = useState<SalesOrderDto | null>(null);

  useEffect(() => {
    let active = true;
    setLoadingInit(true);
    setError(null);

    Promise.all([
      erpApi.customers.getCustomers({ page: 1, pageSize: 100 }),
      erpApi.inventory.getWarehouses(),
      erpApi.catalog.getCategories(),
    ])
      .then(([custRes, whList, catTree]) => {
        if (!active) return;
        setCustomers(custRes.items);
        setWarehouses(whList);
        const flatCats = flattenCategories(catTree);
        setCategories(flatCats);

        if (custRes.items.length > 0 && !selectedCustomerId) {
          setSelectedCustomerId(custRes.items[0].id);
        }
        if (whList.length > 0) {
          const preferred =
            whList.find((w) => w.id === user?.defaultWarehouseId) ?? whList[0];
          setSelectedWarehouseId(preferred.id);
        }
        if (flatCats.length > 0) {
          setSelectedCategoryId(flatCats[0].id);
        }
      })
      .catch((err) => {
        if (!active) return;
        setError(formatApiError(err, "Failed to initialize sell request data."));
      })
      .finally(() => {
        if (active) setLoadingInit(false);
      });

    return () => {
      active = false;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [user?.defaultWarehouseId]);

  useEffect(() => {
    if (!selectedCustomerId) {
      setCreditStatus(null);
      return;
    }
    let active = true;
    setLoadingCredit(true);
    erpApi.customers
      .getCreditStatus(selectedCustomerId)
      .then((res) => {
        if (active) setCreditStatus(res);
      })
      .catch(() => {
        if (active) setCreditStatus(null);
      })
      .finally(() => {
        if (active) setLoadingCredit(false);
      });
    return () => {
      active = false;
    };
  }, [selectedCustomerId]);

  const loadCategoryProducts = useCallback(async () => {
    if (!selectedCategoryId || !selectedWarehouseId) {
      setCategoryPrices([]);
      return;
    }
    setLoadingProducts(true);
    setError(null);
    try {
      const [priceRes, prodRes] = await Promise.all([
        erpApi.catalog.getCategoryProducts(selectedCategoryId, {
          customerId: selectedCustomerId || undefined,
          warehouseId: selectedWarehouseId,
          page: 1,
          pageSize: 100,
        }),
        erpApi.catalog.getProducts({
          categoryId: selectedCategoryId,
          isActive: true,
          page: 1,
          pageSize: 100,
        }),
      ]);
      const byId: Record<string, MasterProductDto> = {};
      for (const p of prodRes.items) {
        byId[p.id] = p;
      }
      setMasterProducts(byId);
      setCategoryPrices(priceRes.items);
      setDraftInputs((prev) => {
        const next = { ...prev };
        for (const item of priceRes.items) {
          if (!next[item.masterProductId]) {
            next[item.masterProductId] = {
              unitId: item.defaultUnitId,
              qty: "1",
              price: String(item.resolvedUnitPrice),
            };
          }
        }
        return next;
      });
    } catch (err) {
      setError(formatApiError(err, "Failed to load category products."));
    } finally {
      setLoadingProducts(false);
    }
  }, [selectedCategoryId, selectedCustomerId, selectedWarehouseId]);

  useEffect(() => {
    if (step === 1) {
      void loadCategoryProducts();
    }
  }, [step, loadCategoryProducts]);

  const filteredCustomers = useMemo(() => {
    const q = customerSearch.trim().toLowerCase();
    if (!q) return customers;
    return customers.filter(
      (c) =>
        c.nameAr.toLowerCase().includes(q) ||
        (c.nameEn ?? "").toLowerCase().includes(q) ||
        c.customerCode.toLowerCase().includes(q),
    );
  }, [customers, customerSearch]);

  const filteredProducts = useMemo(() => {
    const q = productSearch.trim().toLowerCase();
    if (!q) return categoryPrices;
    return categoryPrices.filter(
      (p) =>
        p.nameAr.toLowerCase().includes(q) ||
        (p.nameEn ?? "").toLowerCase().includes(q) ||
        p.sku.toLowerCase().includes(q),
    );
  }, [categoryPrices, productSearch]);

  const selectedCustomer = customers.find((c) => c.id === selectedCustomerId) ?? null;
  const selectedWarehouse = warehouses.find((w) => w.id === selectedWarehouseId) ?? null;

  const totals = useMemo(() => {
    let subTotal = 0;
    let vatTotal = 0;
    for (const line of cart) {
      const net = Math.round(line.orderedQty * line.unitPrice * 100) / 100;
      const vat = Math.round(net * (line.vatRate / 100) * 100) / 100;
      subTotal += net;
      vatTotal += vat;
    }
    return {
      subTotal,
      vatTotal,
      grandTotal: Math.round((subTotal + vatTotal) * 100) / 100,
    };
  }, [cart]);

  const willTriggerCreditWarning = useMemo(() => {
    if (!creditStatus) return false;
    return (
      creditStatus.overdueDebt > 0 ||
      creditStatus.currentBalance + totals.grandTotal > creditStatus.creditLimit
    );
  }, [creditStatus, totals.grandTotal]);

  function addOrUpdateCartLine(item: CategoryProductPriceDto) {
    const draft = draftInputs[item.masterProductId] ?? {
      unitId: item.defaultUnitId,
      qty: "1",
      price: String(item.resolvedUnitPrice),
    };
    const qty = Number(draft.qty);
    const price = Number(draft.price);
    if (!Number.isFinite(qty) || qty <= 0 || !Number.isFinite(price) || price <= 0) {
      return;
    }

    const master = masterProducts[item.masterProductId];
    const unit =
      master?.units.find((u) => u.id === draft.unitId) ??
      master?.units.find((u) => u.isDefaultUnit) ??
      master?.units[0];

    const unitId = unit?.id ?? item.defaultUnitId;
    const line: CartLine = {
      masterProductId: item.masterProductId,
      productUnitId: unitId,
      sku: item.sku,
      productNameAr: item.nameAr,
      productNameEn: item.nameEn ?? item.nameAr,
      unitNameAr: unit?.unitNameAr ?? "وحدة",
      unitNameEn: unit?.unitNameEn ?? "Unit",
      conversionFactor: unit?.conversionFactor ?? 1,
      standardTierPrice:
        unitId === item.defaultUnitId ? item.resolvedUnitPrice : (unit?.wholesalePrice ?? item.resolvedUnitPrice),
      minAllowedPrice: unit?.minAllowedPrice ?? item.minAllowedPrice,
      unitPrice: price,
      orderedQty: qty,
      availableQty: item.availableQty,
      vatRate: master?.vatRate ?? 15,
    };

    setCart((prev) => {
      const idx = prev.findIndex(
        (c) => c.masterProductId === line.masterProductId && c.productUnitId === line.productUnitId,
      );
      if (idx >= 0) {
        const copy = [...prev];
        copy[idx] = line;
        return copy;
      }
      return [...prev, line];
    });
    setIdempotencyKey(createIdempotencyKey());
  }

  function removeCartLine(masterProductId: string, productUnitId: string) {
    setCart((prev) =>
      prev.filter(
        (c) => !(c.masterProductId === masterProductId && c.productUnitId === productUnitId),
      ),
    );
    setIdempotencyKey(createIdempotencyKey());
  }

  async function handleSubmitOrder() {
    setError(null);
    const payload = {
      customerId: selectedCustomerId,
      warehouseId: selectedWarehouseId,
      orderChannel,
      items: cart.map((c) => ({
        masterProductId: c.masterProductId,
        productUnitId: c.productUnitId,
        orderedQty: c.orderedQty,
        unitPrice: c.unitPrice,
      })),
    };

    const parsed = submitOrderSchema.safeParse(payload);
    if (!parsed.success) {
      setError(parsed.error.issues.map((i) => i.message).join(" | "));
      return;
    }

    setSubmitting(true);
    try {
      const created = await erpApi.sales.createOrder(parsed.data, idempotencyKey);
      setSubmittedOrder(created);
      setCart([]);
      setIdempotencyKey(createIdempotencyKey());
    } catch (err) {
      setError(formatApiError(err, "Failed to submit sell request."));
    } finally {
      setSubmitting(false);
    }
  }

  if (submittedOrder) {
    return (
      <RepMobileShell>
        <SurfaceCard title={t.wizard.submittedTitle} description={t.wizard.submittedDesc}>
          <div className="space-y-4">
            <AlertBanner
              tone="success"
              title={t.wizard.submittedTitle}
              message={
                <span className="font-mono font-bold" dir="ltr">
                  {submittedOrder.orderNumber}
                </span>
              }
            />

            <KeyValueList
              columns={2}
              items={[
                {
                  id: "order-number",
                  label: t.wizard.orderNumber,
                  value: (
                    <span className="font-mono" dir="ltr">
                      {submittedOrder.orderNumber}
                    </span>
                  ),
                },
                {
                  id: "status",
                  label: t.wizard.status,
                  value: (
                    <Badge tone="warning">
                      {t.statusLabels[submittedOrder.status] ?? submittedOrder.status}
                    </Badge>
                  ),
                },
                {
                  id: "grand-total",
                  label: t.wizard.grandTotal,
                  value: (
                    <span className="font-bold tabular-nums">
                      {formatSar(submittedOrder.totalAmount, locale)}
                    </span>
                  ),
                },
                {
                  id: "reservation-expires",
                  label: t.wizard.reservationExpires,
                  value: formatDateTime(submittedOrder.reservationExpiresAtUtc, locale),
                },
              ]}
            />

            <div className="flex flex-col gap-2 pt-2 sm:flex-row">
              <Button className="w-full" onClick={() => navigate("/requests")}>
                {t.wizard.trackRequests}
              </Button>
              <Button
                className="w-full"
                variant="secondary"
                onClick={() => {
                  setSubmittedOrder(null);
                  setStep(0);
                }}
              >
                {t.wizard.createAnother}
              </Button>
            </div>
          </div>
        </SurfaceCard>
      </RepMobileShell>
    );
  }

  return (
    <RepMobileShell>
      {!hasPermission("orders:create") ? (
        <AlertBanner tone="warning" message={t.auth.missingOrderCreatePermission} />
      ) : null}

      <div className="rounded-2xl border border-surface-border bg-surface-card p-3">
        <StepProgress
          activeStepIndex={step}
          onStepSelect={(idx) => setStep(idx)}
          steps={[
            { id: "customer", label: t.wizard.step1Label, description: t.wizard.step1Desc },
            { id: "products", label: t.wizard.step2Label, description: t.wizard.step2Desc },
            { id: "review", label: t.wizard.step3Label, description: t.wizard.step3Desc },
          ]}
        />
      </div>

      {error ? (
        <AlertBanner tone="danger" message={error} onDismiss={() => setError(null)} />
      ) : null}

      {step === 0 ? (
        <div className="space-y-4">
          <SurfaceCard title={t.wizard.selectCustomer} state={loadingInit ? "loading" : "ready"}>
            {customers.length === 0 && !loadingInit ? (
              <EmptyState title={t.wizard.noCustomers} />
            ) : (
              <div className="space-y-3">
                <TextField
                  id="customer-search"
                  label={t.wizard.selectCustomer}
                  placeholder={t.wizard.searchCustomerPlaceholder}
                  value={customerSearch}
                  onChange={(e) => setCustomerSearch(e.target.value)}
                />

                <SelectField
                  id="customer-select"
                  label={t.wizard.selectCustomer}
                  value={selectedCustomerId}
                  onChange={(e) => setSelectedCustomerId(e.target.value)}
                  options={filteredCustomers.map((c) => ({
                    value: c.id,
                    label: `${isAr ? c.nameAr : (c.nameEn ?? c.nameAr)} (${c.customerCode})`,
                  }))}
                />

                <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                  <SelectField
                    id="warehouse-select"
                    label={t.wizard.selectWarehouse}
                    value={String(selectedWarehouseId || "")}
                    onChange={(e) => setSelectedWarehouseId(Number(e.target.value))}
                    options={warehouses.map((w) => ({
                      value: String(w.id),
                      label: `${isAr ? w.nameAr : (w.nameEn ?? w.nameAr)} (${w.warehouseCode})`,
                    }))}
                  />

                  <SelectField
                    id="channel-select"
                    label={t.wizard.orderChannel}
                    value={orderChannel}
                    onChange={(e) => setOrderChannel(e.target.value as "PreSales" | "CashVan")}
                    options={[
                      { value: "PreSales", label: t.wizard.preSales },
                      { value: "CashVan", label: t.wizard.cashVan },
                    ]}
                  />
                </div>
              </div>
            )}
          </SurfaceCard>

          {selectedCustomerId ? (
            <SurfaceCard
              title={t.wizard.creditStatusTitle}
              state={loadingCredit ? "loading" : "ready"}
            >
              {creditStatus ? (
                <div className="space-y-3">
                  <KeyValueList
                    columns={2}
                    items={[
                      {
                        id: "grade",
                        label: t.wizard.grade,
                        value: <Badge tone="gold">{creditStatus.gradeCode}</Badge>,
                      },
                      {
                        id: "payment-days",
                        label: t.wizard.paymentDays,
                        value: (
                          <span className="tabular-nums">
                            {creditStatus.effectivePaymentDays}
                          </span>
                        ),
                      },
                      {
                        id: "credit-limit",
                        label: t.wizard.creditLimit,
                        value: (
                          <span className="tabular-nums">
                            {formatSar(creditStatus.creditLimit, locale)}
                          </span>
                        ),
                      },
                      {
                        id: "current-balance",
                        label: t.wizard.currentBalance,
                        value: (
                          <span className="tabular-nums">
                            {formatSar(creditStatus.currentBalance, locale)}
                          </span>
                        ),
                      },
                      {
                        id: "overdue-debt",
                        label: t.wizard.overdueDebt,
                        value: (
                          <span className="font-semibold tabular-nums">
                            {formatSar(creditStatus.overdueDebt, locale)}
                          </span>
                        ),
                      },
                    ]}
                  />

                  {creditStatus.overdueDebt > 0 ||
                  creditStatus.currentBalance >= creditStatus.creditLimit ? (
                    <AlertBanner tone="warning" message={t.wizard.creditWarning} />
                  ) : null}
                </div>
              ) : null}
            </SurfaceCard>
          ) : null}

          <div className="flex justify-end">
            <Button
              disabled={!selectedCustomerId || !selectedWarehouseId || !hasPermission("orders:create")}
              onClick={() => setStep(1)}
            >
              {t.wizard.next}
            </Button>
          </div>
        </div>
      ) : null}

      {step === 1 ? (
        <div className="space-y-4">
          <SurfaceCard title={t.wizard.categories}>
            <div className="flex flex-wrap gap-2">
              {categories.map((cat) => {
                const active = cat.id === selectedCategoryId;
                return (
                  <button
                    key={cat.id}
                    type="button"
                    onClick={() => setSelectedCategoryId(cat.id)}
                    className={`rounded-full border px-3 py-1.5 text-xs font-semibold transition-colors ${
                      active
                        ? "border-brand-gold bg-brand-gold text-surface-obsidian"
                        : "border-surface-border bg-surface-canvas text-text-primary hover:bg-surface-hover"
                    }`}
                  >
                    {isAr ? cat.nameAr : (cat.nameEn ?? cat.nameAr)}
                  </button>
                );
              })}
            </div>

            <div className="mt-3">
              <TextField
                id="product-filter"
                label={t.wizard.searchProducts}
                placeholder={t.wizard.searchProducts}
                value={productSearch}
                onChange={(e) => setProductSearch(e.target.value)}
              />
            </div>
          </SurfaceCard>

          <SurfaceCard
            title={t.wizard.step2Label}
            state={loadingProducts ? "loading" : "ready"}
          >
            {filteredProducts.length === 0 && !loadingProducts ? (
              <EmptyState title={t.wizard.allCategories} />
            ) : (
              <div className="space-y-3">
                {filteredProducts.map((item) => {
                  const master = masterProducts[item.masterProductId];
                  const draft = draftInputs[item.masterProductId] ?? {
                    unitId: item.defaultUnitId,
                    qty: "1",
                    price: String(item.resolvedUnitPrice),
                  };
                  const unitObj = master?.units.find((u) => u.id === draft.unitId);
                  const qtyNum = Number(draft.qty) || 0;
                  const priceNum = Number(draft.price) || 0;
                  const refTierPrice =
                    draft.unitId === item.defaultUnitId
                      ? item.resolvedUnitPrice
                      : (unitObj?.wholesalePrice ?? item.resolvedUnitPrice);
                  const baseQtyNeeded = qtyNum * (unitObj?.conversionFactor ?? 1);
                  const isPriceOverride = priceNum > 0 && priceNum < refTierPrice;
                  const isShortage = baseQtyNeeded > item.availableQty;
                  const inCart = cart.some(
                    (c) =>
                      c.masterProductId === item.masterProductId &&
                      c.productUnitId === draft.unitId,
                  );

                  return (
                    <div
                      key={item.masterProductId}
                      className="space-y-3 rounded-xl border border-surface-border bg-surface-canvas p-3"
                    >
                      <div className="flex items-start justify-between gap-2">
                        <div className="min-w-0">
                          <div className="font-semibold text-text-primary">
                            {isAr ? item.nameAr : (item.nameEn ?? item.nameAr)}
                          </div>
                          <div className="mt-0.5 font-mono text-xs text-text-secondary" dir="ltr">
                            {item.sku}
                          </div>
                        </div>
                        <div className="flex shrink-0 flex-col items-end gap-1">
                          <Badge tone={item.availableQty > 0 ? "success" : "danger"} size="sm">
                            {t.wizard.availableStock}: {formatNumber(item.availableQty, locale)}
                          </Badge>
                          <span className="text-xs font-semibold tabular-nums text-text-secondary">
                            {t.wizard.tierPrice}: {formatSar(refTierPrice, locale)}
                          </span>
                        </div>
                      </div>

                      <div className="grid grid-cols-1 gap-2 sm:grid-cols-3">
                        {master?.units && master.units.length > 0 ? (
                          <SelectField
                            id={`unit-${item.masterProductId}`}
                            label={t.wizard.unit}
                            value={draft.unitId}
                            onChange={(e) => {
                              const nextUnitId = e.target.value;
                              const chosen = master.units.find((u) => u.id === nextUnitId);
                              const nextPrice =
                                nextUnitId === item.defaultUnitId
                                  ? item.resolvedUnitPrice
                                  : (chosen?.wholesalePrice ?? item.resolvedUnitPrice);
                              setDraftInputs((prev) => ({
                                ...prev,
                                [item.masterProductId]: {
                                  ...draft,
                                  unitId: nextUnitId,
                                  price: String(nextPrice),
                                },
                              }));
                            }}
                            options={master.units.map((u) => ({
                              value: u.id,
                              label: `${isAr ? u.unitNameAr : u.unitNameEn} (×${u.conversionFactor})`,
                            }))}
                          />
                        ) : null}

                        <TextField
                          id={`qty-${item.masterProductId}`}
                          label={t.wizard.quantity}
                          inputMode="decimal"
                          value={draft.qty}
                          onChange={(e) =>
                            setDraftInputs((prev) => ({
                              ...prev,
                              [item.masterProductId]: { ...draft, qty: e.target.value },
                            }))
                          }
                        />

                        <TextField
                          id={`price-${item.masterProductId}`}
                          label={t.wizard.proposedPrice}
                          inputMode="decimal"
                          value={draft.price}
                          onChange={(e) =>
                            setDraftInputs((prev) => ({
                              ...prev,
                              [item.masterProductId]: { ...draft, price: e.target.value },
                            }))
                          }
                        />
                      </div>

                      {isPriceOverride ? (
                        <Badge tone="warning" size="sm">
                          {t.wizard.priceOverrideWarning}
                        </Badge>
                      ) : null}

                      {isShortage ? (
                        <Badge tone="danger" size="sm">
                          {t.wizard.shortageWarning}
                        </Badge>
                      ) : null}

                      <div className="flex justify-end">
                        <Button
                          size="compact"
                          variant={inCart ? "secondary" : "primary"}
                          onClick={() => addOrUpdateCartLine(item)}
                        >
                          {inCart ? t.wizard.updateCart : t.wizard.addToCart}
                        </Button>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </SurfaceCard>

          <div className="sticky bottom-20 z-20 flex items-center justify-between gap-3 rounded-2xl border border-brand-gold bg-surface-obsidian px-4 py-3 text-text-inverse shadow-lg">
            <div>
              <div className="text-xs text-text-muted">
                {cart.length} {t.wizard.cartBarItems}
              </div>
              <div className="text-base font-bold tabular-nums text-brand-gold">
                {formatSar(totals.grandTotal, locale)}
              </div>
            </div>
            <div className="flex items-center gap-2">
              <Button size="compact" variant="secondary" onClick={() => setStep(0)}>
                {t.wizard.back}
              </Button>
              <Button size="compact" disabled={cart.length === 0} onClick={() => setStep(2)}>
                {t.wizard.reviewOrder}
              </Button>
            </div>
          </div>
        </div>
      ) : null}

      {step === 2 ? (
        <div className="space-y-4">
          <SurfaceCard title={t.wizard.summaryTitle}>
            <div className="space-y-4">
              <KeyValueList
                columns={2}
                items={[
                  {
                    id: "summary-customer",
                    label: t.wizard.selectCustomer,
                    value: selectedCustomer
                      ? isAr
                        ? selectedCustomer.nameAr
                        : (selectedCustomer.nameEn ?? selectedCustomer.nameAr)
                      : "—",
                  },
                  {
                    id: "summary-warehouse",
                    label: t.wizard.selectWarehouse,
                    value: selectedWarehouse
                      ? isAr
                        ? selectedWarehouse.nameAr
                        : (selectedWarehouse.nameEn ?? selectedWarehouse.nameAr)
                      : `#${selectedWarehouseId}`,
                  },
                  {
                    id: "summary-channel",
                    label: t.wizard.orderChannel,
                    value: <Badge tone="info">{orderChannel}</Badge>,
                  },
                  {
                    id: "summary-total",
                    label: t.wizard.grandTotal,
                    value: (
                      <span className="font-bold tabular-nums">
                        {formatSar(totals.grandTotal, locale)}
                      </span>
                    ),
                  },
                ]}
              />

              {willTriggerCreditWarning ? (
                <AlertBanner tone="warning" message={t.wizard.creditWarning} />
              ) : null}

              <ul className="divide-y divide-surface-border rounded-xl border border-surface-border bg-surface-canvas px-3">
                {cart.map((line) => {
                  const net = line.orderedQty * line.unitPrice;
                  const vat = net * (line.vatRate / 100);
                  const total = net + vat;
                  const isPriceBelowTier = line.unitPrice < line.standardTierPrice;
                  const isShortage = line.orderedQty * line.conversionFactor > line.availableQty;

                  return (
                    <li
                      key={`${line.masterProductId}-${line.productUnitId}`}
                      className="flex items-start justify-between gap-3 py-3"
                    >
                      <div className="min-w-0 space-y-1">
                        <div className="font-semibold text-text-primary">
                          {isAr ? line.productNameAr : line.productNameEn}
                        </div>
                        <div className="text-xs tabular-nums text-text-secondary">
                          <span className="font-mono" dir="ltr">
                            {line.sku}
                          </span>{" "}
                          • {formatNumber(line.orderedQty, locale)}{" "}
                          {isAr ? line.unitNameAr : line.unitNameEn} ×{" "}
                          {formatSar(line.unitPrice, locale)}
                        </div>
                        <div className="flex flex-wrap gap-1">
                          {isPriceBelowTier ? (
                            <Badge tone="warning" size="sm">
                              MR-02
                            </Badge>
                          ) : null}
                          {isShortage ? (
                            <Badge tone="danger" size="sm">
                              RISK-03
                            </Badge>
                          ) : null}
                        </div>
                      </div>
                      <div className="flex shrink-0 flex-col items-end gap-1">
                        <span className="text-sm font-bold tabular-nums">
                          {formatSar(total, locale)}
                        </span>
                        <Button
                          size="compact"
                          variant="ghost"
                          onClick={() => removeCartLine(line.masterProductId, line.productUnitId)}
                        >
                          {t.wizard.remove}
                        </Button>
                      </div>
                    </li>
                  );
                })}
              </ul>

              <div className="space-y-1.5 rounded-xl bg-surface-canvas p-3 text-sm">
                <div className="flex justify-between text-text-secondary">
                  <span>{t.wizard.subtotal}</span>
                  <span className="tabular-nums">{formatSar(totals.subTotal, locale)}</span>
                </div>
                <div className="flex justify-between text-text-secondary">
                  <span>{t.wizard.vat15}</span>
                  <span className="tabular-nums">{formatSar(totals.vatTotal, locale)}</span>
                </div>
                <div className="flex justify-between border-t border-surface-border pt-2 text-base font-bold text-text-primary">
                  <span>{t.wizard.grandTotal}</span>
                  <span className="tabular-nums">{formatSar(totals.grandTotal, locale)}</span>
                </div>
              </div>

              <div className="flex items-center justify-between gap-2 pt-2">
                <Button variant="secondary" onClick={() => setStep(1)} disabled={submitting}>
                  {t.wizard.back}
                </Button>
                <Button
                  loading={submitting}
                  disabled={cart.length === 0 || !isOnline || !hasPermission("orders:create")}
                  onClick={() => void handleSubmitOrder()}
                >
                  {submitting ? t.wizard.submitting : t.wizard.submitToManager}
                </Button>
              </div>
            </div>
          </SurfaceCard>
        </div>
      ) : null}
    </RepMobileShell>
  );
}
