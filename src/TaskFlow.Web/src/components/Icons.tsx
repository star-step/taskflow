import type { SVGProps } from 'react';

type P = SVGProps<SVGSVGElement>;
const base = (size: number) => ({ width: size, height: size, viewBox: `0 0 ${size} ${size}`, 'aria-hidden': true as const, focusable: false as const });

export const LogoIcon = (p: P) => (
  <svg {...base(16)} {...p}><path d="M3 8.5l3 3 7-7" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round" /></svg>
);
export const SearchIcon = (p: P) => (
  <svg {...base(16)} {...p}><circle cx="7" cy="7" r="4.75" fill="none" stroke="currentColor" strokeWidth="1.5" /><path d="M10.5 10.5L14 14" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" /></svg>
);
export const PlusIcon = (p: P) => (
  <svg {...base(16)} {...p}><path d="M8 3v10M3 8h10" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" /></svg>
);
export const ListIcon = (p: P) => (
  <svg {...base(16)} {...p}><path d="M2.5 4h11M2.5 8h11M2.5 12h11" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" /></svg>
);
export const BoardIcon = (p: P) => (
  <svg {...base(16)} {...p}><g fill="none" stroke="currentColor" strokeWidth="1.4"><rect x="2" y="2.5" width="3.3" height="11" rx="1" /><rect x="6.35" y="2.5" width="3.3" height="7" rx="1" /><rect x="10.7" y="2.5" width="3.3" height="9" rx="1" /></g></svg>
);
export const ChevronIcon = (p: P) => (
  <svg {...base(12)} {...p}><path d="M3 4.5l3 3 3-3" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" /></svg>
);
export const MoreIcon = (p: P) => (
  <svg {...base(16)} {...p}><g fill="currentColor"><circle cx="3.5" cy="8" r="1.3" /><circle cx="8" cy="8" r="1.3" /><circle cx="12.5" cy="8" r="1.3" /></g></svg>
);
export const CloseIcon = (p: P) => (
  <svg {...base(16)} {...p}><path d="M4 4l8 8M12 4l-8 8" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" /></svg>
);
export const FilterIcon = (p: P) => (
  <svg {...base(16)} {...p}><path d="M2.5 4h11M4.5 8h7M6.5 12h3" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" /></svg>
);
export const TrashIcon = (p: P) => (
  <svg {...base(16)} {...p}><path d="M3 4.5h10M6.5 4.5V3h3v1.5M4.5 4.5l.6 8.2a1 1 0 001 .8h3.8a1 1 0 001-.8l.6-8.2" fill="none" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" strokeLinejoin="round" /></svg>
);
export const EditIcon = (p: P) => (
  <svg {...base(16)} {...p}><path d="M10.5 3.5l2 2L6 12H4v-2l6.5-6.5z" fill="none" stroke="currentColor" strokeWidth="1.4" strokeLinejoin="round" /></svg>
);
export const AlertIcon = (p: P) => (
  <svg {...base(16)} {...p}><circle cx="8" cy="8" r="6.5" fill="currentColor" /><path d="M8 4.5v4.2" stroke="#fff" strokeWidth="1.6" strokeLinecap="round" /><circle cx="8" cy="11.2" r=".95" fill="#fff" /></svg>
);
export const CheckIcon = (p: P) => (
  <svg {...base(14)} {...p}><path d="M3 7.5l2.6 2.6L11 4.5" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" /></svg>
);
export const GripIcon = (p: P) => (
  <svg width="12" height="16" viewBox="0 0 12 16" aria-hidden focusable={false} {...p}><g fill="currentColor"><circle cx="4" cy="4" r="1.1" /><circle cx="8" cy="4" r="1.1" /><circle cx="4" cy="8" r="1.1" /><circle cx="8" cy="8" r="1.1" /><circle cx="4" cy="12" r="1.1" /><circle cx="8" cy="12" r="1.1" /></g></svg>
);
export const EyeIcon = ({ off, ...p }: P & { off?: boolean }) => (
  <svg {...base(16)} {...p}>
    <path d="M1.5 8S4 3.5 8 3.5 14.5 8 14.5 8 12 12.5 8 12.5 1.5 8 1.5 8z" fill="none" stroke="currentColor" strokeWidth="1.4" />
    <circle cx="8" cy="8" r="2" fill="none" stroke="currentColor" strokeWidth="1.4" />
    {off && <path d="M2.5 13.5l11-11" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" />}
  </svg>
);
export const EmptyIcon = (p: P) => (
  <svg {...base(48)} {...p}><rect x="8" y="10" width="32" height="28" rx="5" fill="none" stroke="currentColor" strokeWidth="2" /><path d="M8 18h32" stroke="currentColor" strokeWidth="2" /><circle cx="30" cy="30" r="6" fill="none" stroke="currentColor" strokeWidth="2" /><path d="M34.5 34.5L39 39" stroke="currentColor" strokeWidth="2" strokeLinecap="round" /></svg>
);
