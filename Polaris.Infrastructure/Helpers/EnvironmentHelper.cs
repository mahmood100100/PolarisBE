namespace Polaris.Infrastructure.Helpers
{
    public static class EnvironmentHelper
    {
        public static string GetEnvironmentVariable(string key, string? defaultValue = null)
        {
            var value = Environment.GetEnvironmentVariable(key);

            if (string.IsNullOrEmpty(value) && defaultValue != null)
                return defaultValue;

            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException(key, $"Environment variable '{key}' is not set.");

            value = value.Trim();
            if (value.StartsWith("\"") && value.EndsWith("\""))
                value = value.Substring(1, value.Length - 2);

            return value;
        }

        public static int GetEnvironmentVariableInt(string key, int defaultValue = 0)
        {
            var value = GetEnvironmentVariable(key, defaultValue.ToString());
            return int.TryParse(value, out int result) ? result : defaultValue;
        }

        public static bool GetEnvironmentVariableBool(string key, bool defaultValue = false)
        {
            var value = GetEnvironmentVariable(key, defaultValue.ToString());
            return bool.TryParse(value, out bool result) ? result : defaultValue;
        }
    }
}