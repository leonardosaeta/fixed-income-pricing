using System;
using System.Collections.Generic;
using System.Linq;
using fixed_income_pricing.Dates;
using fixed_income_pricing.Dates.Interface;
using fixed_income_pricing.Instruments.Government;

namespace fixed_income_pricing.Curvers.Interface;

public interface ICurveBootstrapper
{
    public record LtnQuote(Ltn Instrument, double MarketPrice);

    public interface ICurveBoostrapper
    {
        IYieldCurve Boostrap(DateOnly referenceDate, IEnumerable<LtnQuote> quotes);
    }


    public class PreFixedCurveBootstrapper : ICurveBootstrapper
    {
        private readonly IBusinessDayCalendar _calendar;
        private readonly IDayCountConvention _dayCountConvention;
        private readonly IInterpolator _interpolator;

        public PreFixedCurveBootstrapper(
            IBusinessDayCalendar calendar,
            IDayCountConvention dayCountConvention,
            IInterpolator interpolator)
        {
            _calendar = calendar;
            _dayCountConvention = dayCountConvention;
            _interpolator = interpolator;
        }


        public IYieldCurve Bootstrap(DateOnly referenceDate, IEnumerable<LtnQuote> quotes)
        {
            var pillars = new List<(DateOnly date, double zeroRate)>();
            foreach (var quote in quotes)
            {
                double t = _dayCountConvention.YearFraction(referenceDate, quote.Instrument.MaturityDate, _calendar);
                if (t <= 0)
                    throw new ArgumentException(
                        $"Quote maturity {quote.Instrument.MaturityDate:yyyy-MM-dd} is not after the reference date");
                double impliedYield = Math.Pow(quote.Instrument.FaceValue / quote.MarketPrice, 1.0 / t) - 1.0;
                pillars.Add((quote.Instrument.MaturityDate, impliedYield));
            }
            return new DiscountCurve(referenceDate, _calendar, _dayCountConvention,  pillars, _interpolator);
        }
    }
}