using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace WpfColorHelper;

public static class ColorHelper
{
	private static double Tolerance => 0.000000000000001;


	public static double RationalizeOpacity(int percentage)
	{
		var workingvalue = percentage;

		if (workingvalue < 0)
		{
			workingvalue = 0;
		}
		else if (workingvalue > 100)
		{
			workingvalue = 100;
		}

		return workingvalue / 100f;
	}

	/// <summary>
	/// Create a solid color brush from a hex code or named color value.
	/// </summary>
	/// <param name="color">Color name or hex code</param>
	/// <returns>A SolidColorBrush or null.</returns>
	public static SolidColorBrush GetColorBrush(string color)
	{
		// TODO: Review enabling nullability.
		if (string.IsNullOrWhiteSpace(color))
		{
			return null;
		}

		if (!color?.TrimStart().StartsWith("#") ?? false)
		{
			color = GetHexForNamedColor(color.Trim());
		}

		try
		{
			return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color.Trim()));
		}
		catch
		{
			return null;
		}
	}

	/// <summary>
	/// Create a solid color brush from a hex code or named color value.
	/// </summary>
	/// <param name="color">Color name or hex code</param>
	/// <param name="opacity"></param>
	/// <returns>A SolidColorBrush based on the input. Or a Transparent brush if the input can't be converted.</returns>
	public static SolidColorBrush GetColorBrush(string color, double opacity)
	{
		if (string.IsNullOrWhiteSpace(color))
		{
			return new SolidColorBrush(Colors.Transparent);
		}

		if (!color.TrimStart().StartsWith("#", StringComparison.InvariantCultureIgnoreCase))
		{
			color = GetHexForNamedColor(color.Trim());
		}

		Color parsedColor;

		try
		{
			parsedColor = (Color)ColorConverter.ConvertFromString(color.Trim());
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			// TODO: Review reporting invalid values.
			////Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
			////OutputPane.Instance.Write($"Unable to translate '{color}' into a color.");

			parsedColor = Colors.Transparent;
		}

		// TODO: Add validation (clamping?) for opacity.
		return new SolidColorBrush(parsedColor) { Opacity = opacity };
	}

	/// <summary>
	/// Try and get a color from a string that represents a named color or a HEX code.
	/// </summary>
	public static bool TryGetColor(string colorName, out Color color)
	{
		try
		{
			if (!colorName?.TrimStart().StartsWith("#") ?? false)
			{
				colorName = GetHexForNamedColor(colorName.Trim());
			}

			// If still don't have a hex value, then try to parse it as a known color (SystemColor)
			if (!colorName?.TrimStart().StartsWith("#") ?? false)
			{
				// The System.Windows versions of SystemColors end with "Color" (but the System.Drawing versions don't)
				if (colorName.EndsWith("Color"))
				{
					colorName = colorName.Substring(0, colorName.Length - "Color".Length);
				}

				if (Enum.TryParse(colorName, out System.Drawing.KnownColor knownColor))
				{
					colorName = ColorHelper.ToHex(System.Drawing.Color.FromKnownColor(knownColor));
				}
			}

			// By here, colorName should be a hex value
			color = (Color)ColorConverter.ConvertFromString(colorName.Trim());
			return true;
		}
		catch (System.Exception)
		{
			color = default;
			return false;
		}
	}

	/// <summary>
	/// Get the string representation of the hex value of a color.
	/// </summary>
	public static string ToHex(System.Drawing.Color c)
	{
		// TODO: review adding an RGBA (or ARGB) version
		return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
	}

	public static bool TryGetFromName(string args, out Color color)
	{
		color = default;

		try
		{
			var sdc = ColorHelper.ToHex(System.Drawing.Color.FromName(args));

			return TryGetColor(sdc, out color);
		}
		catch
		{
		}

		return false;
	}

	public static bool TryGetFromInt(string args, out Color color)
	{
		color = default;

		try
		{
			return TryGetColor($"#{int.Parse(args).ToString("X")}", out color);
		}
		catch
		{
		}

		return false;
	}

	public static bool TryGetFromUint(string args, out Color color)
	{
		color = default;

		try
		{
			return TryGetColor($"#{uint.Parse(args).ToString("X")}", out color);
		}
		catch
		{
		}

		return false;
	}

	public static bool TryGetArgbColor(string args, out Color color)
	{
		color = default;

		try
		{
			var parts = args.Split(',');

			var lastPart = parts.Last().Trim();

			if (parts.Length == 2 && lastPart.StartsWith("Color."))
			{
				if (TryGetFromName(lastPart.Substring(6), out Color innerColor))
				{
					color = Color.FromArgb(byte.Parse(parts[0]), innerColor.R, innerColor.G, innerColor.B);
					return true;
				}

				return false;
			}
			else
			{
				if (parts.Length == 1)
				{
					var sdcolor = System.Drawing.Color.FromArgb(int.Parse(parts[0]));
					color = Color.FromArgb(sdcolor.A, sdcolor.R, sdcolor.G, sdcolor.B);
				}
				else if (parts.Length == 3)
				{
					var sdcolor = System.Drawing.Color.FromArgb(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
					color = Color.FromArgb(sdcolor.A, sdcolor.R, sdcolor.G, sdcolor.B);
				}
				else if (parts.Length == 4)
				{
					color = Color.FromArgb(byte.Parse(parts[0]), byte.Parse(parts[1]), byte.Parse(parts[2]), byte.Parse(parts[3]));
				}
				else
				{
					return false;
				}
			}

			return true;
		}
		catch (System.Exception)
		{
			return false;
		}
	}

	public static bool TryGetHsvaColor(string args, out Color color)
	{
		color = default;

		try
		{
			var parts = args.Replace("f", string.Empty).Replace("F", string.Empty).Replace("d", string.Empty).Replace("D", string.Empty).Split(',');

			var lastPart = parts.Last().Trim();

			if (parts.Length == 3)
			{
				if (double.TryParse(parts[0], out double hue) && double.TryParse(parts[1], out double sat) && double.TryParse(parts[2], out double val))
				{
					color = ColorFromHSVA(hue, sat, val, 255);
					return true;
				}

				return false;
			}
			else if (parts.Length == 4)
			{
				if (double.TryParse(parts[0], out double hue) && double.TryParse(parts[1], out double sat) && double.TryParse(parts[2], out double val) && double.TryParse(parts[3], out double alpha))
				{
					color = ColorFromHSVA(hue, sat, val, alpha);
					return true;
				}

				return false;
			}

			return true;
		}
		catch (System.Exception)
		{
			return false;
		}
	}

	public static bool TryGetHslaColor(string args, out Color color)
	{
		color = default;

		try
		{
			var parts = args.Replace("f", string.Empty).Replace("F", string.Empty).Replace("d", string.Empty).Replace("D", string.Empty).Split(',');

			var lastPart = parts.Last().Trim();

			if (parts.Length == 3)
			{
				if (double.TryParse(parts[0], out double hue) && double.TryParse(parts[1], out double sat) && double.TryParse(parts[2], out double lum))
				{
					color = HslaToColor(hue, sat, lum, 255);
					return true;
				}

				return false;
			}
			else if (parts.Length == 4)
			{
				if (double.TryParse(parts[0], out double hue) && double.TryParse(parts[1], out double sat) && double.TryParse(parts[2], out double lum) && double.TryParse(parts[3], out double alpha))
				{
					color = HslaToColor(hue, sat, lum, alpha);
					return true;
				}

				return false;
			}

			return true;
		}
		catch (System.Exception)
		{
			return false;
		}
	}

	public static Color ColorFromHSVA(double hue, double saturation, double value, double alpha = 1.0)
	{
		return ColorFromHSVA(hue, saturation, value, (byte)(alpha * 255));
	}

	public static Color ColorFromHSVA(double hue, double saturation, double value, byte alpha = 255)
	{
		int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
		double f = hue / 60 - Math.Floor(hue / 60);

		value = value * 255;
		var v = Convert.ToByte(value);
		var p = Convert.ToByte(value * (1 - saturation));
		var q = Convert.ToByte(value * (1 - f * saturation));
		var t = Convert.ToByte(value * (1 - (1 - f) * saturation));

		if (hi == 0)
			return Color.FromArgb(alpha, v, t, p);
		else if (hi == 1)
			return Color.FromArgb(alpha, q, v, p);
		else if (hi == 2)
			return Color.FromArgb(alpha, p, v, t);
		else if (hi == 3)
			return Color.FromArgb(alpha, p, q, v);
		else if (hi == 4)
			return Color.FromArgb(alpha, t, p, v);
		else
			return Color.FromArgb(alpha, v, p, q);
	}

	/// <summary>
	/// Converts RGB to HSB. Alpha is ignored.
	/// Output is: { H: [0, 360], S: [0, 1], B: [0, 1] }.
	/// </summary>
	/// <param name="color">The color to convert.</param>
	public static double[] RgBtoHsb(Color color)
	{
		// normalize red, green and blue values
		double r = color.R / 255D;
		double g = color.G / 255D;
		double b = color.B / 255D;

		// conversion start
		double max = System.Math.Max(r, System.Math.Max(g, b));
		double min = System.Math.Min(r, System.Math.Min(g, b));

		double h = 0D;
		if ((System.Math.Abs(max - r) < Tolerance)
				&& (g >= b))
			h = (60D * (g - b)) / (max - min);
		else if ((System.Math.Abs(max - r) < Tolerance)
				&& (g < b))
			h = ((60D * (g - b)) / (max - min)) + 360D;
		else if (System.Math.Abs(max - g) < Tolerance)
			h = ((60D * (b - r)) / (max - min)) + 120D;
		else if (System.Math.Abs(max - b) < Tolerance)
			h = ((60D * (r - g)) / (max - min)) + 240D;

		double s = System.Math.Abs(max) < Tolerance
				? 0D
				: 1D - (min / max);

		return new[]
		{
			Math.Max(0D, Math.Min(360D, h)),
			Math.Max(0D, Math.Min(1D, s)),
			Math.Max(0D, Math.Min(1D, max))
		};
	}

	/// <summary>
	/// Converts RGB to HSL. Alpha is ignored.
	/// Output is: { H: [0, 360], S: [0, 1], L: [0, 1] }.
	/// </summary>
	/// <param name="color">The color to convert.</param>
	public static double[] RgBtoHsl(Color color)
	{
		double h = 0D;
		double s = 0D;
		double l;

		// normalize red, green, blue values
		double r = color.R / 255D;
		double g = color.G / 255D;
		double b = color.B / 255D;

		double max = System.Math.Max(r, System.Math.Max(g, b));
		double min = System.Math.Min(r, System.Math.Min(g, b));

		// hue
		if (System.Math.Abs(max - min) < Tolerance)
			h = 0D; // undefined
		else if ((System.Math.Abs(max - r) < Tolerance)
				&& (g >= b))
			h = (60D * (g - b)) / (max - min);
		else if ((System.Math.Abs(max - r) < Tolerance)
				&& (g < b))
			h = ((60D * (g - b)) / (max - min)) + 360D;
		else if (System.Math.Abs(max - g) < Tolerance)
			h = ((60D * (b - r)) / (max - min)) + 120D;
		else if (System.Math.Abs(max - b) < Tolerance)
			h = ((60D * (r - g)) / (max - min)) + 240D;

		// luminance
		l = (max + min) / 2D;

		// saturation
		if ((System.Math.Abs(l) < Tolerance)
				|| (System.Math.Abs(max - min) < Tolerance))
			s = 0D;
		else if ((0D < l)
				&& (l <= .5D))
			s = (max - min) / (max + min);
		else if (l > .5D)
			s = (max - min) / (2D - (max + min)); //(max-min > 0)?

		return new[]
		{
			System.Math.Max(0D, System.Math.Min(360D, double.Parse($"{h:0.##}"))),
			System.Math.Max(0D, System.Math.Min(1D, double.Parse($"{s:0.##}"))),
			System.Math.Max(0D, System.Math.Min(1D, double.Parse($"{l:0.##}")))
		};
	}

	public static Color HslaToColor(double h, double s, double l, double a = 1.0)
	{
		return HslaToColor(h, s, l, (byte)(a * 255));
	}

	/// <summary>
	/// Converts HSL to RGB, with a specified output Alpha.
	/// Arguments are limited to the defined range:
	/// does not raise exceptions.
	/// </summary>
	/// <param name="h">Hue, must be in [0, 360].</param>
	/// <param name="s">Saturation, must be in [0, 1].</param>
	/// <param name="l">Luminance, must be in [0, 1].</param>
	/// <param name="a">Output Alpha, must be in [0, 255].</param>
	public static Color HslaToColor(double h, double s, double l, byte a = 255)
	{
		h = Math.Max(0D, Math.Min(360D, h));
		s = Math.Max(0D, Math.Min(1D, s));
		l = Math.Max(0D, Math.Min(1D, l));
		a = Math.Max((byte)0, Math.Min((byte)255, a));

		// achromatic argb (gray scale)
		if (Math.Abs(s) < Tolerance)
		{
			return Color.FromArgb(
					a,
					(byte)Math.Max(0, Math.Min(255, Convert.ToInt32(double.Parse($"{l * 255D:0.00}")))),
					(byte)Math.Max(0, Math.Min(255, Convert.ToInt32(double.Parse($"{l * 255D:0.00}")))),
					(byte)Math.Max(0, Math.Min(255, Convert.ToInt32(double.Parse($"{l * 255D:0.00}")))));
		}

		double q = l < .5D
				? l * (1D + s)
				: (l + s) - (l * s);
		double p = (2D * l) - q;

		double hk = h / 360D;
		double[] T = new double[3];
		T[0] = hk + (1D / 3D); // Tr
		T[1] = hk; // Tb
		T[2] = hk - (1D / 3D); // Tg

		for (int i = 0; i < 3; i++)
		{
			if (T[i] < 0D)
				T[i] += 1D;
			if (T[i] > 1D)
				T[i] -= 1D;

			if ((T[i] * 6D) < 1D)
				T[i] = p + ((q - p) * 6D * T[i]);
			else if ((T[i] * 2D) < 1)
				T[i] = q;
			else if ((T[i] * 3D) < 2)
				T[i] = p + ((q - p) * ((2D / 3D) - T[i]) * 6D);
			else
				T[i] = p;
		}

		return Color.FromArgb(
				a,
				(byte)Math.Max(0, Math.Min(255, Convert.ToInt32(double.Parse($"{T[0] * 255D:0.00}")))),
				(byte)Math.Max(0, Math.Min(255, Convert.ToInt32(double.Parse($"{T[1] * 255D:0.00}")))),
				(byte)Math.Max(0, Math.Min(255, Convert.ToInt32(double.Parse($"{T[2] * 255D:0.00}")))));
	}

	public static bool TryGetRgbColor(string args, out Color color)
	{
		color = default;

		try
		{
			var parts = args.Split(',');

			if (parts.Length == 3)
			{
				color = Color.FromRgb(byte.Parse(parts[0]), byte.Parse(parts[1]), byte.Parse(parts[2]));
				return true;
			}
			else
			{
				return false;
			}
		}
		catch (System.Exception)
		{
			return false;
		}
	}

	public static bool TryGetRgbaColor(string args, out Color color)
	{
		color = default;

		try
		{
			var parts = args.Split(',');

			if (parts.Length == 4)
			{
				// Note the order change. The method expectes rgbA, but this is taking Argb
				color = Color.FromArgb(byte.Parse(parts[3]), byte.Parse(parts[0]), byte.Parse(parts[1]), byte.Parse(parts[2]));
				return true;
			}
			else
			{
				return false;
			}
		}
		catch (System.Exception)
		{
			return false;
		}
	}

	public static bool TryGetHexColor(string args, out Color color)
	{
		color = default;

		try
		{
			if (!string.IsNullOrWhiteSpace(args))
			{
				color = (Color)ColorConverter.ConvertFromString(args);
				return true;
			}
			else
			{
				return false;
			}
		}
		catch (System.Exception)
		{
			return false;
		}
	}

	public static string GetHexForNamedColor(string colorName)
	{
		switch (colorName?.ToLowerInvariant().Replace(" ", string.Empty) ?? string.Empty)
		{
			case "aliceblue": return "#F0F8FF";
			case "antiquewhite": return "#FAEBD7";
			case "aqua": return "#00FFFF";
			case "aquamarine": return "#7FFFD4";
			case "azure": return "#F0FFFF";
			case "beige": return "#F5F5DC";
			case "bisque": return "#FFE4C4";
			case "black": return "#000000";
			case "blanchedalmond": return "#FFEBCD";
			case "blue": return "#0000FF";
			case "blueviolet": return "#8A2BE2";
			case "brown": return "#A52A2A";
			case "burgendy": return "#FF6347";
			case "burlywood": return "#DEB887";
			case "cadetblue": return "#5F9EA0";
			case "chartreuse": return "#7FFF00";
			case "chocolate": return "#D2691E";
			case "clear": return "#00000000";
			case "coral": return "#FF7F50";
			case "cornflowerblue": return "#6495ED";
			case "cornsilk": return "#FFF8DC";
			case "crimson": return "#DC143C";
			case "cyan": return "#00FFFF";
			case "darkblue": return "#00008B";
			case "darkcyan": return "#008B8B";
			case "darkgoldenrod": return "#B8860B";
			case "darkgray": return "#A9A9A9";
			case "darkgreen": return "#006400";
			case "darkgrey": return "#A9A9A9";
			case "darkkhaki": return "#BDB76B";
			case "darkmagenta": return "#8B008B";
			case "darkolivegreen": return "#556B2F";
			case "darkorange": return "#FF8C00";
			case "darkorchid": return "#9932CC";
			case "darkred": return "#8B0000";
			case "darksalmon": return "#E9967A";
			case "darkseagreen": return "#8FBC8B";
			case "darkslateblue": return "#483D8B";
			case "darkslategray": return "#2F4F4F";
			case "darkslategrey": return "#2F4F4F";
			case "darkturquoise": return "#00CED1";
			case "darkviolet": return "#9400D3";
			case "darkyellow": return "#D7C32A";
			case "deeppink": return "#FF1493";
			case "deepskyblue": return "#00BFFF";
			case "dimgray": return "#696969";
			case "dimgrey": return "#696969";
			case "dodgerblue": return "#1E90FF";
			case "firebrick": return "#B22222";
			case "floralwhite": return "#FFFAF0";
			case "forestgreen": return "#228B22";
			case "fuchsia": return "#FF00FF";
			case "gainsboro": return "#DCDCDC";
			case "ghostwhite": return "#F8F8FF";
			case "gold": return "#FFD700";
			case "goldenrod": return "#DAA520";
			case "gray": return "#808080";
			case "green": return "#008000";
			case "greenyellow": return "#ADFF2F";
			case "grey": return "#808080";
			case "honeydew": return "#F0FFF0";
			case "hotpink": return "#FF69B4";
			case "indianred": return "#CD5C5C";
			case "indigo": return "#4B0082";
			case "ivory": return "#FFFFF0";
			case "khaki": return "#F0E68C";
			case "lavender": return "#E6E6FA";
			case "lavenderblush": return "#FFF0F5";
			case "lawngreen": return "#7CFC00";
			case "lemonchiffon": return "#FFFACD";
			case "lightblue": return "#ADD8E6";
			case "lightcoral": return "#F08080";
			case "lightcyan": return "#E0FFFF";
			case "lightgoldenrodyellow": return "#FAFAD2";
			case "lightgray": return "#D3D3D3";
			case "lightgreen": return "#90EE90";
			case "lightgrey": return "#d3d3d3";
			case "lightpink": return "#FFB6C1";
			case "lightsalmon": return "#FFA07A";
			case "lightseagreen": return "#20B2AA";
			case "lightskyblue": return "#87CEFA";
			case "lightslategray": return "#778899";
			case "lightslategrey": return "#778899";
			case "lightsteelblue": return "#B0C4DE";
			case "lightyellow": return "#FFFFE0";
			case "lime": return "#00FF00";
			case "limegreen": return "#32CD32";
			case "linen": return "#FAF0E6";
			case "magenta": return "#FF00FF";
			case "maroon": return "#800000";
			case "mediumaquamarine": return "#66CDAA";
			case "mediumblue": return "#0000CD";
			case "mediumorchid": return "#BA55D3";
			case "mediumpurple": return "#9370DB";
			case "mediumseagreen": return "#3CB371";
			case "mediumslateblue": return "#7B68EE";
			case "mediumspringgreen": return "#00FA9A";
			case "mediumturquoise": return "#48D1CC";
			case "mediumvioletred": return "#C71585";
			case "midnightblue": return "#191970";
			case "mint": return "#66CDAA";
			case "mintcream": return "#F5FFFA";
			case "mistyrose": return "#FFE4E1";
			case "moccasin": return "#FFE4B5";
			case "navajowhite": return "#FFDEAD";
			case "navy": return "#000080";
			case "ochre": return "#D7C32A";
			case "oldlace": return "#FDF5E6";
			case "olive": return "#808000";
			case "olivedrab": return "#6B8E23";
			case "orange": return "#FFA500";
			case "orangered": return "#FF4500";
			case "orchid": return "#DA70D6";
			case "palegoldenrod": return "#EEE8AA";
			case "palegreen": return "#98FB98";
			case "paleturquoise": return "#AFEEEE";
			case "palevioletred": return "#DB7093";
			case "papayawhip": return "#FFEFD5";
			case "peachpuff": return "#FFDAB9";
			case "peru": return "#CD853F";
			case "pink": return "#FFC0CB";
			case "plum": return "#DDA0DD";
			case "powderblue": return "#B0E0E6";
			case "purple": return "#800080";
			case "pumpkin": return "#FF4500";
			case "rebeccapurple": return "#663399";
			case "red": return "#FF0000";
			case "rosybrown": return "#BC8F8F";
			case "royalblue": return "#4169E1";
			case "saddlebrown": return "#8B4513";
			case "salmon": return "#FA8072";
			case "sandybrown": return "#F4A460";
			case "seagreen": return "#2E8B57";
			case "seashell": return "#FFF5EE";
			case "sienna": return "#A0522D";
			case "silver": return "#C0C0C0";
			case "skyblue": return "#87CEEB";
			case "slateblue": return "#6A5ACD";
			case "slategray": return "#708090";
			case "slategrey": return "#708090";
			case "snow": return "#FFFAFA";
			case "springgreen": return "#00FF7F";
			case "steelblue": return "#4682B4";
			case "tan": return "#D2B48C";
			case "teal": return "#008080";
			case "thistle": return "#D8BFD8";
			case "tomato": return "#FF6347";
			case "transparent": return "#00000000";
			case "turquoise": return "#40E0D0";
			case "violet": return "#EE82EE";
			case "volt": return "#CEFF00";
			case "wheat": return "#F5DEB3";
			case "white": return "#FFFFFF";
			case "whitesmoke": return "#F5F5F5";
			case "yellow": return "#FFFF00";
			case "yellowgreen": return "#9ACD32";

			// FunColors
			case "dotnetpurple2024": return "#512BD4";
			case "xamarinblue2011": return "#5596D8";
			case "androidbotgreen2024": return "#A7CD45";
			case "rubberduckyellow": return "#FFD700";
			case "gameboygreen": return "#9BBC0F";
			case "barbiepink": return "#DA1884";
			case "potatoheadbrown": return "#8B4513";
			case "ticklemeelmored": return "#FF3F3F";
			case "legored": return "#B40000";
			case "hotwheelsblue": return "#005BAC";
			case "nerforange": return "#FF6F00";
			case "playdohyellow": return "#FBE870";
			case "mylittleponypurple": return "#DDA0DD";
			case "transformerssilver": return "#C0C0C0";
			case "gijoegreen": return "#4B5320";
			case "powerrangersred": return "#FF0000";
			case "teenagemutantninjaturtlesgreen": return "#008000";
			case "carebearsrainbow": return "#FF69B4";
			case "appleiibeige": return "#D3D3D3";
			case "commodore64brown": return "#6C4F3D";
			case "atari800blue": return "#0057A0";
			case "ibmpcgray": return "#808080";
			case "trs80silver": return "#C0C0C0";
			case "zxspectrumblack": return "#000000";
			case "amigawhite": return "#FFFFFF";
			case "msxblue": return "#0000FF";
			case "bratsummer": return "#8ACE00";
			case "bratautumn": return "#FF8C00";
			case "neonelectricblue": return "#154FEE";
			case "neonvividmagenta": return "#FF08FC";
			case "pukepink": return "#FF3AC6";
			case "mushypeas": return "#5FA41C";
			case "painfulred": return "#FF1A00";
			case "shrekgreen": return "#009B00";

			default: return colorName;
		}
	}

	public static byte FloatToByte(float value)
	{
		if (value < 0)
		{
			value = 0;
		}
		else if (value > 1.0f)
		{
			value = 1.0f;
		}

		var result = (byte)(value * 255);

		//if (result< 0)
		//{
		//	return 0;
		//}
		//else if (result > 255)
		//{
		//	return 255;
		//}
		//else
		{
			return result;
		}
	}
}
