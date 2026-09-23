namespace fixed_income_pricing.Indices;

public class IPCAIndex:IIndex
{
   public string Name => "IPCA";
   private readonly IReadOnlyDictionary<DateOnly, double> _monthlyIndexNumber;

   public IPCAIndex(IReadOnlyDictionary<DateOnly, double> monthlyIndexNumber)
   {
      _monthlyIndexNumber = monthlyIndexNumber;
   }

   public double AccrualFactor(DateOnly startDate, DateOnly endDate)
   {
      if (startDate > endDate)
         throw new ArgumentException("startDate connot be <= the endDate");

      return InterpolatedIndexNumber(endDate)/InterpolatedIndexNumber(startDate);
   }

   private double InterpolatedIndexNumber(DateOnly date)
   {
     DateOnly knownReferenceMonth = MostRecentDisclosedMonth(date);
     DateOnly nextReferernceMonth = knownReferenceMonth.AddMonths(1);

     double knownValue = GetIndexNumber(knownReferenceMonth);
     double nextValue = GetIndexNumber(nextReferernceMonth);

     DateOnly disclosureStartDate = new DateOnly(knownReferenceMonth.Year, knownReferenceMonth.Month, 1).AddMonths(1).AddDays(14);
     DateOnly disclosureEndDate = disclosureStartDate.AddMonths(1);

     if (date <= disclosureStartDate)
     {
        return knownValue;
     }

     int elapsedCalendarDays = date.DayNumber - disclosureStartDate.DayNumber;
     int periodCalendarDays = disclosureEndDate.DayNumber - disclosureStartDate.DayNumber;
     double proRataFunciton = (double)elapsedCalendarDays / periodCalendarDays;

     return knownValue * Math.Pow(nextValue/knownValue, proRataFunciton);
   }

   private double GetIndexNumber(DateOnly referernceMonth)
   {
      DateOnly key = new DateOnly(referernceMonth.Year, referernceMonth.Month, 1);

      if(!_monthlyIndexNumber.TryGetValue(key, out double value))
         throw new InvalidOperationException($"IPCA: no index number available for reference month {key:yyyy-M-d dddd}");
      return value;
   }

   private DateOnly MostRecentDisclosedMonth(DateOnly date)
   {
      DateOnly firstOfThisMonth = new DateOnly(date.Year, date.Month, 1);

      return date.Day >= 15
         ? firstOfThisMonth.AddMonths(-1)
         : firstOfThisMonth.AddMonths(-2);
   }
}
