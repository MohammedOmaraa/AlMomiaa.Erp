import logoAsset from "../../assets/almomiaa-logo.jpg";

export type BrandLogoProps = {
  label: string;
  compact?: boolean;
  className?: string;
};

const logoSrc = typeof logoAsset === "string" ? logoAsset : logoAsset.src;

export function BrandLogo({ label, compact = false, className = "" }: BrandLogoProps) {
  return (
    <div
      className={`flex flex-col items-center justify-center rounded-lg bg-surface-obsidian text-text-inverse ${
        compact ? "min-h-14 px-2 py-2" : "gap-2 px-4 py-3"
      } ${className}`}
    >
      {compact ? (
        <span className="min-w-0 break-words text-center text-xs font-bold leading-tight text-brand-gold">
          {label}
        </span>
      ) : (
        <>
          <img
            src={logoSrc}
            alt=""
            className="h-24 w-full max-w-44 object-contain"
          />
          <span className="text-center text-sm font-bold tracking-wide text-brand-gold">
            {label}
          </span>
        </>
      )}
    </div>
  );
}
