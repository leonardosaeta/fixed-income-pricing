namespace fixed_income_pricing;

public class Bus252
{
    public double Fator(double taxaAnual, int du)
    {
        double exp = (double) du / 252;
        return Math.Pow((1 + taxaAnual),  exp); 
    }
    
    public double ValorPresente(int valorFuturo, double taxaAnual, int du)
    {
        var f =  Fator(taxaAnual, du);
        return valorFuturo/f;
    }
    
    public void Foward()
    {
        Console.WriteLine("Foward");
    }
}