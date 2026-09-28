import { redirect } from "next/navigation";

/**
 * P2 parent mobile web — OTP → Home → Statement.
 * Pay/checkout is P3 (plan §17). No native app.
 */
export default function RootPage() {
  redirect("/login");
}
