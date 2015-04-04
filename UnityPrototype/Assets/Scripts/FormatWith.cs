
using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public static class FormatWithHelper
{
	public static string FormatWith(this string format, IDictionary<string, object> source)
	{
		return FormatWith(format, null, source);
	}

	public static string FormatWith(this string format, IFormatProvider provider, IDictionary<string, object> source)
	{
		if (format == null)
			throw new ArgumentNullException("format");
		
		Regex r = new Regex(@"(?<start>\{)+(?<property>[\w\.\[\]]+)(?<format>:[^}]+)?(?<end>\})+",
		                    RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
		
		List<object> values = new List<object>();
		string rewrittenFormat = r.Replace(format, delegate(Match m)
		                                   {
			Group startGroup = m.Groups["start"];
			Group propertyGroup = m.Groups["property"];
			Group formatGroup = m.Groups["format"];
			Group endGroup = m.Groups["end"];
			
			values.Add(source.ContainsKey(propertyGroup.Value) ? source[propertyGroup.Value] : "");
			
			return new string('{', startGroup.Captures.Count) + (values.Count - 1) + formatGroup.Value
				+ new string('}', endGroup.Captures.Count);
		});
		
		return string.Format(provider, rewrittenFormat, values.ToArray());
	}
}