namespace Opto.Models;

/// <summary>KF для коррекции СН сетевыми насосами (karat WYRAB2, записи 185–194).</summary>
public static class NetworkPumpCoefficients
{
    /// <summary>ТС-1 … ТС-5 — значения по умолчанию из karat.</summary>
    public static readonly double[] KaratWyrab2Defaults = [240, 360, 240, 240, 360];
}
