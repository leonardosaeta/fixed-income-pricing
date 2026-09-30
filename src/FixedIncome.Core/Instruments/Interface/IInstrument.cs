namespace fixed_income_pricing.Instruments;

public interface IInstrument
{
    string Id {get;}
    DateOnly MaturityDate {get;}
}
