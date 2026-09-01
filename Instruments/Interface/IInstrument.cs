using System;
using System.Collections.Generic;
using fixed_income_pricing.CashFlows;

namespace fixed_income_pricing.Instruments;

public interface IInstrument
{
    string Id {get;}
    DateTime IssueDate {get;}
    DateTime MaturityDate {get;}
    
    IEnumerable<Cashflow> GenerateCashflows(DateTime valuationDate);
    
}