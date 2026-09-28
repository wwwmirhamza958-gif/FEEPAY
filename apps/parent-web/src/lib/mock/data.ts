import type { ApiErrorEnvelope } from "@fee/api-client";
import type {
  FamilyHome,
  LinkedStudent,
  ParentProfile,
  StudentStatement,
} from "../types";

export const MOCK_OTP_CODE = "123456";
export const MOCK_LINKED_MOBILE = "3001234567";
export const MOCK_UNLINKED_MOBILE = "3000000000";
export const MOCK_RATE_LIMIT_MOBILE = "3099999999";

export const mockApiError: ApiErrorEnvelope = {
  error: {
    code: "RESOURCE_NOT_FOUND",
    message: "We couldn't load your fees. Check your connection and try again.",
    messageKey: "errors.resource_not_found",
    correlationId: "f0000001-aaaa-bbbb-cccc-dddddddddddd",
    details: [],
    retryable: true,
  },
};

export const mockOtpWrong: ApiErrorEnvelope = {
  error: {
    code: "OTP_INVALID",
    message: "That code isn't right. Check the SMS and try again.",
    messageKey: "errors.otp_invalid",
    correlationId: "f0000002-aaaa-bbbb-cccc-dddddddddddd",
    details: [
      {
        field: "code",
        code: "INVALID",
        message: "The one-time code does not match.",
        messageKey: "errors.otp_invalid",
      },
    ],
    retryable: true,
  },
};

export const mockOtpRateLimited: ApiErrorEnvelope = {
  error: {
    code: "OTP_RATE_LIMITED",
    message: "Too many tries. Try again in 10 minutes.",
    messageKey: "errors.otp_rate_limited",
    correlationId: "f0000003-aaaa-bbbb-cccc-dddddddddddd",
    details: [
      {
        field: null,
        code: "RETRY_AFTER",
        message: "Retry after 600 seconds.",
        messageKey: "errors.retry_after",
      },
    ],
    retryable: true,
  },
};

export const mockNoStudents: ApiErrorEnvelope = {
  error: {
    code: "NO_LINKED_STUDENTS",
    message:
      "This number isn't registered with any institution yet. Check with your university's finance office, or ask your child to send you a link code.",
    messageKey: "errors.no_linked_students",
    correlationId: "f0000004-aaaa-bbbb-cccc-dddddddddddd",
    details: [],
    retryable: false,
  },
};

export const mockParent: ParentProfile = {
  id: "parent-1",
  displayName: "Imran",
  mobileE164: "+923001234567",
};

const hamza: LinkedStudent = {
  id: "stu-hamza",
  fullName: "Hamza Khan",
  programLabel: "BS Computer Science",
  termLabel: "Fall 2026",
  institutionId: "ten-pilot",
  institutionName: "Pilot University",
  tenantBrandCss: "var(--tenant-brand)",
  amountDue: "71250.00",
  dueDate: "2026-10-14",
  invoiceStatus: "Issued",
  isOverdue: false,
  clearanceStatus: "NotCleared",
  clearanceCutoffDate: "2026-10-18",
};

const ayesha: LinkedStudent = {
  id: "stu-ayesha",
  fullName: "Ayesha Khan",
  programLabel: "BBA",
  termLabel: "Fall 2026",
  institutionId: "ten-pilot",
  institutionName: "Pilot University",
  tenantBrandCss: "var(--tenant-brand)",
  amountDue: "0.00",
  dueDate: "2026-09-01",
  invoiceStatus: "Paid",
  isOverdue: false,
  clearanceStatus: "Cleared",
};

const sara: LinkedStudent = {
  id: "stu-sara",
  fullName: "Sara Ahmed",
  programLabel: "BS Electrical Engineering",
  termLabel: "Fall 2026",
  institutionId: "ten-city",
  institutionName: "City College of Technology",
  tenantBrandCss: "var(--accent)",
  amountDue: "45000.00",
  dueDate: "2026-10-20",
  invoiceStatus: "PartiallyPaid",
  isOverdue: false,
  clearanceStatus: "ClearedWithDeferment",
};

export const mockFamilyHome: FamilyHome = {
  parent: mockParent,
  totalDue: "116250.00",
  nextDue: {
    studentId: hamza.id,
    studentName: hamza.fullName,
    dueDate: hamza.dueDate,
    amount: hamza.amountDue,
    institutionName: hamza.institutionName,
  },
  groups: [
    {
      institutionId: "ten-pilot",
      institutionName: "Pilot University",
      tenantBrandCss: "var(--tenant-brand)",
      students: [hamza, ayesha],
    },
    {
      institutionId: "ten-city",
      institutionName: "City College of Technology",
      tenantBrandCss: "var(--accent)",
      students: [sara],
    },
  ],
  clearanceAlerts: [
    {
      studentId: hamza.id,
      studentName: "Hamza",
      amountDue: hamza.amountDue,
      cutoffDate: "2026-10-18",
    },
  ],
  firstVisitLinkedCount: 2,
};

