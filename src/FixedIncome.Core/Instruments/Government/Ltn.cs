using System;
using System.Collections.Generic;
using fixed_income_pricing.CashFlows;

namespace fixed_income_pricing.Instruments.Government;

public class Ltn:IInstrument
{
    public string Id {get;}
    public DateTime IssueDate {get;}
    public DateTime MaturityDate {get;}
    
    public double FaceValue {get;}

    public Ltn(string id, DateTime issueDate, DateTime maturityDate, double faceValue = 1000.0)
    {
        if (maturityDate <= issueDate)
            throw new ArgumentException("maturityDate must be greater than issueDate");
        if (faceValue <= 0)
            throw new ArgumentException("faceValue must be greater than 0");

        Id = id;
        IssueDate = issueDate;
        MaturityDate = maturityDate;
        FaceValue = faceValue;
    }

    public IEnumerable<Cashflow> GenerateCashflows(DateTime valuationDate)
    {
        if (valuationDate >= MaturityDate)
            yield break;

        yield return new Cashflow(MaturityDate, FaceValue);
    }
}