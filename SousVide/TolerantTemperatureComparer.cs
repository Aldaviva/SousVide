using UnitsNet;

namespace SousVide;

internal class TolerantTemperatureComparer(Temperature tolerance): IEqualityComparer<Temperature> {

    public bool Equals(Temperature a, Temperature b) => a.Equals(b, tolerance);

    public int GetHashCode(Temperature obj) => obj.GetHashCode();

}