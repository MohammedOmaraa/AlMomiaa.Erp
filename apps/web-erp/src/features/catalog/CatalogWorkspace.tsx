"use client";

import { useCallback, useEffect, useMemo, useState, type FormEvent } from "react";
import type {
  BrandDto,
  CategoryNodeDto,
  CategoryProductPriceDto,
  CreateProductUnitRequest,
  InitialWarehouseStockRequest,
  MasterProductDto,
  PriceListDto,
  ProductAvailabilityDto,
  WarehouseDto,
} from "@almomiaa/api-client";
import {
  AlertBanner,
  Badge,
  Button,
  DataTable,
  DrawerModal,
  KeyValueList,
  PaginationBar,
  SelectField,
  SurfaceCard,
  Tabs,
  TextField,
  type DataTableColumn,
} from "@almomiaa/ui";
import { ErpAppLayout } from "../../components/ErpAppLayout";
import { erpApi, formatApiError } from "../../lib/api-client";
import { formatNumber, formatSar } from "../../lib/formatters";
import { hasPermission } from "../../lib/permissions";
import { useAuth } from "../../providers/AuthProvider";
import { useLocale } from "../../providers/LocaleProvider";

interface BaseUnitMeta {
  value: "Piece" | "Box" | "Carton" | "Pallet";
  ar: string;
  en: string;
  descAr: string;
  descEn: string;
}

const BASE_UNIT_OPTIONS: readonly BaseUnitMeta[] = [
  {
    value: "Piece",
    ar: "حبة",
    en: "Piece",
    descAr: "وحدة بيع مفردة (مثال: علبة واحدة أو زجاجة واحدة)",
    descEn: "Single sellable item (e.g., 1 bottle or 1 unit)",
  },
  {
    value: "Box",
    ar: "علبة / ربطة",
    en: "Box / Pack",
    descAr: "عبوة وسطى تحتوي على عدة حبات",
    descEn: "Intermediate box or shrink-wrapped pack",
  },
  {
    value: "Carton",
    ar: "كرتون",
    en: "Carton",
    descAr: "كرتون جملة قياسي للتوزيع والمستودعات",
    descEn: "Standard wholesale distribution carton",
  },
  {
    value: "Pallet",
    ar: "طبلية",
    en: "Pallet",
    descAr: "طبلية كاملة للشحن والتخزين بالجملة الكبرى",
    descEn: "Full warehouse pallet for bulk B2B distribution",
  },
] as const;

interface AdditionalUnitDraft {
  id: string;
  unitNameAr: string;
  unitNameEn: string;
  conversionFactor: string;
  barcode: string;
  wholesalePrice: string;
  retailPrice: string;
  minAllowedPrice: string;
}

interface WarehouseAllocationDraft {
  selected: boolean;
  initialQty: string;
  minReorderQty: string;
}

function getBaseUnitMeta(unitCode: string): BaseUnitMeta {
  const normalized = unitCode.trim().toLowerCase();
  const match = BASE_UNIT_OPTIONS.find((item) => item.value.toLowerCase() === normalized);
  return match ?? BASE_UNIT_OPTIONS[0];
}

function flattenCategories(
  nodes: CategoryNodeDto[],
  depth = 0,
): Array<{ id: number; labelAr: string; labelEn: string; depth: number }> {
  const out: Array<{ id: number; labelAr: string; labelEn: string; depth: number }> = [];
  for (const n of nodes) {
    out.push({ id: n.id, labelAr: n.nameAr, labelEn: n.nameEn ?? n.nameAr, depth });
    if (n.children.length > 0) {
      out.push(...flattenCategories(n.children, depth + 1));
    }
  }
  return out;
}

function toSafeNumber(raw: string): number {
  const parsed = Number(raw);
  return Number.isFinite(parsed) ? parsed : 0;
}

function formatWithVat(price: number, vatRate: number): number {
  return Math.round(price * (1 + vatRate / 100) * 100) / 100;
}

