using fixed_income_pricing.Instruments;

namespace FixedIncome.Derivatives.Options;

public class IdiOption : IInstrument
{

    public const double PointValue = 1.0;
    public string Id {get;}
    public DateOnly  MaturityDate {get;}
    public double Strike {get;}
    public OptionType Type { get; }

    public IdiOption(string id, DateOnly maturityDate, double strike, OptionType type)
    {
       if(strike<=0)
           throw new ArgumentException("Strike must be greater than zero", nameof(strike)); 
       
       Id = id;
       MaturityDate = maturityDate;
       Strike = strike;
       Type = type;
    }

    public double Payoff(double idiMaturity) => 
        PointValue * (Type == OptionType.call
            ? Math.Max(idiMaturity - Strike,0)
            : Math.Max(Strike - idiMaturity, 0));
}