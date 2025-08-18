namespace DeathrunManager
{
    internal class CommandParser
    {
        public static bool TryParseBool(string input, out bool result)
        {
            result = false;
            return int.TryParse(input, out int value) && value is 0 or 1 && (result = value == 1) || value == 0;
        }

        public static bool TryParsePositiveFloat(string input, out float result)
        {
            return float.TryParse(input, out result) && result > 0;
        }
    }
}
