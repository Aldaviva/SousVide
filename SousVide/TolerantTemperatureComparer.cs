using UnitsNet;

namespace SousVide;

internal class TolerantTemperatureComparer(Temperature tolerance): IEqualityComparer<Temperature> {

    public bool Equals(Temperature x, Temperature y) => x.Equals(y, tolerance);

    public int GetHashCode(Temperature obj) => obj.GetHashCode();

}