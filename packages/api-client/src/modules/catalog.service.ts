import type { AxiosInstance } from "axios";
import type { PagedResult } from "../common/types.js";

export interface CategoryNodeDto {
  id: number;
  parentCategoryId: number | null;
  nameAr: string;
  nameEn: string | null;
  displayOrder: number;
  isActive: boolean;
  children: CategoryNodeDto[];
}

export interface BrandDto {
  id: number;
  nameAr: string;
  nameEn: string | null;
  isActive: boolean;
}

export interface ProductUnitDto {
  id: string;
  unitNameAr: string;
  unitNameEn: string;
  conversionFactor: number;
  barcode: string;
  wholesalePrice: number;
  retailPrice: number;
  minAllowedPrice: number;
  isDefaultUnit: boolean;
}

export interface MasterProductDto {
  id: string;
  sku: string;
  barcode: string;
  nameAr: string;
  nameEn: string | null;
  categoryId: number;
  brandId: number;
  baseUnit: string;
  weightedAvgCost: number | null;
  wholesaleSellingPrice: number;
  retailSellingPrice: number;
  minAllowedSellingPrice: number;
  vatRate: number;
  isActive: boolean;
  units: ProductUnitDto[];
}

export interface ProductWarehouseStockBreakdownDto {
  warehouseId: number;
  warehouseCode: string;
  warehouseNameAr: string;
  warehouseNameEn: string | null;
  warehouseType: string;
  physicalQty: number;
  reservedQty: number;
  incomingQty: number;
  availableQty: number;
  minReorderQty: number;
}

export interface CategoryProductPriceDto {
  masterProductId: string;
  sku: string;
  nameAr: string;
  nameEn: string | null;
  defaultUnitId: string;
  resolvedUnitPrice: number;
  minAllowedPrice: number;
  availableQty: number;
  baseUnit?: string;
  defaultUnitNameAr?: string;
  defaultUnitNameEn?: string;
  totalPhysicalQty?: number;
  totalAvailableQty?: number;
  warehouseStocks?: ProductWarehouseStockBreakdownDto[];
}

export interface ProductAvailabilityDto {
  masterProductId: string;
  sku: string;
  nameAr: string;
  nameEn: string | null;
  baseUnit: string;
  wholesaleSellingPrice: number;
  retailSellingPrice: number;
  minAllowedSellingPrice: number;
  vatRate: number;
  totalPhysicalQty: number;
  totalReservedQty: number;
  totalIncomingQty: number;
  totalAvailableQty: number;
  units: ProductUnitDto[];
  warehouseStocks: ProductWarehouseStockBreakdownDto[];
}

export interface SupplierProductDto {
  id: string;
  masterProductId: string;
  supplierId: string;
  supplierSku: string | null;
  supplierBarcode: string | null;
  lastPurchasePrice: number;
  leadTimeDays: number | null;
  isPreferredSupplier: boolean;
  updatedAtUtc: string;
}

export interface PriceListItemDto {
  id: string;
  productUnitId: string;
  minQuantity: number;
  tierUnitPrice: number;
}

export interface PriceListDto {
  id: number;
  priceListCode: string;
  nameAr: string;
  nameEn: string | null;
  isDefault: boolean;
  isActive: boolean;
  effectiveFromUtc: string;
  effectiveToUtc: string | null;
  items: PriceListItemDto[];
}

export interface CreateProductUnitRequest {
  unitNameAr: string;
  unitNameEn: string;
  conversionFactor: number;
  barcode: string;
  wholesalePrice: number;
  retailPrice: number;
  minAllowedPrice: number;
  isDefaultUnit: boolean;
}

export interface InitialWarehouseStockRequest {
  warehouseId: number;
  initialQty: number;
  minReorderQty?: number;
}

export interface CreateMasterProductRequest {
  sku: string;
  barcode: string;
  nameAr: string;
  nameEn?: string | null;
  categoryId: number;
  brandId: number;
  baseUnit: string;
  weightedAvgCost: number;
  wholesaleSellingPrice: number;
  retailSellingPrice: number;
  minAllowedSellingPrice: number;
  vatRate: number;
  units: CreateProductUnitRequest[];
  initialWarehouseStocks?: InitialWarehouseStockRequest[];
}

export interface LinkSupplierProductRequest {
  supplierId: string;
  supplierSku?: string | null;
  supplierBarcode?: string | null;
  lastPurchasePrice: number;
  leadTimeDays?: number | null;
  isPreferredSupplier: boolean;
}

export interface CreatePriceListRequest {
  priceListCode: string;
  nameAr: string;
  nameEn?: string | null;
  isDefault: boolean;
  effectiveFromUtc?: string | null;
  effectiveToUtc?: string | null;
  items?: Array<{ productUnitId: string; minQuantity: number; tierUnitPrice: number }>;
}

export function createCatalogService(http: AxiosInstance) {
  return {
    getCategories: async () =>
      (await http.get<CategoryNodeDto[]>("/catalog/categories")).data,
    createCategory: async (request: { parentCategoryId?: number | null; nameAr: string; nameEn?: string | null; displayOrder?: number }) =>
      (await http.post<CategoryNodeDto>("/catalog/categories", request)).data,
    getBrands: async () =>
      (await http.get<BrandDto[]>("/catalog/brands")).data,
    createBrand: async (request: { nameAr: string; nameEn?: string | null }) =>
      (await http.post<BrandDto>("/catalog/brands", request)).data,
    getProducts: async (params?: { search?: string; categoryId?: number; brandId?: number; isActive?: boolean; page?: number; pageSize?: number }) =>
      (await http.get<PagedResult<MasterProductDto>>("/catalog/products", { params })).data,
    getCategoryProducts: async (categoryId: number, params?: { customerId?: string; warehouseId?: number; page?: number; pageSize?: number }) =>
      (await http.get<PagedResult<CategoryProductPriceDto>>(`/catalog/categories/${categoryId}/products`, { params })).data,
    getProductAvailability: async (masterProductId: string) =>
      (await http.get<ProductAvailabilityDto>(`/catalog/products/${masterProductId}/availability`)).data,
    createProduct: async (request: CreateMasterProductRequest) =>
      (await http.post<MasterProductDto>("/catalog/products", request)).data,
    linkSupplierProduct: async (masterProductId: string, request: LinkSupplierProductRequest) =>
      (await http.post<SupplierProductDto>(`/catalog/products/${masterProductId}/suppliers`, request)).data,
    getPriceLists: async () =>
      (await http.get<PriceListDto[]>("/catalog/price-lists")).data,
    createPriceList: async (request: CreatePriceListRequest) =>
      (await http.post<PriceListDto>("/catalog/price-lists", request)).data,
  };
}
