using System.Runtime.Serialization;

namespace fixed_income_pricing.Portfolio;

public class Portfolio
{
    public string Id { get; }
    private readonly List<Position> _positions = new();

    public Portfolio(string id)
    {
        Id = id;
    }

    public void AddPosition(Position position) => _positions.Add(position);
    public IReadOnlyList<Position> Positions => _positions;

    public PortfolioRiskReport BuildReport(DateOnly valuationDate)
    {
        if (_positions.Count == 0)
            throw new InvalidDataContractException("Portfolio has no position");
        
        double totalPv = _positions.Sum(p => p.PresentValue);
        double totalDv01 = _positions.Sum(p => p.Dv01);

        var withDuration = _positions.Where(p => p.UnitModifiedDuration.HasValue).ToList();
        double? weightedDuration = withDuration.Count == 0 ? null : withDuration.Sum(p=>p.PresentValue*p.UnitModifiedDuration!.Value)/withDuration.Sum(p=>p.PresentValue);
        
        return new PortfolioRiskReport(Id, valuationDate, totalPv, totalDv01, weightedDuration, _positions);

    }
}