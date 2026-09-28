import { SharePayLinkScreen } from "@/components/payments/SharePayLinkScreen";

export default async function SharePayPage({
  params,
}: {
  params: Promise<{ studentId: string }>;
}) {
  const { studentId } = await params;
  return <SharePayLinkScreen studentId={studentId} />;
}
