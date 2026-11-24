"use client";

import { useCallback, useEffect, useState, type FormEvent } from "react";
import type {
  BrandDto,
  CategoryNodeDto,
  CategoryProductPriceDto,
  MasterProductDto,
  PriceListDto,
} from "@almomiaa/api-client";
import {
  AlertBanner,
  Badge,
  Button,
  DataTable,
  DrawerModal,
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

function flattenCategories(nodes: CategoryNodeDto[], depth = 0): Array<{ id: number; labelAr: string; labelEn: string; depth: number }> {
  const out: Array<{ id: number; labelAr: string; labelEn: string; depth: number }> = [];
  for (const n of nodes) {
    out.push({ id: n.id, labelAr: n.nameAr, labelEn: n.nameEn ?? n.nameAr, depth });
    if (n.children.length > 0) {
      out.push(...flattenCategories(n.children, depth + 1));
    }
  }
  return out;
}

export function CatalogWorkspace() {
  const { locale } = useLocale();
  const { user } = useAuth();
  const isAr = locale === "ar-SA";

  const [tab, setTab] = useState("products");
  const [products, setProducts] = useState<MasterProductDto[]>([]);
  const [totalProducts, setTotalProducts] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [categories, setCategories] = useState<CategoryNodeDto[]>([]);
  const [brands, setBrands] = useState<BrandDto[]>([]);
  const [priceLists, setPriceLists] = useState<PriceListDto[]>([]);
  const [catPrices, setCatPrices] = useState<CategoryProductPriceDto[]>([]);
  const [lookupCatId, setLookupCatId] = useState("");
  const [lookupWhId, setLookupWhId] = useState("1");
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [banner, setBanner] = useState<{ tone: "success" | "danger"; title: string; message: string; details?: string[] } | null>(null);

  // Create Product Drawer
  const [prodOpen, setProdOpen] = useState(false);
  const [sku, setSku] = useState("");
  const [barcode, setBarcode] = useState("");
  const [nameAr, setNameAr] = useState("");
  const [nameEn, setNameEn] = useState("");
  const [catId, setCatId] = useState("");
  const [brandId, setBrandId] = useState("");
  const [wac, setWac] = useState("10");
  const [minPrice, setMinPrice] = useState("15");
  const [wholesalePrice, setWholesalePrice] = useState("20");
  const [retailPrice, setRetailPrice] = useState("25");

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
  const flatCats = flattenCategories(categories);

  const loadData = useCallback(async () => {
    setLoading(true);
    try {
      const [prodRes, catRes, brandRes, plRes] = await Promise.all([
        erpApi.catalog.getProducts({ search: search || undefined, page, pageSize: 25 }),
        erpApi.catalog.getCategories(),
        erpApi.catalog.getBrands(),
        erpApi.catalog.getPriceLists(),
      ]);
      setProducts(prodRes.items);
      setTotalProducts(prodRes.totalCount);
      setCategories(catRes);
      setBrands(brandRes);
      setPriceLists(plRes);
      const flat = flattenCategories(catRes);
      if (!catId && flat[0]) setCatId(String(flat[0].id));
      if (!lookupCatId && flat[0]) setLookupCatId(String(flat[0].id));
      if (!brandId && brandRes[0]) setBrandId(String(brandRes[0].id));
      if (!plUnitId && prodRes.items[0]?.units[0]) setPlUnitId(prodRes.items[0].units[0].id);
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setLoading(false);
    }
  }, [brandId, catId, lookupCatId, page, plUnitId, search]);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  const handleCreateProduct = async (e: FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setBanner(null);
    try {
      const created = await erpApi.catalog.createProduct({
        sku,
        barcode,
        nameAr,
        nameEn: nameEn || null,
        categoryId: Number(catId),
        brandId: Number(brandId),
        baseUnit: "Piece",
        weightedAvgCost: Number(wac),
        wholesaleSellingPrice: Number(wholesalePrice),
        retailSellingPrice: Number(retailPrice),
        minAllowedSellingPrice: Number(minPrice),
        vatRate: 15,
        units: [
          {
            unitNameAr: "حبة",
            unitNameEn: "Piece",
            conversionFactor: 1,
            barcode,
            wholesalePrice: Number(wholesalePrice),
            retailPrice: Number(retailPrice),
            minAllowedPrice: Number(minPrice),
            isDefaultUnit: true,
          },
        ],
      });
      setBanner({ tone: "success", title: isAr ? "تم إضافة المنتج" : "Product Created", message: `${created.sku} — ${created.nameAr}` });
      setProdOpen(false);
      setSku("");
      setBarcode("");
      setNameAr("");
      setNameEn("");
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

  const handleLookupCategoryPrices = async () => {
    if (!lookupCatId) return;
    setSubmitting(true);
    try {
      const res = await erpApi.catalog.getCategoryProducts(Number(lookupCatId), {
        warehouseId: lookupWhId ? Number(lookupWhId) : undefined,
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
    { key: "name", header: isAr ? "المنتج" : "Product", render: (p) => (isAr ? p.nameAr : (p.nameEn ?? p.nameAr)) },
    { key: "wac", header: isAr ? "التكلفة (WAC)" : "WAC", align: "end", numeric: true, render: (p) => (p.weightedAvgCost === null ? <Badge tone="neutral">{isAr ? "مخفي" : "Masked"}</Badge> : formatSar(p.weightedAvgCost, locale)) },
    { key: "min", header: isAr ? "الحد الأدنى" : "Min Price", align: "end", numeric: true, render: (p) => formatSar(p.minAllowedSellingPrice, locale) },
    { key: "wh", header: isAr ? "الجملة" : "Wholesale", align: "end", numeric: true, render: (p) => formatSar(p.wholesaleSellingPrice, locale) },
    { key: "rt", header: isAr ? "التجزئة" : "Retail", align: "end", numeric: true, render: (p) => formatSar(p.retailSellingPrice, locale) },
    { key: "units", header: isAr ? "الوحدات" : "Units", numeric: true, render: (p) => p.units.length },
    {
      key: "act",
      header: isAr ? "إجراءات" : "Actions",
      render: (p) =>
        canManage ? (
          <Button variant="secondary" size="compact" onClick={() => setLinkTarget(p)}>
            {isAr ? "ربط مورد" : "Link Supplier"}
          </Button>
        ) : null,
    },
  ];

  return (
    <ErpAppLayout
      title={isAr ? "الكتالوج والمنتجات وقوائم الأسعار" : "Catalog, Multi-Unit Products & Price Lists"}
      subtitle={isAr ? "إدارة التصنيفات والعلامات التجارية والوحدات والتسعير المتدرج (MR-02)" : "Manage categories, brands, multi-unit products, supplier links, and tiered price lists"}
      requiredPermissions={["catalog:view", "catalog:manage"]}
      headerActions={
        <div className="flex flex-wrap gap-2">
          {canManage && <Button size="compact" onClick={() => setProdOpen(true)}>{isAr ? "+ منتج جديد" : "+ New Product"}</Button>}
          {canManage && <Button variant="secondary" size="compact" onClick={() => setPlOpen(true)}>{isAr ? "+ قائمة أسعار" : "+ Price List"}</Button>}
          <Button variant="ghost" size="compact" onClick={() => void loadData()}>{isAr ? "تحديث" : "Refresh"}</Button>
        </div>
      }
    >
      {banner && <AlertBanner tone={banner.tone} title={banner.title} message={banner.message} details={banner.details} onDismiss={() => setBanner(null)} />}

      <Tabs
        activeId={tab}
        onChange={setTab}
        items={[
          { id: "products", label: isAr ? "المنتجات" : "Products", badge: totalProducts },
          { id: "taxonomy", label: isAr ? "التصنيفات والعلامات" : "Categories & Brands", badge: flatCats.length + brands.length },
          { id: "pricelists", label: isAr ? "قوائم الأسعار" : "Price Lists", badge: priceLists.length },
          { id: "lookup", label: isAr ? "استعلام سعر وتوفر" : "Category Price Lookup" },
        ]}
      />

      {tab === "products" && (
        <SurfaceCard title={isAr ? "المنتجات الرئيسية" : "Master Products"}>
          <div className="space-y-4">
            <div className="flex flex-wrap items-end gap-2">
              <div className="w-full sm:w-72">
                <TextField label={isAr ? "بحث بالاسم أو SKU أو الباركود" : "Search SKU / Barcode / Name"} value={search} onChange={(e) => setSearch(e.target.value)} />
              </div>
              <Button variant="secondary" onClick={() => void loadData()}>{isAr ? "بحث" : "Search"}</Button>
            </div>
            <DataTable columns={productColumns} rows={products} rowKey={(p) => p.id} loading={loading} />
            <PaginationBar page={page} pageSize={25} totalCount={totalProducts} onPageChange={setPage} />
          </div>
        </SurfaceCard>
      )}

      {tab === "taxonomy" && (
        <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
          <SurfaceCard title={isAr ? "شجرة التصنيفات" : "Category Tree"}>
            <div className="space-y-4">
              {canManage && (
                <form onSubmit={handleCreateCategory} className="flex flex-wrap items-end gap-2">
                  <div className="flex-1"><TextField label={isAr ? "تصنيف جديد (عربي)" : "Category (AR)"} value={newCatAr} onChange={(e) => setNewCatAr(e.target.value)} required /></div>
                  <div className="flex-1"><TextField label={isAr ? "الاسم (إنجليزي)" : "Category (EN)"} value={newCatEn} onChange={(e) => setNewCatEn(e.target.value)} /></div>
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
                  <div className="flex-1"><TextField label={isAr ? "علامة جديدة (عربي)" : "Brand (AR)"} value={newBrandAr} onChange={(e) => setNewBrandAr(e.target.value)} required /></div>
                  <div className="flex-1"><TextField label={isAr ? "الاسم (إنجليزي)" : "Brand (EN)"} value={newBrandEn} onChange={(e) => setNewBrandEn(e.target.value)} /></div>
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
              { key: "def", header: isAr ? "افتراضي" : "Default", render: (p) => <Badge tone={p.isDefault ? "gold" : "neutral"}>{p.isDefault ? "Default" : "Custom"}</Badge> },
              { key: "items", header: isAr ? "البنود" : "Items", numeric: true, render: (p) => p.items.length },
            ]}
          />
        </SurfaceCard>
      )}

      {tab === "lookup" && (
        <SurfaceCard title={isAr ? "استعلام أسعار التصنيف والتوفر الفوري" : "Category Price & Stock Resolution"}>
          <div className="space-y-4">
            <div className="flex flex-wrap items-end gap-2">
              <div className="w-60">
                <SelectField
                  label={isAr ? "التصنيف" : "Category"}
                  value={lookupCatId}
                  onChange={(e) => setLookupCatId(e.target.value)}
                  options={flatCats.map((c) => ({ value: String(c.id), label: isAr ? c.labelAr : c.labelEn }))}
                />
              </div>
              <div className="w-40">
                <TextField label={isAr ? "المستودع (ID)" : "Warehouse ID"} value={lookupWhId} onChange={(e) => setLookupWhId(e.target.value)} />
              </div>
              <Button loading={submitting} onClick={() => void handleLookupCategoryPrices()}>{isAr ? "استعلام" : "Query"}</Button>
            </div>
            <DataTable
              rows={catPrices}
              rowKey={(r) => r.masterProductId}
              columns={[
                { key: "sku", header: "SKU", mono: true, render: (r) => r.sku },
                { key: "name", header: isAr ? "المنتج" : "Product", render: (r) => (isAr ? r.nameAr : (r.nameEn ?? r.nameAr)) },
                { key: "price", header: isAr ? "السعر المعتمد" : "Resolved Price", align: "end", numeric: true, render: (r) => formatSar(r.resolvedUnitPrice, locale) },
                { key: "min", header: isAr ? "الحد الأدنى" : "Floor", align: "end", numeric: true, render: (r) => formatSar(r.minAllowedPrice, locale) },
                { key: "avail", header: isAr ? "المتاح" : "Available", align: "end", numeric: true, render: (r) => formatNumber(r.availableQty, locale, 2) },
              ]}
            />
          </div>
        </SurfaceCard>
      )}

      <DrawerModal
        open={prodOpen}
        onClose={() => setProdOpen(false)}
        title={isAr ? "إضافة منتج رئيسي جديد" : "Create Master Product"}
        footer={
          <>
            <Button variant="secondary" onClick={() => setProdOpen(false)}>{isAr ? "إلغاء" : "Cancel"}</Button>
            <Button type="submit" form="create-product-form" loading={submitting}>{isAr ? "حفظ" : "Save"}</Button>
          </>
        }
      >
        <form id="create-product-form" onSubmit={handleCreateProduct} className="space-y-3">
          <TextField label="SKU" value={sku} onChange={(e) => setSku(e.target.value)} required placeholder="ALM-PROD-001" />
          <TextField label={isAr ? "الباركود" : "Barcode"} value={barcode} onChange={(e) => setBarcode(e.target.value)} required placeholder="6281000012345" />
          <TextField label={isAr ? "الاسم بالعربية" : "Name (AR)"} value={nameAr} onChange={(e) => setNameAr(e.target.value)} required />
          <TextField label={isAr ? "الاسم بالإنجليزية" : "Name (EN)"} value={nameEn} onChange={(e) => setNameEn(e.target.value)} />
          <SelectField label={isAr ? "التصنيف" : "Category"} value={catId} onChange={(e) => setCatId(e.target.value)} options={flatCats.map((c) => ({ value: String(c.id), label: isAr ? c.labelAr : c.labelEn }))} />
          <SelectField label={isAr ? "العلامة التجارية" : "Brand"} value={brandId} onChange={(e) => setBrandId(e.target.value)} options={brands.map((b) => ({ value: String(b.id), label: isAr ? b.nameAr : (b.nameEn ?? b.nameAr) }))} />
          <div className="grid grid-cols-2 gap-2">
            <TextField label="WAC" value={wac} onChange={(e) => setWac(e.target.value)} required />
            <TextField label={isAr ? "الحد الأدنى" : "Min Price"} value={minPrice} onChange={(e) => setMinPrice(e.target.value)} required />
            <TextField label={isAr ? "سعر الجملة" : "Wholesale"} value={wholesalePrice} onChange={(e) => setWholesalePrice(e.target.value)} required />
            <TextField label={isAr ? "سعر التجزئة" : "Retail"} value={retailPrice} onChange={(e) => setRetailPrice(e.target.value)} required />
          </div>
        </form>
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
          <TextField label={isAr ? "آخر سعر شراء (ر.س)" : "Last Purchase Price (SAR)"} value={linkSupPrice} onChange={(e) => setLinkSupPrice(e.target.value)} required />
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
          <TextField label={isAr ? "سعر الشريحة (ر.س)" : "Tier Unit Price (SAR)"} value={plPrice} onChange={(e) => setPlPrice(e.target.value)} />
        </form>
      </DrawerModal>
    </ErpAppLayout>
  );
}
