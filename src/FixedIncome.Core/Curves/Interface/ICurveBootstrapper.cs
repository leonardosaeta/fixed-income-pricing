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


        public IYieldCurve Bootstrap(DateOnly referenceDate, IEnumerable<LtnQuote> quotes) =>
            new IterativeCurveBootstrapper(_calendar, _dayCountConvention, _interpolator)
                .Bootstrap(referenceDate, quotes.Select(q => new BootstrapQuote(q.Instrument, q.MarketPrice)));
    }
}