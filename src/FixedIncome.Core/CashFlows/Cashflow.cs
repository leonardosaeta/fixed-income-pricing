namespace fixed_income_pricing.CashFlows
{
public record class Cashflow(DateOnly PaymentDate, double Amount);
}