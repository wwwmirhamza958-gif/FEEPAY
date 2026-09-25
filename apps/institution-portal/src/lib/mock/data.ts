import type { ApiErrorEnvelope } from "@fee/api-client";
import type {
  BrandingConfig,
  FeeHead,
  FeeScheduleVersion,
  InstalmentPlan,
  PreviewRun,
  Program,
  StudentRow,
} from "../types";

export const MOCK_TENANT_NAME = "Pilot University";

export const mockBranding: BrandingConfig = {
  tenantName: MOCK_TENANT_NAME,
  brandColourTokenNote:
    "Tenant colour is identity only (logo chip / 3 px band). Action colours stay product tokens.",
  brandColourCssVar: "var(--tenant-brand)",
};

export const mockPrograms: Program[] = [
  {
    id: "prog-1",
    code: "BS-CS",
    name: "BS Computer Science",
    intakeLabel: "Fall 2026",
  },
  {
    id: "prog-2",
    code: "BBA",
    name: "Bachelor of Business Administration",
    intakeLabel: "Fall 2026",
  },
];

export const mockFeeHeads: FeeHead[] = [
  {
    id: "fh-1",
    code: "TUITION",
    name: "Tuition",
    taxableFor236I: true,
  },
  {
    id: "fh-2",
    code: "LAB",
    name: "Lab fee",
    taxableFor236I: false,
  },
];

export const mockSchedules: FeeScheduleVersion[] = [
  {
    id: "sch-1",
    name: "Fall 2026 undergraduate",
    version: 2,
    effectiveFrom: "2026-09-01",
    status: "Published",
  },
  {
    id: "sch-2",
    name: "Fall 2026 undergraduate",
    version: 3,
    effectiveFrom: "2026-10-01",
    status: "Draft",
  },
];

export const mockInstalmentPlans: InstalmentPlan[] = [
  {
    id: "plan-1",
    name: "Two equal parts",
    parts: 2,
    note: "50% at issue · 50% mid-term",
  },
  {
    id: "plan-2",
    name: "Three parts",
    parts: 3,
    note: "40% · 30% · 30%",
  },
];

export const mockStudents: StudentRow[] = [
  {
    id: "stu-1",
    studentNumber: "2026-CS-0142",
    fullName: "Hamza Ahmed",
    programCode: "BS-CS",
    intakeLabel: "Fall 2026",
  },
  {
    id: "stu-2",
    studentNumber: "2026-BBA-0088",
    fullName: "Ayesha Khan",
    programCode: "BBA",
    intakeLabel: "Fall 2026",
  },
];

export const mockPreviewRun: PreviewRun = {
  id: "prev-1",
  termLabel: "Fall 2026",
  totalAmount: "4287500.00",
  lineCount: 2,
  outlierCount: 1,
  lines: [
    {
      studentNumber: "2026-CS-0142",
      studentName: "Hamza Ahmed",
      programCode: "BS-CS",
      amount: "142500.00",
      isOutlier: false,
      invoiceStatus: "Draft",
    },
    {
      studentNumber: "2026-BBA-0088",
      studentName: "Ayesha Khan",
      programCode: "BBA",
      amount: "4145000.00",
      isOutlier: true,
      outlierReason: "Amount is more than 3× the program median.",
      invoiceStatus: "Draft",
    },
  ],
};

export const mockApiError: ApiErrorEnvelope = {
  error: {
    code: "BILLING_API_UNAVAILABLE",
    message:
      "Billing APIs are not available yet. Portal is running against local mock data.",
    messageKey: "errors.billing_api_unavailable",
    correlationId: "11111111-2222-4333-8444-555555555555",
    details: [],
    retryable: true,
  },
};

/** Simulate network; resolves after ~400 ms so skeletons show (§5.6). */
export function delay<T>(value: T, ms = 450): Promise<T> {
  return new Promise((resolve) => {
    setTimeout(() => resolve(value), ms);
  });
}
