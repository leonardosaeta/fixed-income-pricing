using fixed_income_pricing.Indices;

namespace fixed_income_pricing.Instruments.Bank;

public class PostFixedCDB:IInstrument
{
    public string Id { get; }
    public DateOnly IssueDate { get; }
    public DateOnly MaturityDate { get; }
    public double Notional { get; }
    public double PercentualCdi { get; }

    public PostFixedCDB(
        string id,
        DateOnly issueDate,
        DateOnly maturityDate,
        double notional,
        double percentualCdi)
    {
        if (maturityDate <= issueDate)
            throw new ArgumentException("maturityDate must be greater than issueDate");
        if (notional <= 0)
            throw new ArgumentException("notional must be greater than zero");
        if (percentualCdi <= 0)
            throw new ArgumentException("percentualCdi must be greater than zero");
        
        Id = id;
        IssueDate = issueDate;
        MaturityDate = maturityDate;
        Notional = notional;
        PercentualCdi = percentualCdi;
    }

    public double RedemptionValue(IDailyRateIndex cdiIndex, AccrualRounding rounding = AccrualRounding.B3) =>
        AccruedValue(MaturityDate, cdiIndex, rounding);

    public double AccruedValue(DateOnly asOfDate, IDailyRateIndex cdiIndex, AccrualRounding rounding = AccrualRounding.B3)
    {
        if (asOfDate < IssueDate)
            throw new ArgumentException("asOfDate cannot be before IssueDate");

        DateOnly cutoff = asOfDate > MaturityDate ? MaturityDate : asOfDate;
        return Notional * DailyRateAccrual.Factor(cdiIndex, IssueDate, cutoff, PercentualCdi, rounding);
    }
}
