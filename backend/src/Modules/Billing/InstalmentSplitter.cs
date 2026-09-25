using FeePlatform.Modules.FeeEngine.Money;

namespace FeePlatform.Modules.Billing;

/// <summary>Splits an invoice total across instalment plan parts stored in Billing (plan §7.5).</summary>
public static class InstalmentSplitter
{
    public static IReadOnlyList<(int Sequence, string Label, decimal Amount, DateOnly DueDate)> Split(
        decimal totalAmount,
        DateOnly issueDate,
        IReadOnlyList<InstalmentPlanPart> partsOrdered)
    {
        if (partsOrdered.Count == 0)
            throw new InvalidOperationException("Instalment plan has no parts.");

        var shareSum = partsOrdered.Sum(p => p.Share);
        if (shareSum <= 0)
            throw new InvalidOperationException("Instalment plan shares must sum to a positive value.");

        var results = new List<(int Sequence, string Label, decimal Amount, DateOnly DueDate)>();
        decimal allocated = 0;

        for (var i = 0; i < partsOrdered.Count; i++)
        {
            var part = partsOrdered[i];
            decimal amount;
            if (i == partsOrdered.Count - 1)
            {
                amount = MoneyMath.RoundHalfUp(totalAmount - allocated);
            }
            else
            {
                amount = MoneyMath.RoundHalfUp(totalAmount * (part.Share / shareSum));
                allocated += amount;
            }

            results.Add((
                part.Sequence,
                string.IsNullOrWhiteSpace(part.Label) ? $"Instalment {part.Sequence}" : part.Label,
                amount,
                issueDate.AddDays(part.DueOffsetDays)));
        }

        return results;
    }
}
