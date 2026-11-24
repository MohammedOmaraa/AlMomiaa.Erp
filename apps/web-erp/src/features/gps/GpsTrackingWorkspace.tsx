"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import type { LiveRepGpsDto, RepGpsHistoryPointDto } from "@almomiaa/api-client";
import {
  AlertBanner,
  Badge,
  Button,
  DataTable,
  EmptyState,
  KeyValueList,
  SurfaceCard,
  type DataTableColumn,
} from "@almomiaa/ui";
import { ErpAppLayout } from "../../components/ErpAppLayout";
import { erpApi, formatApiError } from "../../lib/api-client";
import { formatDateTime } from "../../lib/formatters";
import { useLocale } from "../../providers/LocaleProvider";

const GPS_ALLOWED_ROLES = ["SUPER_ADMIN", "EXEC_MGR", "SALES_MGR"] as const;
const REFRESH_SECONDS = 10;

interface GoogleMapInstance {
  setCenter: (pos: { lat: number; lng: number }) => void;
  setZoom: (zoom: number) => void;
}

interface GoogleMarkerInstance {
  setMap: (map: GoogleMapInstance | null) => void;
  addListener: (event: string, handler: () => void) => void;
}

interface GoogleMapsApi {
  Map: new (el: HTMLElement, opts: { center: { lat: number; lng: number }; zoom: number }) => GoogleMapInstance;
  Marker: new (opts: {
    position: { lat: number; lng: number };
    map: GoogleMapInstance;
    title?: string;
    label?: string;
  }) => GoogleMarkerInstance;
}

declare global {
  interface Window {
    google?: { maps?: GoogleMapsApi };
  }
}

