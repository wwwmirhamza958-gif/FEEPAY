import { PayScreen } from "@/components/payments/PayScreen";

export default async function PayPage({
  params,
}: {
  params: Promise<{ studentId: string }>;
}) {
  const { studentId } = await params;
  return <PayScreen studentId={studentId} />;
}
