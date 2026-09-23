using fixed_income_pricing.CashFlows;
using fixed_income_pricing.Indices;

namespace fixed_income_pricing.Instruments.Bank;

public class PostFixedCDB:IInstrument
{
    public string Id { get; }
    public DateOnly IssueDate { get; }
    public DateOnly MaturityDate { get; }

    private readonly double _notional;
    private readonly double _percentualCdi;
    private readonly IIndex _cdiIndex;

    public PostFixedCDB(
        string id,
        DateOnly issueDate,
        DateOnly maturityDate,
        double notional,
        double percentualCdi,
        IIndex cdiIndex)
    {
        if (maturityDate <= issueDate)
            throw new ArgumentException("maturityDate must be greater than issueDate");
        if (notional < 0 )
            throw new ArgumentException("notional must be greater than zero");
        if (percentualCdi <= 0)
            throw new ArgumentException("percentualCdi must be greater than zero");
        
        Id = id;
        IssueDate = issueDate;
        MaturityDate = maturityDate;
        _notional = notional;
        _percentualCdi = percentualCdi;
        _cdiIndex = cdiIndex;
    }

    public IEnumerable<Cashflow> GenerateCashflows(DateOnly valuationDate)
    {
        if (valuationDate >= MaturityDate)
        {
            yield break;
        }

        yield return new Cashflow(MaturityDate, RedemptionValue());
    }

    public double RedemptionValue()
    {
        double cdiFactor = _cdiIndex.AccrualFactor(IssueDate, MaturityDate);
        double acrrualFactor = 1 + (cdiFactor - 1) * _percentualCdi;
        return _notional * acrrualFactor;
    }
    
    public double AccruedValue(DateOnly asOfDate)
    {
        if (asOfDate < IssueDate)
            throw new ArgumentException("asOfDate cannot be before IssueDate");

        DateOnly cutoff = asOfDate > MaturityDate ? MaturityDate : asOfDate;
        double cdiFactor = _cdiIndex.AccrualFactor(IssueDate, cutoff);
        double accrualFactor = 1 + (cdiFactor - 1) * _percentualCdi;
        return _notional * accrualFactor;
    }
    
    public PostFixedCDB WithIndex(IIndex newIndex) => new PostFixedCDB(Id, IssueDate, MaturityDate, _notional, _percentualCdi, newIndex);
}