function HybridGoogleMap({
  locations,
  selectedRep,
  onSelectRep,
  isAr,
}: {
  locations: LiveRepGpsDto[];
  selectedRep: LiveRepGpsDto | null;
  onSelectRep: (rep: LiveRepGpsDto) => void;
  isAr: boolean;
}) {
  const apiKey = (process.env.NEXT_PUBLIC_GOOGLE_MAPS_API_KEY ?? "").trim();
  const mapContainerRef = useRef<HTMLDivElement | null>(null);
  const mapInstanceRef = useRef<GoogleMapInstance | null>(null);
  const markersRef = useRef<GoogleMarkerInstance[]>([]);
  const [mapsApiReady, setMapsApiReady] = useState(false);

  useEffect(() => {
    if (!apiKey) return;
    if (window.google?.maps) {
      setMapsApiReady(true);
      return;
    }

    const scriptId = "almomiaa-google-maps-sdk";
    const existing = document.getElementById(scriptId) as HTMLScriptElement | null;
    if (existing) {
      existing.addEventListener("load", () => setMapsApiReady(true), { once: true });
      return;
    }

    const script = document.createElement("script");
    script.id = scriptId;
    script.src = `https://maps.googleapis.com/maps/api/js?key=${encodeURIComponent(apiKey)}`;
    script.async = true;
    script.defer = true;
    script.onload = () => setMapsApiReady(true);
    document.head.appendChild(script);
  }, [apiKey]);

  useEffect(() => {
    if (!apiKey || !mapsApiReady || !mapContainerRef.current || !window.google?.maps) {
      return;
    }

    const maps = window.google.maps;
    const center = selectedRep
      ? { lat: selectedRep.latitude, lng: selectedRep.longitude }
      : locations[0]
        ? { lat: locations[0].latitude, lng: locations[0].longitude }
        : { lat: 24.7136, lng: 46.6753 }; // Riyadh default

    if (!mapInstanceRef.current) {
      mapInstanceRef.current = new maps.Map(mapContainerRef.current, { center, zoom: 12 });
    } else {
      mapInstanceRef.current.setCenter(center);
    }

    for (const marker of markersRef.current) {
      marker.setMap(null);
    }
    markersRef.current = [];

    for (const rep of locations) {
      const marker = new maps.Marker({
        position: { lat: rep.latitude, lng: rep.longitude },
        map: mapInstanceRef.current,
        title: `${rep.fullNameAr} (${rep.employeeCode})`,
        label: rep.employeeCode.slice(0, 3),
      });
      marker.addListener("click", () => onSelectRep(rep));
      markersRef.current.push(marker);
    }
  }, [apiKey, mapsApiReady, locations, selectedRep, onSelectRep]);

  if (apiKey) {
    return (
      <div className="space-y-2">
        <div
          ref={mapContainerRef}
          className="h-[420px] w-full overflow-hidden rounded-xl border border-surface-border bg-surface-canvas"
        />
        <p className="text-xs text-text-secondary">
          {isAr
            ? "خريطة Google Maps التفاعلية الحية — يتم تحديث العلامات كل 10 ثوانٍ."
            : "Interactive Google Maps JS API — markers refresh every 10 seconds."}
        </p>
      </div>
    );
  }

  const target = selectedRep ?? locations[0] ?? null;
  const lat = target ? target.latitude : 24.7136;
  const lng = target ? target.longitude : 46.6753;
  const embedSrc = `https://maps.google.com/maps?q=${encodeURIComponent(`${lat},${lng}`)}&z=14&output=embed`;
  const externalUrl = `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(`${lat},${lng}`)}`;

  return (
    <div className="space-y-2">
      <div className="overflow-hidden rounded-xl border border-surface-border bg-surface-canvas">
        <iframe
          key={`${lat}-${lng}`}
          title={isAr ? "خريطة Google Maps لموقع المندوب" : "Google Maps Rep Location"}
          src={embedSrc}
          className="h-[420px] w-full border-0"
          loading="lazy"
          referrerPolicy="no-referrer-when-downgrade"
        />
      </div>
      <div className="flex flex-wrap items-center justify-between gap-2 text-xs text-text-secondary">
        <span>
          {isAr
            ? "وضع Google Maps المدمج (يتحدث تلقائيًا كل 10 ثوانٍ — أضف NEXT_PUBLIC_GOOGLE_MAPS_API_KEY لعرض جميع المناديب بعلامات متعددة)."
            : "Zero-key Google Maps embed (auto-refreshes every 10s — configure NEXT_PUBLIC_GOOGLE_MAPS_API_KEY for multi-pin JS API mode)."}
        </span>
        <a
          href={externalUrl}
          target="_blank"
          rel="noopener noreferrer"
          className="font-semibold text-brand-gold underline-offset-4 hover:underline"
        >
          {isAr ? "فتح في خرائط Google ↗" : "Open in Google Maps ↗"}
        </a>
      </div>
    </div>
  );
}

