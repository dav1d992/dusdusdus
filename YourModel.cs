public class YourModel
{
    public string Observation { get; set; }
    public PainEvaluation PainEvaluation { get; set; }
    public OxygenSaturation OxygenSaturation { get; set; }
}

public class PainEvaluation
{
    public string ResultCode { get; set; }
    public decimal ResultValue { get; set; }
    public DateTime DateTime { get; set; }
    public string Note { get; set; }
}

public class OxygenSaturation
{
    public decimal ResultValue { get; set; }
    public DateTime DateTime { get; set; }
}