export function CatalogWorkspace() {
  const { locale } = useLocale();
  const { status, user } = useAuth();
  const isAr = locale === "ar-SA";

  const [tab, setTab] = useState("products");
  const [products, setProducts] = useState<MasterProductDto[]>([]);
  const [totalProducts, setTotalProducts] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [categories, setCategories] = useState<CategoryNodeDto[]>([]);
  const [brands, setBrands] = useState<BrandDto[]>([]);
  const [priceLists, setPriceLists] = useState<PriceListDto[]>([]);
  const [warehouses, setWarehouses] = useState<WarehouseDto[]>([]);
  const [catPrices, setCatPrices] = useState<CategoryProductPriceDto[]>([]);
  const [lookupCatId, setLookupCatId] = useState("");
  const [lookupWhId, setLookupWhId] = useState("");
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [banner, setBanner] = useState<{
    tone: "success" | "danger" | "warning";
    title: string;
    message: string;
    details?: string[];
  } | null>(null);

  // Stock Availability Breakdown Drawer
  const [availabilityOpen, setAvailabilityOpen] = useState(false);
  const [availabilityLoading, setAvailabilityLoading] = useState(false);
  const [availabilityData, setAvailabilityData] = useState<ProductAvailabilityDto | null>(null);

  // Create Product Drawer
  const [prodOpen, setProdOpen] = useState(false);
  const [sku, setSku] = useState("");
  const [barcode, setBarcode] = useState("");
  const [nameAr, setNameAr] = useState("");
  const [nameEn, setNameEn] = useState("");
  const [catId, setCatId] = useState("");
  const [brandId, setBrandId] = useState("");
  const [baseUnit, setBaseUnit] = useState<BaseUnitMeta["value"]>("Piece");
  const [vatRate, setVatRate] = useState("15");
  const [wac, setWac] = useState("10");
  const [minPrice, setMinPrice] = useState("15");
  const [wholesalePrice, setWholesalePrice] = useState("20");
  const [retailPrice, setRetailPrice] = useState("25");
  const [extraUnits, setExtraUnits] = useState<AdditionalUnitDraft[]>([]);
  const [warehouseAllocations, setWarehouseAllocations] = useState<Record<number, WarehouseAllocationDraft>>({});

  // Link Supplier Drawer
  const [linkTarget, setLinkTarget] = useState<MasterProductDto | null>(null);
  const [linkSupId, setLinkSupId] = useState("");
  const [linkSupSku, setLinkSupSku] = useState("");
  const [linkSupPrice, setLinkSupPrice] = useState("11");

  // Category / Brand / PriceList quick forms
  const [newCatAr, setNewCatAr] = useState("");
  const [newCatEn, setNewCatEn] = useState("");
  const [newBrandAr, setNewBrandAr] = useState("");
  const [newBrandEn, setNewBrandEn] = useState("");
  const [plOpen, setPlOpen] = useState(false);
  const [plCode, setPlCode] = useState("");
  const [plNameAr, setPlNameAr] = useState("");
  const [plUnitId, setPlUnitId] = useState("");
  const [plPrice, setPlPrice] = useState("19");

  const canManage = hasPermission(user, "catalog:manage");
  const flatCats = useMemo(() => flattenCategories(categories), [categories]);
  const selectedUnitMeta = useMemo(() => getBaseUnitMeta(baseUnit), [baseUnit]);
  const unitLabelAr = selectedUnitMeta.ar;
  const unitLabelEn = selectedUnitMeta.en;

  const wacNum = toSafeNumber(wac);
  const minPriceNum = toSafeNumber(minPrice);
  const wholesaleNum = toSafeNumber(wholesalePrice);
  const retailNum = toSafeNumber(retailPrice);
  const vatNum = toSafeNumber(vatRate);

  const pricingMarginPct =
    wholesaleNum > 0 ? Math.round(((wholesaleNum - wacNum) / wholesaleNum) * 1000) / 10 : 0;
  const pricingHasFloorWarning = minPriceNum > wholesaleNum || wholesaleNum > retailNum;

  const activeWarehouses = useMemo(() => warehouses.filter((w) => w.isActive), [warehouses]);

  const allocationSummary = useMemo(() => {
    let selectedCount = 0;
    let totalInitialQty = 0;
    for (const wh of activeWarehouses) {
      const entry = warehouseAllocations[wh.id];
      if (entry?.selected) {
        selectedCount += 1;
        totalInitialQty += Math.max(0, toSafeNumber(entry.initialQty));
      }
    }
    return { selectedCount, totalInitialQty };
  }, [activeWarehouses, warehouseAllocations]);

  const loadData = useCallback(async () => {
    setLoading(true);
    try {
      const [prodRes, catRes, brandRes, plRes, whRes] = await Promise.all([
        erpApi.catalog.getProducts({ search: search || undefined, page, pageSize: 25 }),
        erpApi.catalog.getCategories(),
        erpApi.catalog.getBrands(),
        erpApi.catalog.getPriceLists(),
        erpApi.inventory.getWarehouses().catch(() => [] as WarehouseDto[]),
      ]);
      setProducts(prodRes.items);
      setTotalProducts(prodRes.totalCount);
      setCategories(catRes);
      setBrands(brandRes);
      setPriceLists(plRes);
      setWarehouses(whRes);

      const flat = flattenCategories(catRes);
      if (!catId && flat[0]) setCatId(String(flat[0].id));
      if (!lookupCatId && flat[0]) setLookupCatId(String(flat[0].id));
      if (!brandId && brandRes[0]) setBrandId(String(brandRes[0].id));
      if (!plUnitId && prodRes.items[0]?.units[0]) setPlUnitId(prodRes.items[0].units[0].id);

      setWarehouseAllocations((prev) => {
        const next: Record<number, WarehouseAllocationDraft> = { ...prev };
        for (const wh of whRes) {
          if (!next[wh.id]) {
            next[wh.id] = { selected: false, initialQty: "0", minReorderQty: "10" };
          }
        }
        return next;
      });
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setLoading(false);
    }
  }, [brandId, catId, lookupCatId, page, plUnitId, search]);

  useEffect(() => {
    if (status !== "authenticated" || !user) return;
    void loadData();
  }, [loadData, status, user]);

  const handleOpenAvailability = async (productId: string) => {
    setAvailabilityOpen(true);
    setAvailabilityLoading(true);
    setAvailabilityData(null);
    try {
      const res = await erpApi.catalog.getProductAvailability(productId);
      setAvailabilityData(res);
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
      setAvailabilityOpen(false);
    } finally {
      setAvailabilityLoading(false);
    }
  };

  const handleSelectAllWarehouses = () => {
    setWarehouseAllocations((prev) => {
      const next: Record<number, WarehouseAllocationDraft> = { ...prev };
      for (const wh of activeWarehouses) {
        next[wh.id] = {
          selected: true,
          initialQty: prev[wh.id]?.initialQty ?? "0",
          minReorderQty: prev[wh.id]?.minReorderQty ?? "10",
        };
      }
      return next;
    });
  };

  const handleClearAllWarehouses = () => {
    setWarehouseAllocations((prev) => {
      const next: Record<number, WarehouseAllocationDraft> = { ...prev };
      for (const wh of activeWarehouses) {
        next[wh.id] = {
          selected: false,
          initialQty: prev[wh.id]?.initialQty ?? "0",
          minReorderQty: prev[wh.id]?.minReorderQty ?? "10",
        };
      }
      return next;
    });
  };

  const handleAddPackagingUnit = (preset: "Carton" | "Pallet" | "Box") => {
    const factor = preset === "Box" ? 6 : preset === "Carton" ? 24 : 40;
    const presetAr = preset === "Box" ? "علبة" : preset === "Carton" ? "كرتون" : "طبلية";
    const suffix = String(extraUnits.length + 1).padStart(2, "0");
    const suggestedBarcode = barcode.trim() ? `${barcode.trim()}-${preset.slice(0, 1)}${suffix}` : "";
    setExtraUnits((prev) => [
      ...prev,
      {
        id: `${Date.now()}-${prev.length}`,
        unitNameAr: `${presetAr} (${factor} ${unitLabelAr})`,
        unitNameEn: `${preset} (${factor} ${unitLabelEn})`,
        conversionFactor: String(factor),
        barcode: suggestedBarcode,
        wholesalePrice: String(Math.round(wholesaleNum * factor * 100) / 100),
        retailPrice: String(Math.round(retailNum * factor * 100) / 100),
        minAllowedPrice: String(Math.round(minPriceNum * factor * 100) / 100),
      },
    ]);
  };

  const handleUpdateExtraUnit = (id: string, field: keyof AdditionalUnitDraft, value: string) => {
    setExtraUnits((prev) =>
      prev.map((u) => {
        if (u.id !== id) return u;
        const updated = { ...u, [field]: value };
        if (field === "conversionFactor") {
          const factor = toSafeNumber(value);
          if (factor > 0) {
            updated.wholesalePrice = String(Math.round(wholesaleNum * factor * 100) / 100);
            updated.retailPrice = String(Math.round(retailNum * factor * 100) / 100);
            updated.minAllowedPrice = String(Math.round(minPriceNum * factor * 100) / 100);
          }
        }
        return updated;
      }),
    );
  };

  const handleCreateProduct = async (e: FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setBanner(null);
    try {
      const unitsPayload: CreateProductUnitRequest[] = [
        {
          unitNameAr: unitLabelAr,
          unitNameEn: unitLabelEn,
          conversionFactor: 1,
          barcode: barcode.trim(),
          wholesalePrice: wholesaleNum,
          retailPrice: retailNum,
          minAllowedPrice: minPriceNum,
          isDefaultUnit: true,
        },
        ...extraUnits
          .filter((u) => u.unitNameAr.trim() && u.barcode.trim())
          .map((u) => ({
            unitNameAr: u.unitNameAr.trim(),
            unitNameEn: u.unitNameEn.trim() || u.unitNameAr.trim(),
            conversionFactor: Math.max(0.0001, toSafeNumber(u.conversionFactor)),
            barcode: u.barcode.trim(),
            wholesalePrice: toSafeNumber(u.wholesalePrice),
            retailPrice: toSafeNumber(u.retailPrice),
            minAllowedPrice: toSafeNumber(u.minAllowedPrice),
            isDefaultUnit: false,
          })),
      ];

      const initialWarehouseStocks: InitialWarehouseStockRequest[] = activeWarehouses
        .filter((wh) => warehouseAllocations[wh.id]?.selected)
        .map((wh) => ({
          warehouseId: wh.id,
          initialQty: Math.max(0, toSafeNumber(warehouseAllocations[wh.id]?.initialQty ?? "0")),
          minReorderQty: Math.max(0, toSafeNumber(warehouseAllocations[wh.id]?.minReorderQty ?? "10")),
        }));

      const created = await erpApi.catalog.createProduct({
        sku,
        barcode,
        nameAr,
        nameEn: nameEn || null,
        categoryId: Number(catId),
        brandId: Number(brandId),
        baseUnit,
        weightedAvgCost: wacNum,
        wholesaleSellingPrice: wholesaleNum,
        retailSellingPrice: retailNum,
        minAllowedSellingPrice: minPriceNum,
        vatRate: vatNum,
        units: unitsPayload,
        initialWarehouseStocks: initialWarehouseStocks.length > 0 ? initialWarehouseStocks : undefined,
      });

      const stockSummaryMsg =
        initialWarehouseStocks.length > 0
          ? isAr
            ? ` • تم التوزيع على ${initialWarehouseStocks.length} مستودع بإجمالي ${formatNumber(allocationSummary.totalInitialQty, locale, 2)} ${unitLabelAr}`
            : ` • Allocated across ${initialWarehouseStocks.length} warehouse(s) (${formatNumber(allocationSummary.totalInitialQty, locale, 2)} ${unitLabelEn})`
          : "";

      setBanner({
        tone: "success",
        title: isAr ? "تم إضافة المنتج بنجاح" : "Product Created",
        message: `${created.sku} — ${created.nameAr}${stockSummaryMsg}`,
      });
      setProdOpen(false);
      setSku("");
      setBarcode("");
      setNameAr("");
      setNameEn("");
      setExtraUnits([]);
      handleClearAllWarehouses();
      await loadData();
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setSubmitting(false);
    }
  };

  const handleLinkSupplier = async (e: FormEvent) => {
    e.preventDefault();
    if (!linkTarget) return;
    setSubmitting(true);
    try {
      await erpApi.catalog.linkSupplierProduct(linkTarget.id, {
        supplierId: linkSupId,
        supplierSku: linkSupSku || null,
        lastPurchasePrice: Number(linkSupPrice),
        isPreferredSupplier: true,
      });
      setBanner({ tone: "success", title: isAr ? "تم ربط المورد بالمنتج" : "Supplier Linked", message: linkTarget.sku });
      setLinkTarget(null);
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setSubmitting(false);
    }
  };

  const handleCreateCategory = async (e: FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    try {
      await erpApi.catalog.createCategory({ nameAr: newCatAr, nameEn: newCatEn || null, displayOrder: 1 });
      setNewCatAr("");
      setNewCatEn("");
      setBanner({ tone: "success", title: isAr ? "تم إضافة التصنيف" : "Category Created", message: newCatAr });
      await loadData();
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setSubmitting(false);
    }
  };

  const handleCreateBrand = async (e: FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    try {
      await erpApi.catalog.createBrand({ nameAr: newBrandAr, nameEn: newBrandEn || null });
      setNewBrandAr("");
      setNewBrandEn("");
      setBanner({ tone: "success", title: isAr ? "تم إضافة العلامة التجارية" : "Brand Created", message: newBrandAr });
      await loadData();
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setSubmitting(false);
    }
  };

  const handleCreatePriceList = async (e: FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    try {
      await erpApi.catalog.createPriceList({
        priceListCode: plCode,
        nameAr: plNameAr,
        isDefault: false,
        items: plUnitId ? [{ productUnitId: plUnitId, minQuantity: 1, tierUnitPrice: Number(plPrice) }] : [],
      });
      setPlOpen(false);
      setPlCode("");
      setPlNameAr("");
      setBanner({ tone: "success", title: isAr ? "تم إنشاء قائمة الأسعار" : "Price List Created", message: plCode });
      await loadData();
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setSubmitting(false);
    }
  };

  const handleLookupCategoryPrices = async (overrideWhId?: string) => {
    if (!lookupCatId) return;
    const targetWh = overrideWhId !== undefined ? overrideWhId : lookupWhId;
    setSubmitting(true);
    try {
      const res = await erpApi.catalog.getCategoryProducts(Number(lookupCatId), {
        warehouseId: targetWh ? Number(targetWh) : undefined,
      });
      setCatPrices(res.items);
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setSubmitting(false);
    }
  };

  const productColumns: DataTableColumn<MasterProductDto>[] = [
    { key: "sku", header: "SKU", mono: true, render: (p) => p.sku },
    {
      key: "name",
      header: isAr ? "المنتج" : "Product",
      render: (p) => (
        <div>
          <p className="font-medium text-text-primary">{isAr ? p.nameAr : (p.nameEn ?? p.nameAr)}</p>
          <p className="font-mono text-xs text-text-muted">{p.barcode}</p>
        </div>
      ),
    },
    {
      key: "baseUnit",
      header: isAr ? "وحدة التسعير" : "Base Unit",
      render: (p) => {
        const meta = getBaseUnitMeta(p.baseUnit);
        return (
          <Badge tone="gold">
            {isAr ? `${meta.ar} (${meta.en})` : `${meta.en} (${meta.ar})`}
          </Badge>
        );
      },
    },
    {
      key: "wac",
      header: isAr ? "التكلفة (WAC)" : "WAC",
      align: "end",
      numeric: true,
      render: (p) =>
        p.weightedAvgCost === null ? (
          <Badge tone="neutral">{isAr ? "مخفي" : "Masked"}</Badge>
        ) : (
          formatSar(p.weightedAvgCost, locale)
        ),
    },
    {
      key: "min",
      header: isAr ? "الحد الأدنى (MR-02)" : "Min Floor",
      align: "end",
      numeric: true,
      render: (p) => formatSar(p.minAllowedSellingPrice, locale),
    },
    {
      key: "wh",
      header: isAr ? "الجملة" : "Wholesale",
      align: "end",
      numeric: true,
      render: (p) => formatSar(p.wholesaleSellingPrice, locale),
    },
    {
      key: "rt",
      header: isAr ? "التجزئة" : "Retail",
      align: "end",
      numeric: true,
      render: (p) => formatSar(p.retailSellingPrice, locale),
    },
    { key: "units", header: isAr ? "الوحدات" : "Units", numeric: true, render: (p) => p.units.length },
    {
      key: "act",
      header: isAr ? "إجراءات" : "Actions",
      render: (p) => (
        <div className="flex flex-wrap gap-1.5">
          <Button variant="secondary" size="compact" onClick={() => void handleOpenAvailability(p.id)}>
            {isAr ? "توزيع المستودعات" : "Warehouses"}
          </Button>
          {canManage && (
            <Button variant="ghost" size="compact" onClick={() => setLinkTarget(p)}>
              {isAr ? "ربط مورد" : "Link Supplier"}
            </Button>
          )}
        </div>
      ),
    },
  ];

  return (
    <ErpAppLayout
      title={isAr ? "الكتالوج والمنتجات وقوائم الأسعار" : "Catalog, Multi-Unit Products & Price Lists"}
      subtitle={
        isAr
          ? "إدارة التصنيفات والعلامات التجارية والوحدات التفصيلية وتوزيع المخزون متعدد المستودعات (MR-02)"
          : "Manage categories, brands, detailed per-unit pricing, and cross-warehouse stock availability"
      }
      requiredPermissions={["catalog:view", "catalog:manage"]}
      headerActions={
        <div className="flex flex-wrap gap-2">
          {canManage && (
            <Button size="compact" onClick={() => setProdOpen(true)}>
              {isAr ? "+ منتج جديد" : "+ New Product"}
            </Button>
          )}
          {canManage && (
            <Button variant="secondary" size="compact" onClick={() => setPlOpen(true)}>
              {isAr ? "+ قائمة أسعار" : "+ Price List"}
            </Button>
          )}
          <Button variant="ghost" size="compact" onClick={() => void loadData()}>
            {isAr ? "تحديث" : "Refresh"}
          </Button>
        </div>
      }
    >
      {banner && (
        <AlertBanner
          tone={banner.tone}
          title={banner.title}
          message={banner.message}
          details={banner.details}
          onDismiss={() => setBanner(null)}
        />
      )}

      <Tabs
        activeId={tab}
        onChange={setTab}
        items={[
          { id: "products", label: isAr ? "المنتجات" : "Products", badge: totalProducts },
          { id: "lookup", label: isAr ? "استعلام سعر وتوفر (كل المستودعات)" : "Price & Multi-Warehouse Lookup" },
          { id: "taxonomy", label: isAr ? "التصنيفات والعلامات" : "Categories & Brands", badge: flatCats.length + brands.length },
          { id: "pricelists", label: isAr ? "قوائم الأسعار" : "Price Lists", badge: priceLists.length },
        ]}
      />

      {tab === "products" && (
        <SurfaceCard title={isAr ? "المنتجات الرئيسية" : "Master Products"}>
          <div className="space-y-4">
            <div className="flex flex-wrap items-end gap-2">
              <div className="w-full sm:w-72">
                <TextField
                  label={isAr ? "بحث بالاسم أو SKU أو الباركود" : "Search SKU / Barcode / Name"}
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                />
              </div>
              <Button variant="secondary" onClick={() => void loadData()}>
                {isAr ? "بحث" : "Search"}
              </Button>
            </div>
            <DataTable columns={productColumns} rows={products} rowKey={(p) => p.id} loading={loading} />
            <PaginationBar page={page} pageSize={25} totalCount={totalProducts} onPageChange={setPage} />
          </div>
        </SurfaceCard>
      )}

      {tab === "lookup" && (
        <SurfaceCard
          title={isAr ? "استعلام السعر والتوفر عبر جميع المستودعات" : "Cross-Warehouse Price & Availability Lookup"}
          description={
            isAr
              ? "استعرض السعر المعتمد والكمية الإجمالية في كل المستودعات مع تفصيل الكمية المتاحة داخل كل مستودع بدقة"
              : "Inspect resolved pricing alongside total available stock across all warehouses and exact per-warehouse counts"
          }
        >
          <div className="space-y-4">
            <div className="flex flex-wrap items-end gap-3">
              <div className="w-full sm:w-64">
                <SelectField
                  label={isAr ? "التصنيف" : "Category"}
                  value={lookupCatId}
                  onChange={(e) => setLookupCatId(e.target.value)}
                  options={flatCats.map((c) => ({ value: String(c.id), label: isAr ? c.labelAr : c.labelEn }))}
                />
              </div>
              <div className="w-full sm:w-72">
                <SelectField
                  label={isAr ? "نطاق المستودع" : "Warehouse Scope"}
                  value={lookupWhId}
                  onChange={(e) => setLookupWhId(e.target.value)}
                  options={[
                    {
                      value: "",
                      label: isAr ? "جميع المستودعات (الإجمالي + التفصيل)" : "All Warehouses (Total + Breakdown)",
                    },
                    ...activeWarehouses.map((w) => ({
                      value: String(w.id),
                      label: `${w.warehouseCode} — ${isAr ? w.nameAr : (w.nameEn ?? w.nameAr)}`,
                    })),
                  ]}
                />
              </div>
              <Button loading={submitting} onClick={() => void handleLookupCategoryPrices()}>
                {isAr ? "استعلام فوري" : "Query Availability"}
              </Button>
            </div>

            <DataTable
              rows={catPrices}
              rowKey={(r) => r.masterProductId}
              columns={[
                { key: "sku", header: "SKU", mono: true, render: (r) => r.sku },
                {
                  key: "name",
                  header: isAr ? "المنتج والوحدة" : "Product & Unit",
                  render: (r) => {
                    const unitMeta = getBaseUnitMeta(r.baseUnit ?? "Piece");
                    return (
                      <div>
                        <p className="font-medium text-text-primary">{isAr ? r.nameAr : (r.nameEn ?? r.nameAr)}</p>
                        <p className="text-xs text-text-muted">
                          {isAr
                            ? `الوحدة: ${r.defaultUnitNameAr ?? unitMeta.ar}`
                            : `Unit: ${r.defaultUnitNameEn ?? unitMeta.en}`}
                        </p>
                      </div>
                    );
                  },
                },
                {
                  key: "price",
                  header: isAr ? "السعر المعتمد" : "Resolved Price",
                  align: "end",
                  numeric: true,
                  render: (r) => formatSar(r.resolvedUnitPrice, locale),
                },
                {
                  key: "min",
                  header: isAr ? "الحد الأدنى" : "Min Floor",
                  align: "end",
                  numeric: true,
                  render: (r) => formatSar(r.minAllowedPrice, locale),
                },
                {
                  key: "total",
                  header: isAr ? "إجمالي كل المستودعات" : "Total (All Warehouses)",
                  align: "end",
                  numeric: true,
                  render: (r) => {
                    const totalQty = r.totalAvailableQty ?? r.availableQty;
                    return (
                      <Badge tone={totalQty > 0 ? "success" : "danger"} mono>
                        {formatNumber(totalQty, locale, 2)}
                      </Badge>
                    );
                  },
                },
                ...(lookupWhId
                  ? [
                      {
                        key: "selectedWh",
                        header: isAr ? "المتاح بالمستودع المحدد" : "Selected WH Qty",
                        align: "end" as const,
                        numeric: true,
                        render: (r: CategoryProductPriceDto) => (
                          <Badge tone={r.availableQty > 0 ? "gold" : "warning"} mono>
                            {formatNumber(r.availableQty, locale, 2)}
                          </Badge>
                        ),
                      },
                    ]
                  : []),
                {
                  key: "breakdown",
                  header: isAr ? "تفصيل المستودعات" : "Per-Warehouse Breakdown",
                  render: (r) => {
                    const stocks = r.warehouseStocks ?? [];
                    if (stocks.length === 0) {
                      return <span className="text-xs text-text-muted">{isAr ? "لا يوجد رصيد مسجل" : "No warehouse stock"}</span>;
                    }
                    return (
                      <div className="flex flex-wrap gap-1.5">
                        {stocks.map((ws) => (
                          <Badge
                            key={ws.warehouseId}
                            tone={ws.availableQty <= ws.minReorderQty ? "warning" : "info"}
                            mono
                          >
                            {ws.warehouseCode}: {formatNumber(ws.availableQty, locale, 2)}
                          </Badge>
                        ))}
                      </div>
                    );
                  },
                },
                {
                  key: "details",
                  header: isAr ? "التفصيل الكامل" : "Full Breakdown",
                  render: (r) => (
                    <Button variant="secondary" size="compact" onClick={() => void handleOpenAvailability(r.masterProductId)}>
                      {isAr ? "عرض التفصيل" : "Inspect"}
                    </Button>
                  ),
                },
              ]}
            />
          </div>
        </SurfaceCard>
      )}

      {tab === "taxonomy" && (
        <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
          <SurfaceCard title={isAr ? "شجرة التصنيفات" : "Category Tree"}>
            <div className="space-y-4">
              {canManage && (
                <form onSubmit={handleCreateCategory} className="flex flex-wrap items-end gap-2">
                  <div className="flex-1">
                    <TextField label={isAr ? "تصنيف جديد (عربي)" : "Category (AR)"} value={newCatAr} onChange={(e) => setNewCatAr(e.target.value)} required />
                  </div>
                  <div className="flex-1">
                    <TextField label={isAr ? "الاسم (إنجليزي)" : "Category (EN)"} value={newCatEn} onChange={(e) => setNewCatEn(e.target.value)} />
                  </div>
                  <Button type="submit" loading={submitting}>{isAr ? "إضافة" : "Add"}</Button>
                </form>
              )}
              <DataTable
                rows={flatCats}
                rowKey={(c) => String(c.id)}
                columns={[
                  { key: "id", header: "ID", mono: true, render: (c) => c.id },
                  { key: "name", header: isAr ? "التصنيف" : "Category", render: (c) => (isAr ? c.labelAr : c.labelEn) },
                ]}
              />
            </div>
          </SurfaceCard>

          <SurfaceCard title={isAr ? "العلامات التجارية" : "Brands"}>
            <div className="space-y-4">
              {canManage && (
                <form onSubmit={handleCreateBrand} className="flex flex-wrap items-end gap-2">
                  <div className="flex-1">
                    <TextField label={isAr ? "علامة جديدة (عربي)" : "Brand (AR)"} value={newBrandAr} onChange={(e) => setNewBrandAr(e.target.value)} required />
                  </div>
                  <div className="flex-1">
                    <TextField label={isAr ? "الاسم (إنجليزي)" : "Brand (EN)"} value={newBrandEn} onChange={(e) => setNewBrandEn(e.target.value)} />
                  </div>
                  <Button type="submit" loading={submitting}>{isAr ? "إضافة" : "Add"}</Button>
                </form>
              )}
              <DataTable
                rows={brands}
                rowKey={(b) => String(b.id)}
                columns={[
                  { key: "id", header: "ID", mono: true, render: (b) => b.id },
                  { key: "name", header: isAr ? "العلامة" : "Brand", render: (b) => (isAr ? b.nameAr : (b.nameEn ?? b.nameAr)) },
                ]}
              />
            </div>
          </SurfaceCard>
        </div>
      )}

      {tab === "pricelists" && (
        <SurfaceCard title={isAr ? "قوائم الأسعار المتدرجة (MR-02)" : "Tiered Price Lists (MR-02)"}>
          <DataTable
            rows={priceLists}
            rowKey={(p) => String(p.id)}
            loading={loading}
            columns={[
              { key: "code", header: isAr ? "الرمز" : "Code", mono: true, render: (p) => p.priceListCode },
              { key: "name", header: isAr ? "الاسم" : "Name", render: (p) => (isAr ? p.nameAr : (p.nameEn ?? p.nameAr)) },
              {
                key: "def",
                header: isAr ? "افتراضي" : "Default",
                render: (p) => <Badge tone={p.isDefault ? "gold" : "neutral"}>{p.isDefault ? "Default" : "Custom"}</Badge>,
              },
              { key: "items", header: isAr ? "البنود" : "Items", numeric: true, render: (p) => p.items.length },
            ]}
          />
        </SurfaceCard>
      )}

      {/* Create Product Drawer — Dynamic Base Unit, Detailed Pricing, Extra Units & Multi-Warehouse Allocation */}
      <DrawerModal
        open={prodOpen}
        onClose={() => setProdOpen(false)}
        title={isAr ? "إضافة منتج رئيسي جديد وتوزيعه على المستودعات" : "Create Master Product & Allocate Warehouses"}
        subtitle={
          isAr
            ? "حدد وحدة التسعير الأساسية، تفاصيل الأسعار، وحدات التعبئة الإضافية، والكميات الافتتاحية لكل مستودع"
            : "Configure base unit pricing, optional packaging units, and opening stock per warehouse"
        }
        footer={
          <>
            <Button variant="secondary" onClick={() => setProdOpen(false)}>
              {isAr ? "إلغاء" : "Cancel"}
            </Button>
            <Button type="submit" form="create-product-form" loading={submitting}>
              {isAr ? "حفظ المنتج وتثبيت الأرصدة" : "Save Product & Stock"}
            </Button>
          </>
        }
      >
        <form id="create-product-form" onSubmit={handleCreateProduct} className="space-y-5">
          {/* Section 1: Identity & Base Unit */}
          <div className="space-y-3 rounded-xl border border-surface-border bg-surface-canvas p-4">
            <h4 className="text-sm font-semibold text-text-primary">
              {isAr ? "1. بيانات المنتج ووحدة الأساس" : "1. Product Identity & Base Unit"}
            </h4>
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <TextField label="SKU" value={sku} onChange={(e) => setSku(e.target.value)} required placeholder="ALM-PROD-001" />
              <TextField label={isAr ? "الباركود الأساسي" : "Primary Barcode"} value={barcode} onChange={(e) => setBarcode(e.target.value)} required placeholder="6281000012345" />
              <TextField label={isAr ? "الاسم بالعربية" : "Name (AR)"} value={nameAr} onChange={(e) => setNameAr(e.target.value)} required />
              <TextField label={isAr ? "الاسم بالإنجليزية" : "Name (EN)"} value={nameEn} onChange={(e) => setNameEn(e.target.value)} />
              <SelectField
                label={isAr ? "التصنيف" : "Category"}
                value={catId}
                onChange={(e) => setCatId(e.target.value)}
                options={flatCats.map((c) => ({ value: String(c.id), label: isAr ? c.labelAr : c.labelEn }))}
              />
              <SelectField
                label={isAr ? "العلامة التجارية" : "Brand"}
                value={brandId}
                onChange={(e) => setBrandId(e.target.value)}
                options={brands.map((b) => ({ value: String(b.id), label: isAr ? b.nameAr : (b.nameEn ?? b.nameAr) }))}
              />
            </div>

            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <SelectField
                label={isAr ? "وحدة الأساس للتسعير والجرد" : "Base Pricing & Inventory Unit"}
                value={baseUnit}
                onChange={(e) => setBaseUnit(e.target.value as BaseUnitMeta["value"])}
                hint={isAr ? selectedUnitMeta.descAr : selectedUnitMeta.descEn}
                options={BASE_UNIT_OPTIONS.map((opt) => ({
                  value: opt.value,
                  label: isAr ? `${opt.ar} (${opt.en})` : `${opt.en} (${opt.ar})`,
                }))}
              />
              <TextField
                label={isAr ? "نسبة ضريبة القيمة المضافة (%)" : "VAT Rate (%)"}
                value={vatRate}
                inputMode="decimal"
                onChange={(e) => setVatRate(e.target.value)}
                hint={isAr ? "النسبة النظامية في السعودية 15%" : "Standard KSA ZATCA VAT rate is 15%"}
              />
            </div>
          </div>

          {/* Section 2: Detailed Pricing per Selected Base Unit */}
          <div className="space-y-3 rounded-xl border border-surface-border bg-surface-canvas p-4">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <h4 className="text-sm font-semibold text-text-primary">
                {isAr
                  ? `2. التسعير التفصيلي لكل (${unitLabelAr}) — قبل الضريبة`
                  : `2. Detailed Pricing per (${unitLabelEn}) — Excl. VAT`}
              </h4>
              <Badge tone={pricingMarginPct >= 15 ? "success" : "warning"} mono>
                {isAr ? `هامش الجملة: ${pricingMarginPct}%` : `Wholesale Margin: ${pricingMarginPct}%`}
              </Badge>
            </div>

            <p className="text-xs text-text-muted">
              {isAr
                ? `جميع القيم التالية تُحسب لكل ${unitLabelAr} واحد (1 ${unitLabelEn}). إذا أردت التسعير للكرتون أو الطبلية غيّر "وحدة الأساس" أعلاه أو أضف وحدات تعبئة إضافية بالأسفل.`
                : `All figures below apply to 1 ${unitLabelEn} (${unitLabelAr}). Switch Base Unit above or add packaging units below for Carton/Pallet pricing.`}
            </p>

            {pricingHasFloorWarning && (
              <AlertBanner
                tone="warning"
                title={isAr ? "تنبيه تسلسل الأسعار" : "Pricing Hierarchy Notice"}
                message={
                  isAr
                    ? "يُفضل أن يكون الحد الأدنى ≤ سعر الجملة ≤ سعر التجزئة لضمان سلامة قاعدة MR-02."
                    : "Recommended hierarchy: Min Allowed Floor ≤ Wholesale ≤ Retail (MR-02)."
                }
              />
            )}

            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <TextField
                label={isAr ? `متوسط التكلفة (WAC) لكل ${unitLabelAr} (ر.س)` : `Weighted Avg Cost (WAC) / ${unitLabelEn} (SAR)`}
                value={wac}
                inputMode="decimal"
                onChange={(e) => setWac(e.target.value)}
                required
                hint={
                  isAr
                    ? `التكلفة الواصلة للمستودع لكل ${unitLabelAr} (مخفية عن مناديب المبيعات)`
                    : `Landed unit cost per ${unitLabelEn} (masked from sales reps)`
                }
              />
              <TextField
                label={isAr ? `الحد الأدنى المسموح للبيع لكل ${unitLabelAr} — MR-02 (ر.س)` : `Min Allowed Floor / ${unitLabelEn} — MR-02 (SAR)`}
                value={minPrice}
                inputMode="decimal"
                onChange={(e) => setMinPrice(e.target.value)}
                required
                hint={
                  isAr
                    ? `أقل سعر بيع مسموح للـ${unitLabelAr} (شامل الضريبة: ${formatSar(formatWithVat(minPriceNum, vatNum), locale)})`
                    : `Strict price floor per ${unitLabelEn} (Incl. VAT: ${formatSar(formatWithVat(minPriceNum, vatNum), locale)})`
                }
              />
              <TextField
                label={isAr ? `سعر الجملة لكل ${unitLabelAr} (ر.س)` : `Wholesale Price / ${unitLabelEn} (SAR)`}
                value={wholesalePrice}
                inputMode="decimal"
                onChange={(e) => setWholesalePrice(e.target.value)}
                required
                hint={
                  isAr
                    ? `السعر الافتراضي لطلبات الجملة (شامل ضريبة ${vatNum}%: ${formatSar(formatWithVat(wholesaleNum, vatNum), locale)})`
                    : `Default B2B wholesale price (Incl. ${vatNum}% VAT: ${formatSar(formatWithVat(wholesaleNum, vatNum), locale)})`
                }
              />
              <TextField
                label={isAr ? `سعر التجزئة لكل ${unitLabelAr} (ر.س)` : `Retail Price / ${unitLabelEn} (SAR)`}
                value={retailPrice}
                inputMode="decimal"
                onChange={(e) => setRetailPrice(e.target.value)}
                required
                hint={
                  isAr
                    ? `سعر البيع للمتاجر الصغيرة والتجزئة (شامل ضريبة ${vatNum}%: ${formatSar(formatWithVat(retailNum, vatNum), locale)})`
                    : `Standard retail price (Incl. ${vatNum}% VAT: ${formatSar(formatWithVat(retailNum, vatNum), locale)})`
                }
              />
            </div>
          </div>

          {/* Section 3: Optional Additional Packaging Units */}
          <div className="space-y-3 rounded-xl border border-surface-border bg-surface-canvas p-4">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <div>
                <h4 className="text-sm font-semibold text-text-primary">
                  {isAr ? "3. وحدات تعبئة إضافية (اختياري)" : "3. Additional Packaging Units (Optional)"}
                </h4>
                <p className="text-xs text-text-muted">
                  {isAr
                    ? `أضف كرتون أو علبة أو طبلية مع معامل التحويل من (${unitLabelAr}) وأسعارها الخاصة.`
                    : `Add Box, Carton, or Pallet units with conversion multipliers from ${unitLabelEn}.`}
                </p>
              </div>
              <div className="flex flex-wrap gap-1.5">
                <Button type="button" variant="secondary" size="compact" onClick={() => handleAddPackagingUnit("Box")}>
                  {isAr ? "+ علبة" : "+ Box"}
                </Button>
                <Button type="button" variant="secondary" size="compact" onClick={() => handleAddPackagingUnit("Carton")}>
                  {isAr ? "+ كرتون" : "+ Carton"}
                </Button>
                <Button type="button" variant="secondary" size="compact" onClick={() => handleAddPackagingUnit("Pallet")}>
                  {isAr ? "+ طبلية" : "+ Pallet"}
                </Button>
              </div>
            </div>

            {extraUnits.length > 0 && (
              <div className="space-y-3">
                {extraUnits.map((u, index) => (
                  <div key={u.id} className="space-y-2 rounded-lg border border-surface-border bg-surface-panel p-3">
                    <div className="flex items-center justify-between">
                      <Badge tone="info">
                        {isAr ? `وحدة إضافية #${index + 1}` : `Packaging Unit #${index + 1}`}
                      </Badge>
                      <Button
                        type="button"
                        variant="ghost"
                        size="compact"
                        onClick={() => setExtraUnits((prev) => prev.filter((item) => item.id !== u.id))}
                      >
                        {isAr ? "حذف" : "Remove"}
                      </Button>
                    </div>
                    <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
                      <TextField
                        label={isAr ? "اسم الوحدة (عربي)" : "Unit Name (AR)"}
                        value={u.unitNameAr}
                        onChange={(e) => handleUpdateExtraUnit(u.id, "unitNameAr", e.target.value)}
                        required
                      />
                      <TextField
                        label={isAr ? "اسم الوحدة (إنجليزي)" : "Unit Name (EN)"}
                        value={u.unitNameEn}
                        onChange={(e) => handleUpdateExtraUnit(u.id, "unitNameEn", e.target.value)}
                        required
                      />
                      <TextField
                        label={isAr ? `معامل التحويل (كم ${unitLabelAr})` : `Conversion (${unitLabelEn}s per unit)`}
                        value={u.conversionFactor}
                        inputMode="decimal"
                        onChange={(e) => handleUpdateExtraUnit(u.id, "conversionFactor", e.target.value)}
                        required
                      />
                      <TextField
                        label={isAr ? "باركود الوحدة" : "Unit Barcode"}
                        value={u.barcode}
                        onChange={(e) => handleUpdateExtraUnit(u.id, "barcode", e.target.value)}
                        required
                      />
                      <TextField
                        label={isAr ? "الحد الأدنى للوحدة" : "Unit Min Floor"}
                        value={u.minAllowedPrice}
                        inputMode="decimal"
                        onChange={(e) => handleUpdateExtraUnit(u.id, "minAllowedPrice", e.target.value)}
                        required
                      />
                      <TextField
                        label={isAr ? "سعر جملة الوحدة" : "Unit Wholesale"}
                        value={u.wholesalePrice}
                        inputMode="decimal"
                        onChange={(e) => handleUpdateExtraUnit(u.id, "wholesalePrice", e.target.value)}
                        required
                      />
                      <TextField
                        label={isAr ? "سعر تجزئة الوحدة" : "Unit Retail"}
                        value={u.retailPrice}
                        inputMode="decimal"
                        onChange={(e) => handleUpdateExtraUnit(u.id, "retailPrice", e.target.value)}
                        required
                      />
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Section 4: Multi-Warehouse Initial Stock Allocation */}
          <div className="space-y-3 rounded-xl border border-surface-border bg-surface-canvas p-4">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <div>
                <h4 className="text-sm font-semibold text-text-primary">
                  {isAr ? "4. توزيع المنتج والرصيد الافتتاحي على المستودعات" : "4. Multi-Warehouse Initial Stock Allocation"}
                </h4>
                <p className="text-xs text-text-muted">
                  {isAr
                    ? `اختر كل المستودعات أو بعضها وحدد كمية كل مستودع بـ(${unitLabelAr}) — مثال: 100 في المستودع الرئيسي و 40 في مستودع الفرع.`
                    : `Select all or specific warehouses and split opening stock in ${unitLabelEn}s across them.`}
                </p>
              </div>
              <div className="flex flex-wrap gap-1.5">
                <Button type="button" variant="secondary" size="compact" onClick={handleSelectAllWarehouses}>
                  {isAr ? "تحديد جميع المستودعات" : "Select All Warehouses"}
                </Button>
                <Button type="button" variant="ghost" size="compact" onClick={handleClearAllWarehouses}>
                  {isAr ? "إلغاء التحديد" : "Clear"}
                </Button>
              </div>
            </div>

            <div className="flex flex-wrap items-center gap-2">
              <Badge tone="gold" mono>
                {isAr
                  ? `المستودعات المختارة: ${allocationSummary.selectedCount} / ${activeWarehouses.length}`
                  : `Selected Warehouses: ${allocationSummary.selectedCount} / ${activeWarehouses.length}`}
              </Badge>
              <Badge tone="success" mono>
                {isAr
                  ? `إجمالي الرصيد الافتتاحي: ${formatNumber(allocationSummary.totalInitialQty, locale, 2)} ${unitLabelAr}`
                  : `Total Opening Stock: ${formatNumber(allocationSummary.totalInitialQty, locale, 2)} ${unitLabelEn}`}
              </Badge>
            </div>

            {activeWarehouses.length === 0 ? (
              <p className="text-xs text-text-muted">
                {isAr ? "لا توجد مستودعات نشطة متاحة لحسابك." : "No active warehouses available for your account."}
              </p>
            ) : (
              <div className="space-y-2">
                {activeWarehouses.map((wh) => {
                  const alloc = warehouseAllocations[wh.id] ?? { selected: false, initialQty: "0", minReorderQty: "10" };
                  return (
                    <div
                      key={wh.id}
                      className="space-y-2 rounded-lg border border-surface-border bg-surface-panel p-3"
                    >
                      <label className="flex cursor-pointer items-center justify-between gap-2">
                        <span className="flex items-center gap-2">
                          <input
                            type="checkbox"
                            checked={alloc.selected}
                            onChange={(e) =>
                              setWarehouseAllocations((prev) => ({
                                ...prev,
                                [wh.id]: { ...alloc, selected: e.target.checked },
                              }))
                            }
                          />
                          <span className="font-mono text-xs font-semibold text-text-primary">{wh.warehouseCode}</span>
                          <span className="text-sm text-text-primary">
                            {isAr ? wh.nameAr : (wh.nameEn ?? wh.nameAr)}
                          </span>
                        </span>
                        <Badge tone="neutral">{wh.warehouseType}</Badge>
                      </label>

                      {alloc.selected && (
                        <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
                          <TextField
                            label={isAr ? `الكمية الافتتاحية (${unitLabelAr})` : `Opening Qty (${unitLabelEn})`}
                            value={alloc.initialQty}
                            inputMode="decimal"
                            onChange={(e) =>
                              setWarehouseAllocations((prev) => ({
                                ...prev,
                                [wh.id]: { ...alloc, initialQty: e.target.value },
                              }))
                            }
                          />
                          <TextField
                            label={isAr ? `حد إعادة الطلب (${unitLabelAr})` : `Min Reorder (${unitLabelEn})`}
                            value={alloc.minReorderQty}
                            inputMode="decimal"
                            onChange={(e) =>
                              setWarehouseAllocations((prev) => ({
                                ...prev,
                                [wh.id]: { ...alloc, minReorderQty: e.target.value },
                              }))
                            }
                          />
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        </form>
      </DrawerModal>

      {/* Multi-Warehouse Product Availability Breakdown Drawer */}
      <DrawerModal
        open={availabilityOpen}
        onClose={() => setAvailabilityOpen(false)}
        title={
          availabilityData
            ? `${availabilityData.sku} — ${isAr ? availabilityData.nameAr : (availabilityData.nameEn ?? availabilityData.nameAr)}`
            : isAr
              ? "تفصيل المخزون عبر المستودعات"
              : "Cross-Warehouse Stock Breakdown"
        }
        subtitle={
          isAr
            ? "الكمية الكلية للمنتج عبر جميع المستودعات وتفصيل كل مستودع بدقة"
            : "Aggregate stock across all warehouses plus exact per-warehouse breakdown"
        }
      >
        {availabilityLoading && (
          <p className="py-8 text-center text-sm text-text-muted">
            {isAr ? "جاري تحميل أرصدة المستودعات..." : "Loading warehouse availability..."}
          </p>
        )}

        {!availabilityLoading && availabilityData && (
          <div className="space-y-5">
            <KeyValueList
              items={[
                {
                  id: "baseUnit",
                  label: isAr ? "وحدة الأساس" : "Base Unit",
                  value: (
                    <Badge tone="gold">
                      {isAr
                        ? `${getBaseUnitMeta(availabilityData.baseUnit).ar} (${getBaseUnitMeta(availabilityData.baseUnit).en})`
                        : `${getBaseUnitMeta(availabilityData.baseUnit).en} (${getBaseUnitMeta(availabilityData.baseUnit).ar})`}
                    </Badge>
                  ),
                },
                {
                  id: "totalAvailable",
                  label: isAr ? "إجمالي المتاح (كل المستودعات)" : "Total Available (All Warehouses)",
                  value: (
                    <Badge tone={availabilityData.totalAvailableQty > 0 ? "success" : "danger"} mono>
                      {formatNumber(availabilityData.totalAvailableQty, locale, 2)}
                    </Badge>
                  ),
                },
                {
                  id: "totalPhysical",
                  label: isAr ? "إجمالي الرصيد الفعلي" : "Total Physical Qty",
                  value: formatNumber(availabilityData.totalPhysicalQty, locale, 2),
                  mono: true,
                },
                {
                  id: "totalReserved",
                  label: isAr ? "إجمالي المحجوز للطلبات" : "Total Reserved Qty",
                  value: formatNumber(availabilityData.totalReservedQty, locale, 2),
                  mono: true,
                },
                {
                  id: "totalIncoming",
                  label: isAr ? "إجمالي الوارد (قيد النقل/الشراء)" : "Total Incoming Qty",
                  value: formatNumber(availabilityData.totalIncomingQty, locale, 2),
                  mono: true,
                },
                {
                  id: "pricing",
                  label: isAr ? "أسعار الوحدة الأساسية (أدنى / جملة / تجزئة)" : "Base Unit Prices (Min / Wholesale / Retail)",
                  value: `${formatSar(availabilityData.minAllowedSellingPrice, locale)} / ${formatSar(availabilityData.wholesaleSellingPrice, locale)} / ${formatSar(availabilityData.retailSellingPrice, locale)}`,
                  mono: true,
                },
              ]}
            />

            <div className="space-y-2">
              <h4 className="text-sm font-semibold text-text-primary">
                {isAr ? "تفصيل الرصيد في كل مستودع" : "Exact Stock per Warehouse"}
              </h4>
              <DataTable
                rows={availabilityData.warehouseStocks}
                rowKey={(w) => String(w.warehouseId)}
                columns={[
                  {
                    key: "wh",
                    header: isAr ? "المستودع" : "Warehouse",
                    render: (w) => (
                      <div>
                        <p className="font-mono text-xs font-semibold text-text-primary">{w.warehouseCode}</p>
                        <p className="text-xs text-text-muted">{isAr ? w.warehouseNameAr : (w.warehouseNameEn ?? w.warehouseNameAr)}</p>
                      </div>
                    ),
                  },
                  { key: "phys", header: isAr ? "فعلي" : "Physical", align: "end", numeric: true, render: (w) => formatNumber(w.physicalQty, locale, 2) },
                  { key: "res", header: isAr ? "محجوز" : "Reserved", align: "end", numeric: true, render: (w) => formatNumber(w.reservedQty, locale, 2) },
                  { key: "inc", header: isAr ? "وارد" : "Incoming", align: "end", numeric: true, render: (w) => formatNumber(w.incomingQty, locale, 2) },
                  {
                    key: "avail",
                    header: isAr ? "المتاح" : "Available",
                    align: "end",
                    numeric: true,
                    render: (w) => (
                      <Badge tone={w.availableQty <= w.minReorderQty ? "warning" : "success"} mono>
                        {formatNumber(w.availableQty, locale, 2)}
                      </Badge>
                    ),
                  },
                ]}
              />
            </div>

            {availabilityData.units.length > 0 && (
              <div className="space-y-2">
                <h4 className="text-sm font-semibold text-text-primary">
                  {isAr ? "وحدات التعبئة والأسعار" : "Packaging Units & Pricing"}
                </h4>
                <DataTable
                  rows={availabilityData.units}
                  rowKey={(u) => u.id}
                  columns={[
                    {
                      key: "unit",
                      header: isAr ? "الوحدة" : "Unit",
                      render: (u) => (
                        <div className="flex items-center gap-1.5">
                          <span>{isAr ? u.unitNameAr : u.unitNameEn}</span>
                          {u.isDefaultUnit && <Badge tone="gold">{isAr ? "أساسية" : "Base"}</Badge>}
                        </div>
                      ),
                    },
                    { key: "factor", header: isAr ? "المعامل" : "Factor", align: "end", numeric: true, render: (u) => `×${formatNumber(u.conversionFactor, locale, 2)}` },
                    { key: "min", header: isAr ? "الحد الأدنى" : "Min Floor", align: "end", numeric: true, render: (u) => formatSar(u.minAllowedPrice, locale) },
                    { key: "wh", header: isAr ? "الجملة" : "Wholesale", align: "end", numeric: true, render: (u) => formatSar(u.wholesalePrice, locale) },
                    { key: "rt", header: isAr ? "التجزئة" : "Retail", align: "end", numeric: true, render: (u) => formatSar(u.retailPrice, locale) },
                  ]}
                />
              </div>
            )}
          </div>
        )}
      </DrawerModal>

      <DrawerModal
        open={linkTarget !== null}
        onClose={() => setLinkTarget(null)}
        title={isAr ? "ربط مورد بالمنتج" : "Link Supplier to Product"}
        subtitle={linkTarget?.sku}
        footer={
          <>
            <Button variant="secondary" onClick={() => setLinkTarget(null)}>{isAr ? "إلغاء" : "Cancel"}</Button>
            <Button type="submit" form="link-supplier-form" loading={submitting}>{isAr ? "ربط" : "Link"}</Button>
          </>
        }
      >
        <form id="link-supplier-form" onSubmit={handleLinkSupplier} className="space-y-3">
          <TextField label={isAr ? "معرف المورد (Supplier GUID)" : "Supplier ID (GUID)"} value={linkSupId} onChange={(e) => setLinkSupId(e.target.value)} required />
          <TextField label={isAr ? "رمز المورد للصنف" : "Supplier SKU"} value={linkSupSku} onChange={(e) => setLinkSupSku(e.target.value)} />
          <TextField label={isAr ? "آخر سعر شراء (ر.س)" : "Last Purchase Price (SAR)"} value={linkSupPrice} inputMode="decimal" onChange={(e) => setLinkSupPrice(e.target.value)} required />
        </form>
      </DrawerModal>

      <DrawerModal
        open={plOpen}
        onClose={() => setPlOpen(false)}
        title={isAr ? "إنشاء قائمة أسعار متدرجة" : "Create Price List"}
        footer={
          <>
            <Button variant="secondary" onClick={() => setPlOpen(false)}>{isAr ? "إلغاء" : "Cancel"}</Button>
            <Button type="submit" form="create-pl-form" loading={submitting}>{isAr ? "حفظ" : "Save"}</Button>
          </>
        }
      >
        <form id="create-pl-form" onSubmit={handleCreatePriceList} className="space-y-3">
          <TextField label={isAr ? "رمز القائمة" : "Price List Code"} value={plCode} onChange={(e) => setPlCode(e.target.value)} required placeholder="WHOLESALE-2026" />
          <TextField label={isAr ? "الاسم بالعربية" : "Name (AR)"} value={plNameAr} onChange={(e) => setPlNameAr(e.target.value)} required />
          <TextField label={isAr ? "معرف وحدة المنتج (ProductUnit GUID)" : "Product Unit ID (GUID)"} value={plUnitId} onChange={(e) => setPlUnitId(e.target.value)} />
          <TextField label={isAr ? "سعر الشريحة (ر.س)" : "Tier Unit Price (SAR)"} value={plPrice} inputMode="decimal" onChange={(e) => setPlPrice(e.target.value)} />
        </form>
      </DrawerModal>
    </ErpAppLayout>
  );
}
