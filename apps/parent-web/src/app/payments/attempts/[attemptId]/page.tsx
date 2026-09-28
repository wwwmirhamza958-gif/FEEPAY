import { PaymentStatusScreen } from "@/components/payments/PaymentStatusScreen";

export default async function PaymentAttemptPage({
  params,
}: {
  params: Promise<{ attemptId: string }>;
}) {
  const { attemptId } = await params;
  return <PaymentStatusScreen attemptId={attemptId} />;
}
