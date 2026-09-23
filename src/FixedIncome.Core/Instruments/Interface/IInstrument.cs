using System;
using System.Collections.Generic;
using fixed_income_pricing.CashFlows;

namespace fixed_income_pricing.Instruments;

public interface IInstrument
{
    string Id {get;}
    DateOnly IssueDate {get;}
    DateOnly MaturityDate {get;}
    IEnumerable<Cashflow> GenerateCashflows(DateOnly valuationDate);
}