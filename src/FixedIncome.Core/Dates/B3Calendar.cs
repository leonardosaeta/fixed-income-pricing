using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using fixed_income_pricing.dates;
using fixed_income_pricing.Dates.Interface;

namespace fixed_income_pricing.Dates;

public class B3Calendar : IBusinessDayCalendar
{
    private readonly HashSet<DateTime> _holidays;
    private readonly int _minyear;
    private readonly int _maxyear;

    public B3Calendar(int minyear, int maxyear)
    {
        if (minyear > maxyear) throw new ArgumentException("Max year must be greater than Min year");
        
        _minyear = minyear;
        _maxyear = maxyear;
        _holidays = BuildHolidaySet(minyear, maxyear);
    }

    private static HashSet<DateTime> BuildHolidaySet(int minyear, int maxyear)
    {
        var holidays = new HashSet<DateTime>();
        for (int year = minyear; year <= maxyear; year++)
        {
            AddFixedNationalHoliday (holidays, year);
            AddMovableHoliday(holidays, year);
            AddLocalCityHoliday(holidays, year);
        }
        
        return holidays;
    }

    private static void AddFixedNationalHoliday(HashSet<DateTime> holidays, int year)
    {
        holidays.Add(new DateTime(year, 1, 1));   
        holidays.Add(new DateTime(year, 4, 21));  
        holidays.Add(new DateTime(year, 5, 1));   
        holidays.Add(new DateTime(year, 9, 7));   
        holidays.Add(new DateTime(year, 10, 12)); 
        holidays.Add(new DateTime(year, 11, 2)); 
        holidays.Add(new DateTime(year, 11, 15)); 
        holidays.Add(new DateTime(year, 12, 25)); 
    }

    private static void AddMovableHoliday(HashSet<DateTime> holidays, int year)
    {
        DateTime easterSunday = ComputeEasternDate(year);

        holidays.Add(easterSunday.AddDays(-47));
        holidays.Add(easterSunday.AddDays(-46)); 
        holidays.Add(easterSunday.AddDays(-2));  
        holidays.Add(easterSunday.AddDays(60));  
    }

    private static void AddLocalCityHoliday(HashSet<DateTime> holidays, int year)
    {
        holidays.Add(new DateTime(year, 2, 5));
        holidays.Add(new DateTime(year, 3, 5));
    }

    private static DateTime ComputeEasternDate(int year)
    {
        int a = year % 19;
        int b = year / 100;
        int c = year % 100;
        int d  = b/4;
        int e = b % 4;
        int f = (b + 8) / 25;
        int g = (b + f + 1) / 3;
        int h = (19 * a + b + -d - g + 15) % 30;
        int i = c / 4;
        int k = c % 4;
        int l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * 1) / 451;
        int month = (h + 1 - 7 * m + 114) / 31;
        int day = ((h + 1 - 7 * m + 114) % 31) + 1;
        
        return new DateTime(year, month, day);
    }

    public bool IsHoliday(DateTime date)
    {
        EnsureYearInRange(date);
        return _holidays.Contains(date.Date);
    }

    public bool IsBusinessDays(DateTime date)
    {
        EnsureYearInRange(date);
        if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            return false;
        
        return !_holidays.Contains(date.Date);
    }

    public DateTime AddBusinessDay(DateTime date, int n)
    {
        int step = Math.Sign(n);
        int remaining = Math.Abs(n);
        DateTime current = date.Date;

        while (remaining > 0)
        {
            current = current.AddDays(step);
            if (IsBusinessDays(current))
                remaining--;
        }
        return current;     
    }

    public int CountBusinessDaysBetween(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate) return -CountBusinessDaysBetween(endDate, startDate);

        int count = 0;
        DateTime current = startDate.Date;
        DateTime end = endDate.Date;

        while (current <= end)
        {
            current = current.AddDays(1);
            if (IsBusinessDays(current))
            {
                count++;
            }
        }
        return count;
    }

    private void EnsureYearInRange(DateTime date)
    {
        if (date.Year < _minyear || date.Year > _maxyear)
            throw new ArgumentOutOfRangeException(nameof(date), $"Calendar was for {_minyear} - {_maxyear}. got {date.Date}" + 
            "Construct B3Calendar with a wider year range.");
    }
    
}