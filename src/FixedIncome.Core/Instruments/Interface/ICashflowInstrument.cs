using fixed_income_pricing.CashFlows;

namespace fixed_income_pricing.Instruments;

public interface ICashflowInstrument : IInstrument
{
    IEnumerable<Cashflow> GenerateCashflows(DateOnly valuationDate);
}