export function GpsTrackingWorkspace() {
  const { locale } = useLocale();
  const isAr = locale === "ar-SA";

  const [locations, setLocations] = useState<LiveRepGpsDto[]>([]);
  const [selectedRepId, setSelectedRepId] = useState<string | null>(null);
  const [history, setHistory] = useState<RepGpsHistoryPointDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [autoRefresh, setAutoRefresh] = useState(true);
  const [countdown, setCountdown] = useState(REFRESH_SECONDS);
  const [lastUpdated, setLastUpdated] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const loadLive = useCallback(async () => {
    setError(null);
    try {
      const list = await erpApi.operations.getLiveGps();
      setLocations(list);
      setLastUpdated(new Date().toISOString());
      setSelectedRepId((prev) => {
        if (prev && list.some((x) => x.repId === prev)) return prev;
        return list[0]?.repId ?? null;
      });
    } catch (err) {
      const f = formatApiError(err);
      setError(f.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadLive();
  }, [loadLive]);

  useEffect(() => {
    if (!autoRefresh) return;
    setCountdown(REFRESH_SECONDS);
    const tick = setInterval(() => {
      setCountdown((prev) => {
        if (prev <= 1) {
          void loadLive();
          return REFRESH_SECONDS;
        }
        return prev - 1;
      });
    }, 1000);
    return () => clearInterval(tick);
  }, [autoRefresh, loadLive]);

  const selectedRep = useMemo(
    () => locations.find((l) => l.repId === selectedRepId) ?? locations[0] ?? null,
    [locations, selectedRepId],
  );

  useEffect(() => {
    if (!selectedRep) {
      setHistory([]);
      return;
    }
    let active = true;
    erpApi.operations
      .getRepGpsHistory(selectedRep.repId, { hours: 24, limit: 25 })
      .then((pts) => {
        if (active) setHistory(pts);
      })
      .catch(() => {
        if (active) setHistory([]);
      });
    return () => {
      active = false;
    };
  }, [selectedRep, lastUpdated]);

  const columns: DataTableColumn<LiveRepGpsDto>[] = [
    { key: "code", header: isAr ? "الرمز" : "Code", mono: true, render: (r) => r.employeeCode },
    {
      key: "name",
      header: isAr ? "مندوب المبيعات" : "Sales Rep",
      render: (r) => (
        <div>
          <div className="font-semibold text-text-primary">{isAr ? r.fullNameAr : (r.fullNameEn ?? r.fullNameAr)}</div>
          <div className="font-mono text-xs text-text-secondary" dir="ltr">{r.phone}</div>
        </div>
      ),
    },
    {
      key: "status",
      header: isAr ? "الحالة" : "Status",
      render: (r) => (
        <Badge tone={r.isOnlineRecent ? "success" : "neutral"} size="sm">
          {r.isOnlineRecent ? (isAr ? "نشط الآن" : "Active") : (isAr ? "آخر موقع" : "Last Known")}
        </Badge>
      ),
    },
    {
      key: "coords",
      header: isAr ? "الإحداثيات" : "Coordinates",
      mono: true,
      render: (r) => `${r.latitude.toFixed(5)}, ${r.longitude.toFixed(5)}`,
    },
    {
      key: "time",
      header: isAr ? "آخر تحديث" : "Last Ping",
      render: (r) => formatDateTime(r.recordedAtUtc, locale),
    },
    {
      key: "act",
      header: isAr ? "خريطة" : "Map",
      render: (r) => (
        <Button
          size="compact"
          variant={selectedRep?.repId === r.repId ? "primary" : "secondary"}
          onClick={() => setSelectedRepId(r.repId)}
        >
          {isAr ? "تتبع" : "Locate"}
        </Button>
      ),
    },
  ];

  return (
    <ErpAppLayout
      title={isAr ? "التتبع المباشر لمناديب المبيعات (Google Maps — كل 10 ثوانٍ)" : "Live Sales Rep GPS Tracking (Google Maps — Every 10s)"}
      subtitle={
        isAr
          ? "متاح حصريًا لأدوار SUPER_ADMIN و EXEC_MGR و SALES_MGR"
          : "Restricted exclusively to SUPER_ADMIN, EXEC_MGR, and SALES_MGR"
      }
      allowedRoles={GPS_ALLOWED_ROLES}
      headerActions={
        <div className="flex flex-wrap items-center gap-2">
          <Badge tone={autoRefresh ? "success" : "neutral"} mono>
            {autoRefresh ? `${countdown}s` : isAr ? "متوقف" : "Paused"}
          </Badge>
          <Button
            size="compact"
            variant="secondary"
            onClick={() => setAutoRefresh((v) => !v)}
          >
            {autoRefresh ? (isAr ? "إيقاف التحديث" : "Pause 10s") : (isAr ? "استئناف 10ث" : "Resume 10s")}
          </Button>
          <Button
            size="compact"
            onClick={() => {
              setCountdown(REFRESH_SECONDS);
              void loadLive();
            }}
            loading={loading}
          >
            {isAr ? "تحديث الآن" : "Refresh Now"}
          </Button>
        </div>
      }
    >
      {error ? <AlertBanner tone="danger" message={error} onDismiss={() => setError(null)} /> : null}

      <div className="grid grid-cols-1 gap-4 xl:grid-cols-3">
        <div className="xl:col-span-2">
          <SurfaceCard
            title={
              selectedRep
                ? `${isAr ? "الموقع المباشر:" : "Live Location:"} ${isAr ? selectedRep.fullNameAr : (selectedRep.fullNameEn ?? selectedRep.fullNameAr)}`
                : isAr
                  ? "خريطة Google Maps الحية"
                  : "Live Google Map"
            }
            description={
              lastUpdated
                ? `${isAr ? "آخر مزامنة:" : "Last synced:"} ${formatDateTime(lastUpdated, locale)}`
                : undefined
            }
          >
            <HybridGoogleMap
              locations={locations}
              selectedRep={selectedRep}
              onSelectRep={(rep) => setSelectedRepId(rep.repId)}
              isAr={isAr}
            />
          </SurfaceCard>
        </div>

        <SurfaceCard title={isAr ? "تفاصيل المندوب المختار" : "Selected Rep Telemetry"}>
          {selectedRep ? (
            <div className="space-y-4">
              <KeyValueList
                columns={1}
                items={[
                  {
                    id: "gps-rep-name",
                    label: isAr ? "المندوب" : "Rep",
                    value: `${isAr ? selectedRep.fullNameAr : (selectedRep.fullNameEn ?? selectedRep.fullNameAr)} (${selectedRep.employeeCode})`,
                  },
                  {
                    id: "gps-coords",
                    label: isAr ? "خط العرض / الطول" : "Lat / Lng",
                    value: (
                      <span className="font-mono" dir="ltr">
                        {selectedRep.latitude.toFixed(6)}, {selectedRep.longitude.toFixed(6)}
                      </span>
                    ),
                  },
                  {
                    id: "gps-acc",
                    label: isAr ? "دقة الإشارة (متر)" : "Accuracy (m)",
                    value: selectedRep.accuracyMeters != null ? `${selectedRep.accuracyMeters} m` : "—",
                  },
                  {
                    id: "gps-time",
                    label: isAr ? "وقت آخر إشارة" : "Recorded At",
                    value: formatDateTime(selectedRep.recordedAtUtc, locale),
                  },
                ]}
              />

              <div className="space-y-1.5">
                <p className="text-xs font-semibold text-text-secondary">
                  {isAr ? "آخر نقاط المسار (24 ساعة)" : "Recent Route Points (24h)"} ({history.length})
                </p>
                {history.length === 0 ? (
                  <p className="text-xs text-text-secondary">—</p>
                ) : (
                  <ul className="max-h-48 space-y-1 overflow-y-auto rounded-lg border border-surface-border bg-surface-canvas p-2 text-xs">
                    {history.slice(0, 10).map((pt) => (
                      <li key={pt.id} className="flex items-center justify-between gap-2">
                        <span className="font-mono text-text-primary" dir="ltr">
                          {pt.latitude.toFixed(5)}, {pt.longitude.toFixed(5)}
                        </span>
                        <span className="tabular-nums text-text-secondary">
                          {formatDateTime(pt.recordedAtUtc, locale)}
                        </span>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            </div>
          ) : (
            <EmptyState
              title={isAr ? "لا توجد إشارات GPS مسجلة بعد" : "No GPS pings recorded yet"}
              description={
                isAr
                  ? "ستظهر مواقع المناديب تلقائيًا هنا كل 10 ثوانٍ بمجرد وصول إشارات /api/v1/gps/pings."
                  : "Rep locations will appear here automatically every 10s once /api/v1/gps/pings receives telemetry."
              }
            />
          )}
        </SurfaceCard>
      </div>

      <SurfaceCard title={isAr ? "قائمة المناديب المتتبعين" : "Tracked Field Representatives"}>
        <DataTable columns={columns} rows={locations} rowKey={(r) => r.repId} loading={loading} />
      </SurfaceCard>
    </ErpAppLayout>
  );
}
