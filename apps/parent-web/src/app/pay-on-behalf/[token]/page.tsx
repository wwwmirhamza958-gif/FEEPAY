import { PayOnBehalfScreen } from "@/components/payments/PayOnBehalfScreen";

export default async function PayOnBehalfPage({
  params,
}: {
  params: Promise<{ token: string }>;
}) {
  const { token } = await params;
  return <PayOnBehalfScreen token={token} />;
}
