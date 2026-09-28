import { ReceiptScreen } from "@/components/payments/ReceiptScreen";

export default async function ReceiptPage({
  params,
}: {
  params: Promise<{ receiptId: string }>;
}) {
  const { receiptId } = await params;
  return <ReceiptScreen receiptId={receiptId} />;
}