export const mockAllPaidHome: FamilyHome = {
  parent: mockParent,
  totalDue: "0.00",
  nextDue: null,
  groups: [
    {
      institutionId: "ten-pilot",
      institutionName: "Pilot University",
      tenantBrandCss: "var(--tenant-brand)",
      students: [{ ...ayesha }],
    },
  ],
  clearanceAlerts: [],
};

export const mockStatements: Record<string, StudentStatement> = {
  "stu-hamza": {
    studentId: "stu-hamza",
    studentName: "Hamza Khan",
    programLabel: "BS Computer Science",
    termLabel: "Fall 2026",
    institutionName: "Pilot University",
    tenantBrandCss: "var(--tenant-brand)",
    amountDue: "71250.00",
    dueDate: "2026-10-14",
    invoiceStatus: "Issued",
    isOverdue: false,
    clearanceStatus: "NotCleared",
    updatedBannerDate: undefined,
    termDiff: {
      totalPercentChange: 6.4,
      lines: [
        { feeHeadName: "Tuition", percentChange: 8.2, direction: "up" },
        { feeHeadName: "Lab fee", percentChange: -2.0, direction: "down" },
      ],
    },
    lines: [
      {
        id: "line-tuition",
        feeHeadName: "Tuition",
        amount: "142500.00",
        kind: "Charge",
        trace: "15 credit hours × Rs 9,500 = Rs 142,500",
      },
      {
        id: "line-lab",
        feeHeadName: "Lab fee",
        amount: "15000.00",
        kind: "Charge",
        trace: "Flat lab fee for Fall 2026",
      },
      {
        id: "line-sib",
        feeHeadName: "Sibling discount",
        amount: "-35625.00",
        kind: "Discount",
        trace: "25% sibling discount on tuition",
      },
      {
        id: "line-paid",
        feeHeadName: "Prior payment applied",
        amount: "-50625.00",
        kind: "Adjustment",
        trace: "Credit from partial payment on 1 Sep",
      },
    ],
    instalments: [
      {
        id: "inst-1",
        sequence: 1,
        dueDate: "2026-10-14",
        amount: "71250.00",
        status: "Issued",
        isOverdue: false,
        payable: true,
      },
      {
        id: "inst-2",
        sequence: 2,
        dueDate: "2026-12-15",
        amount: "50625.00",
        status: "Issued",
        isOverdue: false,
        payable: false,
      },
    ],
    nextPayableAmount: "71250.00",
  },
  "stu-ayesha": {
    studentId: "stu-ayesha",
    studentName: "Ayesha Khan",
    programLabel: "BBA",
    termLabel: "Fall 2026",
    institutionName: "Pilot University",
    tenantBrandCss: "var(--tenant-brand)",
    amountDue: "0.00",
    dueDate: "2026-09-01",
    invoiceStatus: "Paid",
    isOverdue: false,
    clearanceStatus: "Cleared",
    termDiff: {
      totalPercentChange: 0,
      lines: [],
    },
    lines: [
      {
        id: "line-tuition-a",
        feeHeadName: "Tuition",
        amount: "95000.00",
        kind: "Charge",
        trace: "Flat programme fee for Fall 2026",
      },
      {
        id: "line-paid-a",
        feeHeadName: "Payment received",
        amount: "-95000.00",
        kind: "Adjustment",
        trace: "Paid in full on 28 Aug",
      },
    ],
    instalments: [],
    nextPayableAmount: "0.00",
  },
  "stu-sara": {
    studentId: "stu-sara",
    studentName: "Sara Ahmed",
    programLabel: "BS Electrical Engineering",
    termLabel: "Fall 2026",
    institutionName: "City College of Technology",
    tenantBrandCss: "var(--accent)",
    amountDue: "45000.00",
    dueDate: "2026-10-20",
    invoiceStatus: "PartiallyPaid",
    isOverdue: false,
    clearanceStatus: "ClearedWithDeferment",
    updatedBannerDate: "2026-10-02",
    termDiff: {
      totalPercentChange: 3.1,
      lines: [
        { feeHeadName: "Tuition", percentChange: 3.1, direction: "up" },
      ],
    },
    lines: [
      {
        id: "line-tuition-s",
        feeHeadName: "Tuition",
        amount: "90000.00",
        kind: "Charge",
        trace: "12 credit hours × Rs 7,500 = Rs 90,000",
      },
      {
        id: "line-paid-s",
        feeHeadName: "Prior payment applied",
        amount: "-45000.00",
        kind: "Adjustment",
        trace: "First instalment paid on 5 Sep",
      },
    ],
    instalments: [
      {
        id: "inst-s1",
        sequence: 1,
        dueDate: "2026-09-05",
        amount: "45000.00",
        status: "Paid",
        isOverdue: false,
        payable: false,
      },
      {
        id: "inst-s2",
        sequence: 2,
        dueDate: "2026-10-20",
        amount: "45000.00",
        status: "Issued",
        isOverdue: false,
        payable: true,
      },
    ],
    nextPayableAmount: "45000.00",
  },
};

export function delay<T>(value: T, ms = 450): Promise<T> {
  return new Promise((resolve) => {
    setTimeout(() => resolve(value), ms);
  });
}
