using System;
using System.Globalization;

public static class NumFormat
{
	public static string ToM1Decimal(double value, bool useCommasBelowMillion = true)
	{
		if (Math.Abs(value) >= 1000000.0)
		{
			return (value / 1000000.0).ToString("0.0", CultureInfo.InvariantCulture) + "百万";
		}
		if (!useCommasBelowMillion)
		{
			return ((long)Math.Round(value)).ToString(CultureInfo.InvariantCulture);
		}
		return value.ToString("N0", CultureInfo.InvariantCulture);
	}
}
