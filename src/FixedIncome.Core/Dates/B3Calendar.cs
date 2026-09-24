using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using fixed_income_pricing.dates;
using fixed_income_pricing.Dates.Interface;

namespace fixed_income_pricing.Dates;

public class B3Calendar : IBusinessDayCalendar
{
    private readonly HashSet<DateOnly> _holidays;
    private readonly int _minyear;
    private readonly int _maxyear;

    public B3Calendar(int minyear, int maxyear)
    {
        if (minyear > maxyear) throw new ArgumentException("Max year must be greater than Min year");

        _minyear = minyear;
        _maxyear = maxyear;
        _holidays = BuildHolidaySet(minyear, maxyear);
    }

    private static HashSet<DateOnly> BuildHolidaySet(int minyear, int maxyear)
    {
        var holidays = new HashSet<DateOnly>();
        for (int year = minyear; year <= maxyear; year++)
        {
            AddFixedNationalHoliday (holidays, year);
            AddMovableHoliday(holidays, year);
        }

        return holidays;
    }

    private static void AddFixedNationalHoliday(HashSet<DateOnly> holidays, int year)
    {
        holidays.Add(new DateOnly(year, 1, 1));
        holidays.Add(new DateOnly(year, 4, 21));
        holidays.Add(new DateOnly(year, 5, 1));
        holidays.Add(new DateOnly(year, 9, 7));
        holidays.Add(new DateOnly(year, 10, 12));
        holidays.Add(new DateOnly(year, 11, 2));
        holidays.Add(new DateOnly(year, 11, 15));
        if (year >= 2024) holidays.Add(new DateOnly(year, 11, 20)); 
        holidays.Add(new DateOnly(year, 12, 25));
    }

    private static void AddMovableHoliday(HashSet<DateOnly> holidays, int year)
    {
        DateOnly easterSunday = ComputeEasternDate(year);

        holidays.Add(easterSunday.AddDays(-48));
        holidays.Add(easterSunday.AddDays(-47));
        holidays.Add(easterSunday.AddDays(-2));
        holidays.Add(easterSunday.AddDays(60));
    }

    private static DateOnly ComputeEasternDate(int year)
    {
        int a = year % 19;
        int b = year / 100;
        int c = year % 100;
        int d  = b/4;
        int e = b % 4;
        int f = (b + 8) / 25;
        int g = (b - f + 1) / 3;
        int h = (19 * a + b + -d - g + 15) % 30;
        int i = c / 4;
        int k = c % 4;
        int l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int month = (h + l - 7 * m + 114) / 31;
        int day = ((h + l - 7 * m + 114) % 31) + 1;

        return new DateOnly(year, month, day);
    }

    public bool IsHoliday(DateOnly date)
    {
        EnsureYearInRange(date);
        return _holidays.Contains(date);
    }

    public bool IsBusinessDays(DateOnly date)
    {
        EnsureYearInRange(date);
        if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            return false;

        return !_holidays.Contains(date);
    }

    public DateOnly AddBusinessDay(DateOnly date, int n)
    {
        int step = Math.Sign(n);
        int remaining = Math.Abs(n);
        DateOnly current = date;

        while (remaining > 0)
        {
            current = current.AddDays(step);
            if (IsBusinessDays(current))
                remaining--;
        }
        return current;
    }

    public int CountBusinessDaysBetween(DateOnly startDate, DateOnly endDate)
    {
        if (startDate > endDate) return -CountBusinessDaysBetween(endDate, startDate);

        int count = 0;
        DateOnly current = startDate;
        DateOnly end = endDate;

        while (current < end)
        {
            current = current.AddDays(1);
            if (IsBusinessDays(current))
            {
                count++;
            }
        }
        return count;
    }

    private void EnsureYearInRange(DateOnly date)
    {
        if (date.Year < _minyear || date.Year > _maxyear)
            throw new ArgumentOutOfRangeException(nameof(date), $"Calendar was for {_minyear} - {_maxyear}. got {date}" +
            "Construct B3Calendar with a wider year range.");
    }

}
