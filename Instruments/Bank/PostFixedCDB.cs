using fixed_income_pricing.CashFlows;
using fixed_income_pricing.Indices;

namespace fixed_income_pricing.Instruments.Bank;

public class PostFixedCDB:IInstrument
{
    public string Id { get; }
    public DateTime IssueDate { get; }
    public DateTime MaturityDate { get; }

    private readonly double _notional;
    private readonly double _percentualCdi;
    private readonly IIndex _cdiIndex;

    public PostFixedCDB(
        string id,
        DateTime issueDate,
        DateTime maturityDate,
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

    public IEnumerable<Cashflow> GenerateCashflows(DateTime valuationDate)
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
}