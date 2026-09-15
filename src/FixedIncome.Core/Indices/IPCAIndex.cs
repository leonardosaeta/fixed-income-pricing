namespace fixed_income_pricing.Indices;

public class IPCAIndex:IIndex
{
   public string Name => "IPCA";
   private readonly IReadOnlyDictionary<DateTime, double> _monthlyIndexNumber;

   public IPCAIndex(IReadOnlyDictionary<DateTime, double> monthlyIndexNumber)
   {
      _monthlyIndexNumber = monthlyIndexNumber;
   }

   public double AccrualFactor(DateTime startDate, DateTime endDate)
   {
      if (startDate > endDate)
         throw new ArgumentException("startDate connot be <= the endDate");
      
      return InterpolatedIndexNumber(endDate)/InterpolatedIndexNumber(startDate);
   }

   private double InterpolatedIndexNumber(DateTime date)
   {
     DateTime knownReferenceMonth = MostRecentDisclosedMonth(date);
     DateTime nextReferernceMonth = knownReferenceMonth.AddMonths(1);
   
     double knownValue = GetIndexNumber(knownReferenceMonth);
     double nextValue = GetIndexNumber(nextReferernceMonth);
     
     DateTime disclosureStartDate = new DateTime(knownReferenceMonth.Year, knownReferenceMonth.Month, 1).AddMonths(1).AddDays(14);
     DateTime disclosureEndDate = disclosureStartDate.AddMonths(1);

     if (date <= disclosureStartDate)
     {
        return knownValue;
     }
     
     int elapsedCalendarDays = (date.Date - disclosureStartDate).Days;
     int periodCalendarDays = (disclosureEndDate - disclosureStartDate).Days;
     double proRataFunciton = (double)elapsedCalendarDays / periodCalendarDays;
     
     return knownValue * Math.Pow(nextValue/knownValue, proRataFunciton);
   }

   private double GetIndexNumber(DateTime referernceMonth)
   {
      DateTime key = new DateTime(referernceMonth.Year, referernceMonth.Month, 1);
      
      if(!_monthlyIndexNumber.TryGetValue(key, out double value))
         throw new InvalidOperationException($"IPCA: no index number available for reference month {key:yyyy-M-d dddd}");
      return value;
   }

   private DateTime MostRecentDisclosedMonth(DateTime date)
   {
      DateTime firstOfThisMonth = new DateTime(date.Year, date.Month, 1);

      return date.Day >= 15
         ? firstOfThisMonth.AddMonths(-1)
         : firstOfThisMonth.AddMonths(-2);
   }
}