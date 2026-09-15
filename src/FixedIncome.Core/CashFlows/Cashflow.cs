using System;

namespace fixed_income_pricing.CashFlows
{
public record class Cashflow(DateTime PaymentDate, double Amount);
}