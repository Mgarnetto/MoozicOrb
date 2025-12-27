namespace MoozicOrb
{
    public static class Station
    {
        static string[] stations = new string[] { "Station 1", "Station 2" };

        public static string JoinStation(string station)
        {
            return station + " joined.";
        }

    }
}
